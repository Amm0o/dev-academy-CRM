using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using System;

namespace CRM.Infra
{
    public class DatabaseCreation {
        private readonly DatabaseAccess _dbAccess;
        private readonly ILogger<DatabaseCreation> _logger;

        // Constructor Method
        public DatabaseCreation(DatabaseAccess dbAccess, ILogger<DatabaseCreation> logger = null) {
            _dbAccess = dbAccess ?? throw new ArgumentNullException(nameof(dbAccess));
            _logger = logger;
        }

        private bool CheckIfDbExists(string dbName) {
            var dbExists = _dbAccess.ExecuteScalar<int>(
                    "SELECT COUNT(*) FROM sys.databases WHERE name = @dbName",
                    new SqlParameter("@dbName", dbName)
            );

            return dbExists > 0;
        }

        public bool CreateDatabaseIfNotExist(string databaseName) {
            try {
                _logger?.LogInformation("Checking if {DatabaseName} exists...", databaseName);

                // Check if database exists
                bool dbExist = CheckIfDbExists(databaseName);

                if (dbExist) {
                    _logger?.LogInformation("The database: {DatabaseName} was already created", databaseName);
                    return true;
                } 

                // Create Database
                _logger?.LogInformation("Database: {DatabaseName} did not exist proceeding with creation!", databaseName);
                _dbAccess.ExecuteNonQuery($"CREATE DATABASE {databaseName}");
                
                // Check if db was created
                CheckIfDbExists(databaseName);
                _logger?.LogInformation("Database: {DatabaseName} was created!", databaseName);

                // Return true since db was created
                return true;

            } catch (Exception ex) {
                _logger?.LogError(ex, "There was a problem with creating database: {DatabaseName}.", databaseName);
                return false;
            }
        }

        public bool CreateTablesIfNotExist(string databaseName) {
            _logger?.LogInformation("Creating tables if they don't exist in db: {DatabaseName}", databaseName);

            try {
                // Switch context to the correct db
                _dbAccess.ExecuteNonQuery($"USE {databaseName}");
                
                // Create Users table
                _dbAccess.ExecuteNonQuery(@"
                    IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Users')
                    BEGIN
                        CREATE TABLE Users (
                            UserId INT PRIMARY KEY IDENTITY(1,1),
                            Name NVARCHAR(100) NOT NULL,
                            Email NVARCHAR(100) NOT NULL UNIQUE,
                            PasswordHash NVARCHAR(255) NOT NULL,
                            CreatedAt DATETIME2 NOT NULL DEFAULT GETDATE()
                        );
                    END
                ");
                
                // Create Products table
                _dbAccess.ExecuteNonQuery(@"
                    IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Products')
                    BEGIN
                        CREATE TABLE Products (
                            ProductId INT PRIMARY KEY IDENTITY(1,1),
                            Name NVARCHAR(100) NOT NULL,
                            Description NVARCHAR(500),
                            Category NVARCHAR(50),
                            Price DECIMAL(18,2) NOT NULL,
                            Stock INT NOT NULL DEFAULT 0,
                            ProductGuid UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
                            CreatedAt DATETIME2 NOT NULL DEFAULT GETDATE(),
                            UpdatedAt DATETIME2 NOT NULL DEFAULT GETDATE()
                        );
                    END
                ");
                
                // Create Orders table - Updated to match new Order model
                _dbAccess.ExecuteNonQuery(@"
                    IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Orders')
                    BEGIN
                        CREATE TABLE Orders (
                            OrderId INT PRIMARY KEY IDENTITY(1,1),
                            OrderGuid UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
                            UserId INT NOT NULL,
                            UserNameOrder NVARCHAR(100) NOT NULL,
                            OrderDescription NVARCHAR(500),
                            OrderDate DATETIME2 NOT NULL DEFAULT GETDATE(),
                            Status NVARCHAR(20) NOT NULL DEFAULT 'Pending',
                            TotalAmount DECIMAL(18,2) NOT NULL DEFAULT 0,
                            CreatedAt DATETIME2 NOT NULL DEFAULT GETDATE(),
                            UpdatedAt DATETIME2 NOT NULL DEFAULT GETDATE(),
                            CONSTRAINT FK_Orders_Users FOREIGN KEY (UserId) 
                                REFERENCES Users(UserId)
                        );
                        
                        CREATE INDEX IX_Orders_OrderGuid ON Orders(OrderGuid);
                    END
                ");
                
                // Create OrderItems table - Already matches the OrderItem model
                _dbAccess.ExecuteNonQuery(@"
                    IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'OrderItems')
                    BEGIN
                        CREATE TABLE OrderItems (
                            OrderItemId INT PRIMARY KEY IDENTITY(1,1),
                            OrderId INT NOT NULL,
                            ProductId INT NOT NULL,
                            Quantity INT NOT NULL,
                            UnitPrice DECIMAL(18,2) NOT NULL,
                            CreatedAt DATETIME2 NOT NULL DEFAULT GETDATE(),
                            CONSTRAINT FK_OrderItems_Orders FOREIGN KEY (OrderId) 
                                REFERENCES Orders(OrderId),
                            CONSTRAINT FK_OrderItems_Products FOREIGN KEY (ProductId) 
                                REFERENCES Products(ProductId)
                        );
                    END
                ");
                
                _logger?.LogInformation("Tables created or already existed");
                return true;

            } catch (Exception ex) {
                _logger?.LogError(ex, "There was a problem with creation tables for {DatabaseName}.", databaseName);
                return false;                
            }
        }
    }
}