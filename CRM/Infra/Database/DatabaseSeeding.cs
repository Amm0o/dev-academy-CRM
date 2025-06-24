using BCrypt.Net;
using CRM.Models;
using Microsoft.Extensions.Configuration;


namespace CRM.Infra
{
    public class DatabaseSeeder
    {
        private readonly BasicCrud _basicCrud;
        private readonly IConfiguration _configuration;
        private readonly ILogger<DatabaseSeeder> _logger;

        public DatabaseSeeder(BasicCrud basicCrud, IConfiguration configuration, ILogger<DatabaseSeeder> logger)
        {
            _basicCrud = basicCrud;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task SeedInitialAdminAsync()
        {
            try
            {
                var adminEmail = _configuration["AdminUser:Email"] ?? "admin@crm.com";
                var adminPassword = _configuration["AdminUser:Password"];
                var adminName = _configuration["AdminUser:Name"] ?? "System Administrator";

                // Check if admin password is configured
                if (string.IsNullOrEmpty(adminPassword))
                {
                    _logger.LogWarning("Admin password was not found in config file skipping adming user creation");
                    return;
                }

                // Check if admin already exists
                if (_basicCrud.CheckIfValueExists("Users", "Email", adminEmail))
                {
                    _logger.LogInformation("Skipping user admin  creation since admin user {email} was already in db", adminEmail);
                    return;
                }

                // Create admin user
                var hashedPassword = BCrypt.Net.BCrypt.HashPassword(adminPassword);
                var adminUser = new User(adminName, adminEmail, hashedPassword, UserRole.Admin);

                _basicCrud.RegisterUser(adminUser, hashedPassword);

                _logger.LogInformation("Initial admin created succesfully with email {email}", adminEmail);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while creating initial admin");
                throw;
            }
        }
    }
}