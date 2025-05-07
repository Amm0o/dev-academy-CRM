using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System.IO;
using CRM.Infra.Misc;


namespace CRM.Infra.Logging {
    public static class LoggingConfig {


        private static readonly ILogger _logger;

        static LoggingConfig() {
            using ILoggerFactory loggerFactory = ConfigureLogging();
            _logger = loggerFactory.CreateLogger("CRM.App");
        }

        public static ILogger Logger => _logger;
        public static ILoggerFactory ConfigureLogging(IConfiguration? configuration = null) {

            // Ensuring the logs directory exists if not create it
            FolderIO.CheckIfFolderExists("Logs");

            // Default log level with configuration override
            LogLevel minimumLogLevel = LogLevel.Information;

            // Read configuration if available
            if (configuration != null) {
                string configLogLevel = configuration.GetValue<string>("Logging:minimumLogLevel") ?? string.Empty;

                Console.WriteLine("LogLevel: " + configLogLevel);
                
                if(string.IsNullOrEmpty(configLogLevel))
                    throw new Exception("Unable to get configLogLevel");

                if(!string.IsNullOrWhiteSpace(configLogLevel) && Enum.TryParse<LogLevel>(configLogLevel, out LogLevel configureLogLevel)) {
                    minimumLogLevel = configureLogLevel;
                }
            }

            return LoggerFactory.Create(builder => {
                builder.AddConsole(); // Console logging
                builder.AddDebug(); // Debug output
                builder.AddFile("Logs/CRM_Logs.txt"); // File logging
                builder.SetMinimumLevel(minimumLogLevel); // Capture information and above
            });
        }
    }
}