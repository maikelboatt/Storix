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

                // Find the Prepare method - search in base classes too!
                MethodInfo? prepareMethod = FindPrepareMethod(viewModelType, parameter?.GetType() ?? typeof(int));

                if (prepareMethod != null)
                {
                    try
                    {
                        object convertedParameter = parameter switch
                        {
                            null         => 0,
                            int intParam => intParam,
                            _            => Convert.ToInt32(parameter)
                        };

                        prepareMethod.Invoke(
                            viewModel,
                            new object[]
                            {
                                convertedParameter
                            });
                    }
                    catch (Exception ex)
                    {
                        throw new MvxException($"Failed to invoke Prepare method on {viewModelType.Name}: {ex.Message}", ex);
                    }
                }

                // Initialize the ViewModel asynchronously
                await viewModel.Initialize(); // ✅ AWAIT instead of GetResult()

                return viewModel;
            });
        }

        /// <summary>
        /// Finds the Prepare method in the ViewModel type hierarchy (including base classes)
        /// </summary>
        private static MethodInfo? FindPrepareMethod( Type viewModelType, Type parameterType )
        {
            // Search the entire type hierarchy (including base classes)
            Type? currentType = viewModelType;

            while (currentType != null && currentType != typeof(object))
            {
                // Look for Prepare method with the specific parameter type
                MethodInfo? method = currentType.GetMethod(
                    "Prepare",
                    BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly,
                    null,
                    new[]
                    {
                        parameterType
                    },
                    null);

                if (method != null)
                    return method;

                // Move to base class
                currentType = currentType.BaseType;
            }

            return null;
        }
    }
}
