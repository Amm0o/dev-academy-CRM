using Microsoft.Data.SqlClient;
using CRM.Infra;


namespace CRM.Infra
{
    public class DatabaseCreation {
        private readonly DatabaseAccess _dbAccess;
        private readonly ILogger<DatabaseCreation> _logger;


        // Constructor Method
        public DatabaseCreation (DatabaseAccess dbAccess, ILogger<DatabaseCreation> logger = null) {

            _dbAccess = dbAccess ?? throw new ArgumentNullException(nameof(dbAccess));
            _logger = logger;
        }

        private bool CheckIfDbExists(string dbName) {

            var dbExists = _dbAccess.ExecuteScalar<int>(
                    "SELECT COUNT(*) FROM sys.databases WHERE name = @dbName",
                    new SqlParameter("@dbName", dbName)
            );

            return dbExists > 0 ? true : false;
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
                _logger?.LogError("There was a problem with creating database: {DatabaseName}.", databaseName);
                return false;
            }
        }
    }
}