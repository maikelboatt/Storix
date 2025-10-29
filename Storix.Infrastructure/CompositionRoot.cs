using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MvvmCross.Exceptions;
using MvvmCross.IoC;
using MvvmCross.ViewModels;
using Storix.Application.Services.Products.Interfaces;
using Storix.Application.Stores;
using Storix.Core;
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
            // IConfigurationRoot? configuration = iocProvider.Resolve<IConfigurationRoot>();

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
                typeof(App).Assembly,              // Core assembly
                typeof(IProductService).Assembly,  // Application assembly
                typeof(ProductRepository).Assembly // DataAccess assembly
            ];

            foreach (Assembly assembly in assembliesToScan)
            {
                // Register Services
                assembly
                    .CreatableTypes()
                    .EndingWith("Service")
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

                // Register ViewModels
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
            //
            // // Register ModalNavigationControl
            // iocProvider.RegisterType<IModalNavigationControl, ModalNavigationControl>();
            //
            // Register ViewModelFactory
            iocProvider.RegisterSingleton<IViewModelFactory>(new ViewModelFactory());
        }

        private static void RegisterViewModelFactory( IMvxIoCProvider iocProvider )
        {
            // Register the viewmodel factory function
            iocProvider.RegisterSingleton<Func<Type, object, MvxViewModel>>(() => ( viewModelType, parameter ) =>
            {
                // Resolve the ViewModel instance from the IoC container
                MvxViewModel viewModel = (MvxViewModel)(iocProvider.Resolve(viewModelType)
                                                        ?? throw new MvxIoCResolveException($"Failed to resolve ViewModel of type: {viewModelType.FullName}"));

                // Invoke the "Prepare" method on the ViewModel if it exists
                viewModelType
                    .GetMethod(
                        "Prepare",
                        new[]
                        {
                            parameter.GetType()
                        })
                    ?.Invoke(
                        viewModel,
                        new[]
                        {
                            parameter
                        });

                // Initialize the ViewModel
                viewModel.Initialize();
                return viewModel;
            });
        }
    }
}
