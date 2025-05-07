using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using CRM.Infra;
using CRM.Infra.Logging;
using Microsoft.AspNetCore.Builder;
using System;
using System.Collections.Generic;
using System.Linq;

class CRMMain
{
    static void Main(string[] args)
    {
        try
        {
            // Create a configuration from appsettings.json
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: false)
                .AddEnvironmentVariables()
                .Build();

            // Enable console logging for troubleshooting
            Console.WriteLine("Starting application...");
            Console.WriteLine($"Log level from config: {configuration["Logging:minimumLogLevel"] ?? "not found"}");

            // Create web application builder
            var builder = WebApplication.CreateBuilder(args);

            // Use the existing LoggingConfig to configure logging directly
            builder.Logging.ClearProviders();
            builder.Logging.AddConsole(options => {
                options.IncludeScopes = true;
                options.TimestampFormat = "[HH:mm:ss] ";
            });
            builder.Logging.AddDebug();
            builder.Logging.AddFile("Logs/CRM_Logs.txt");

            // Set minimum log level
            LogLevel minimumLogLevel = LogLevel.Debug; // Lower this for troubleshooting
            builder.Logging.SetMinimumLevel(minimumLogLevel);
            
            // Register services
            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            // Register DatabaseAccess with DI
            builder.Services.AddSingleton<DatabaseAccess>(sp => 
            {
                var connectionString = configuration.GetValue<string>("ConnectionString:SqlServer");
                var logger = sp.GetRequiredService<ILogger<DatabaseAccess>>();
                return new DatabaseAccess(connectionString, logger);
            });

            // Build the application
            var app = builder.Build();

            // Get logger from application services - use CRMMain as the category
            var logger = app.Services.GetRequiredService<ILogger<CRMMain>>();
            
            // Force log a test message at all levels
            // logger.LogTrace("Trace message - should only appear if trace logging enabled");
            // logger.LogDebug("Debug message - should appear if debug logging enabled");
            // logger.LogInformation("Information message - this should always appear");
            // logger.LogWarning("Warning test message");
            // logger.LogError("Error test message");

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
                logger.LogInformation("Development environment detected, Swagger enabled");
            }

            app.UseHttpsRedirection();
            app.UseAuthorization();
            app.MapControllers();

            // Log startup message with higher severity to ensure it appears
            logger.LogWarning("CRM application started successfully at {Time}", DateTime.UtcNow);
            Console.WriteLine($"Application started at {DateTime.UtcNow}");

            app.Run();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Application failed to start: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
        }
    }
}