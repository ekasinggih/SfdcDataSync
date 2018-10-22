using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using log4net;
using log4net.Config;
using Microsoft.Extensions.DependencyInjection;
using SfdcDataSync.Core;
using SfdcDataSync.Core.Adapter;

namespace SfdcDataSync
{
    class Program
    {
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        static async Task Main(string[] args)
        {
            // configure logger
            string outputLog = string.Empty;
            if (args.Length > 1)
                outputLog = args[1];
            ConfigureLog(outputLog);

            // get config files
            if (!args.Any())
            {
                Log.Fatal("argument config filename not foud. please execute with argument filename config. ex: $ dotnet SfdcDataSync.dll RTContacts.json");
                return;
            }
            string configFile = args[0];

            // configure Ioc
            ServiceProvider serviceProvider = ConfigureIoC();

            // execute job
            ISqlAdapter sqlAdapter = serviceProvider.GetService<ISqlAdapter>();
            ISfdcAdapter sfdcAdapter = serviceProvider.GetService<ISfdcAdapter>();
            IResultLogger resultLogger = serviceProvider.GetService<IResultLogger>();
            JobExecutor executor = new JobExecutor(sqlAdapter, sfdcAdapter, resultLogger);

            try
            {
                await executor.ExecuteAsync(configFile);
            }
            catch (Exception ex)
            {
                Log.Fatal("error on executing job", ex);
            }
        }
        public static ServiceProvider ConfigureIoC()
        {
            ServiceProvider serviceProvider = new ServiceCollection()
                .AddSingleton<ISfdcAdapter, SfdcAdapter>()
                .AddSingleton<ISqlAdapter, SqlAdapter>()
                .AddSingleton<IResultLogger, SqlResultLogger>()
                .BuildServiceProvider();

            return serviceProvider;
        }

        private static void ConfigureLog(string outputPath = "")
        {
            var logRepository = LogManager.GetRepository(Assembly.GetEntryAssembly());
            XmlConfigurator.Configure(logRepository, new FileInfo("log4net.config"));
            Logger.SetLogger(Log.Debug, Log.Debug, Log.Info, Log.Info, Log.Warn, Log.Warn, Log.Error, Log.Error, Log.Fatal, Log.Fatal, outputPath);
        }
    }
}
