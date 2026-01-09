using System;
using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MvvmCross.Exceptions;
using MvvmCross.IoC;
using MvvmCross.ViewModels;
using Storix.Application.DataAccess;
using Storix.Application.Services.Products;
using Storix.Application.Services.Products.Interfaces;
using Storix.Application.Stores;
using Storix.Core;
using Storix.Core.Control;
using Storix.Core.Factory;
using Storix.DataAccess.DBAccess;
using Storix.DataAccess.Repositories;

namespace Storix.Infrastructure
{
    public static class CompositionRoot
    {
        public static void RegisterAllDependencies( IMvxIoCProvider iocProvider, IConfigurationRoot configuration )
        {
            RegisterConfiguration(iocProvider);
            RegisterLogging(iocProvider);
            RegisterDataAccess(iocProvider, configuration);
            RegisterServicesByConvention(iocProvider);
            RegisterCustomServices(iocProvider);
            RegisterViewModelFactory(iocProvider);
        }

        private static void RegisterConfiguration( IMvxIoCProvider iocProvider )
        {
            IConfigurationRoot configuration = new ConfigurationBuilder()
                                               .SetBasePath(AppContext.BaseDirectory)
                                               .AddJsonFile("appsettings.json", false, true)
                                               .Build();

            // Register the configuration file
            iocProvider.RegisterSingleton(configuration);
        }

        private static void RegisterLogging( IMvxIoCProvider iocProvider )
        {
            iocProvider.RegisterType(typeof(ILogger<>), typeof(Logger<>));
        }

        private static void RegisterDataAccess( IMvxIoCProvider iocProvider, IConfigurationRoot configurationRoot )
        {
            // Register ISqlDataAccess with the connection string
            iocProvider.LazyConstructAndRegisterSingleton<ISqlDataAccess>(() =>
            {
                ILogger<SqlDataAccess>? logger = iocProvider.Resolve<ILogger<SqlDataAccess>>();
                return new SqlDataAccess(configurationRoot.GetConnectionString("StorixDB"), logger);
            });
        }

        private static void RegisterServicesByConvention( IMvxIoCProvider iocProvider )
        {
            Assembly[] assembliesToScan =
            [
                typeof(App).Assembly,               // Core assembly
                typeof(IProductService).Assembly,   // Application assembly
                typeof(ProductRepository).Assembly, // DataAccess assembly
                typeof(CompositionRoot).Assembly    // Infrastructure assembly
            ];

            foreach (Assembly assembly in assembliesToScan)
            {
                // Register Services
                assembly
                    .CreatableTypes()
                    .EndingWith("Service")
                    .AsInterfaces()
                    .RegisterAsLazySingleton();

                // Register Managers
                assembly
                    .CreatableTypes()
                    .EndingWith("Manager")
                    .AsInterfaces()
                    .RegisterAsLazySingleton();

                // Register Stores
                assembly
                    .CreatableTypes()
                    .EndingWith("Store")
                    .AsInterfaces()
                    .RegisterAsLazySingleton();

                // Register Helpers
                assembly
                    .CreatableTypes()
                    .EndingWith("Helper")
                    .AsInterfaces()
                    .RegisterAsLazySingleton();

                // Register Validation Classes
                assembly
                    .CreatableTypes()
                    .EndingWith("Validation")
                    .AsInterfaces()
                    .RegisterAsLazySingleton();

                // Register Repositories
                assembly
                    .CreatableTypes()
                    .EndingWith("Repository")
                    .AsInterfaces()
                    .RegisterAsLazySingleton();

                // Register Validators (FluentValidation)
                assembly
                    .CreatableTypes()
                    .EndingWith("Validator")
                    .AsInterfaces()
                    .RegisterAsLazySingleton();

                // Register ViewModels (including abstract base classes will be ignored)
                assembly
                    .CreatableTypes()
                    .EndingWith("ViewModel")
                    .AsTypes()
                    .RegisterAsDynamic();
            }
        }

        private static void RegisterCustomServices( IMvxIoCProvider iocProvider )
        {
            // Register ModalNavigationStore
            iocProvider.RegisterSingleton(new ModalNavigationStore());

            // Register ModalNavigationControl
            iocProvider.RegisterType<IModalNavigationControl, ModalNavigationControl>();

            // Register ViewModelFactory
            iocProvider.RegisterSingleton<IViewModelFactory>(new ViewModelFactory());
        }

        private static void RegisterViewModelFactory( IMvxIoCProvider iocProvider )
        {
            // Register async viewmodel factory function
            iocProvider.RegisterSingleton<Func<Type, object, Task<MvxViewModel>>>(() => async ( viewModelType, parameter ) =>
            {
                // Resolve the ViewModel instance from the IoC container
                MvxViewModel viewModel = (MvxViewModel)(iocProvider.Resolve(viewModelType)
                                                        ?? throw new MvxIoCResolveException($"Failed to resolve ViewModel of type: {viewModelType.FullName}"));

                // ✅ Find the Prepare method with the actual parameter type
                Type? parameterType = parameter?.GetType();
                MethodInfo? prepareMethod = null;

                if (parameterType != null)
                {
                    // Try to find Prepare method that matches the parameter type
                    prepareMethod = FindPrepareMethod(viewModelType, parameterType);
                }

                // Fallback to int parameter if no specific type found
                if (prepareMethod == null && parameter != null)
                {
                    prepareMethod = FindPrepareMethod(viewModelType, typeof(int));
                    if (prepareMethod != null)
                    {
                        // Convert parameter to int if needed
                        parameter = parameter switch
                        {
                            int intParam => intParam,
                            _            => Convert.ToInt32(parameter)
                        };
                    }
                }

                if (prepareMethod != null)
                {
                    try
                    {
                        System.Diagnostics.Debug.WriteLine($"🔧 Calling Prepare with {parameter?.GetType().Name ?? "null"}");
                        prepareMethod.Invoke(
                            viewModel,
                            new[]
                            {
                                parameter
                            });
                        System.Diagnostics.Debug.WriteLine($"✅ Prepare called successfully");
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"❌ Failed to invoke Prepare: {ex.Message}");
                        throw new MvxException($"Failed to invoke Prepare method on {viewModelType.Name}: {ex.Message}", ex);
                    }
                }

                System.Diagnostics.Debug.WriteLine($"🔧 Calling Initialize()");
                await viewModel.Initialize();
                System.Diagnostics.Debug.WriteLine($"✅ Initialize() completed");

                return viewModel;

            });
        }

        /// <summary>
        /// Finds the Prepare method in the ViewModel type hierarchy (including base classes)
        /// </summary>
        private static MethodInfo? FindPrepareMethod( Type viewModelType, Type parameterType )
        {
            System.Diagnostics.Debug.WriteLine($"🔍 Searching for Prepare method in {viewModelType.Name}");
            System.Diagnostics.Debug.WriteLine($"🔍 Looking for parameter type: {parameterType.Name}");

            // Search the entire type hierarchy (including base classes)
            Type? currentType = viewModelType;
            int depth = 0;

            while (currentType != null && currentType != typeof(object))
            {
                System.Diagnostics.Debug.WriteLine($"🔍 Checking type: {currentType.Name} at depth {depth}");

                // Get all public instance methods named "Prepare" declared only in the current type
                List<MethodInfo> prepareMethods = currentType
                                                  .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                                                  .Where(m => m.Name == "Prepare")
                                                  .ToList();

                System.Diagnostics.Debug.WriteLine($"    Found {prepareMethods.Count} Prepare methods");

                foreach (MethodInfo method in from method in prepareMethods
                                              let parameters = method.GetParameters()
                                              where parameters.Length == 1 && parameters[0].ParameterType == parameterType
                                              select method)
                {
                    System.Diagnostics.Debug.WriteLine($"✅ Found matching Prepare method in {currentType.Name} with parameter type {parameterType.Name}");
                    return method;
                }

                // Move to the base type
                currentType = currentType.BaseType;
                depth++;
            }
            System.Diagnostics.Debug.WriteLine($"❌ No matching Prepare method found for parameter type {parameterType.Name}");
            return null;
        }
    }
}
