using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
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
using System.Threading.Tasks;

class CRMMain
{
    static async Task Main(string[] args)
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

            // Register DB Creation 
            builder.Services.AddSingleton<DatabaseCreation>(sp => {
                var dbAccess = sp.GetRequiredService<DatabaseAccess>();
                var logger = sp.GetRequiredService<ILogger<DatabaseCreation>>();
                return new DatabaseCreation(dbAccess, logger);
            });

            // Add service for basic CRUD activities
            builder.Services.AddSingleton<BasicCrud>();

            // Add JWT configuration
            builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = builder.Configuration["Jwt:Issuer"],
                    ValidAudience = builder.Configuration["Jwt:Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:SecretKey"]))
                };
            });

            // Register BlackListing Service
            builder.Services.AddSingleton<CRM.Infra.Authentication.ITokenBlacklistService, CRM.Infra.Authentication.TokenBlacklistService>();

            // Add JWT Authorization service.
            builder.Services.AddAuthorization(options =>
            {
                options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
                options.AddPolicy("RegularOrAdmin", policy => policy.RequireRole("Regular", "Admin"));
            });
            

            // Register the service
            builder.Services.AddScoped<CRM.Infra.Authentication.JwtService>();

            // Register db seeder
            builder.Services.AddScoped<DatabaseSeeder>();

            // Add CORS service
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("DevelopmentPolicy", builder =>
                {
                    builder.WithOrigins("http://localhost:3000") // React dev server
                        .AllowAnyHeader()
                        .AllowAnyMethod()
                        .AllowCredentials();
                });
            });


            // Build the application
            var app = builder.Build();

            // Get logger from application services - use CRMMain as the category
            var logger = app.Services.GetRequiredService<ILogger<CRMMain>>();

            // Initialize DB and it's tables

            try
            {
                logger.LogInformation("Initializing DBs and it's tables...");

                var dbCreation = app.Services.GetRequiredService<DatabaseCreation>();

                // Get database name from configuration
                string dbName = configuration.GetValue<string>("ConnectionString:DatabaseName") ?? "CRM";

                // Create databse and table
                var dbCreated = dbCreation.CreateDatabaseIfNotExist(dbName);
                if (dbCreated)
                {
                    logger.LogInformation("Database created initiating table creation");

                    bool tablesCreated = dbCreation.CreateTablesIfNotExist(dbName);

                    if (tablesCreated)
                        logger.LogInformation("Tables created");
                    else
                        logger.LogError("Failed to created the tables");
                }
                else
                {
                    logger.LogError("Failed to create db");
                }

            }
            catch (Exception ex)
            {
                logger.LogCritical(ex, "Failed to created db and it's tables");
            }

            // Now seed db
            using (var scope = app.Services.CreateScope())
            {
                var seeder = scope.ServiceProvider.GetRequiredService<DatabaseSeeder>();
                await seeder.SeedInitialAdminAsync();
            }
            
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
            app.MapControllers();

            // Add authentication and authorization middleware
            app.UseAuthentication();
            // Use middleware for blacklisting
            app.UseMiddleware<CRM.Infra.Middlewares.Authentication.JwtBlacklistMiddleware>();
            app.UseAuthorization();

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