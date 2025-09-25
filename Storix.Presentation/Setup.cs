using System.IO;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MvvmCross.IoC;
using MvvmCross.Platforms.Wpf.Core;
using Serilog;
using Serilog.Extensions.Logging;
using Storix.Infrastructure;

namespace Storix.Presentation
{
    public class Setup:MvxWpfSetup<Core.App>
    {
        private IConfigurationRoot _configuration;

        protected override ILoggerProvider? CreateLogProvider() => new SerilogLoggerProvider();

        protected override ILoggerFactory? CreateLogFactory()
        {
            _configuration = new ConfigurationBuilder()
                             .SetBasePath(Directory.GetCurrentDirectory())
                             .AddJsonFile("appsettings.json", false, true)
                             .Build();

            Log.Logger = new LoggerConfiguration()
                         .ReadFrom.Configuration(_configuration)
                         .CreateLogger();

            return new SerilogLoggerFactory();
        }

        protected override void InitializeFirstChance( IMvxIoCProvider iocProvider )
        {
            base.InitializeFirstChance(iocProvider);

            CompositionRoot.RegisterAllDependencies(iocProvider, _configuration);
        }
    }
}
