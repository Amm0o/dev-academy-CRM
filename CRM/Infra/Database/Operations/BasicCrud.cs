

using Microsoft.Data.SqlClient;

namespace CRM.Infra {
    public class BasicCrud
    {

        private readonly DatabaseAccess _dbAccess;
        private readonly ILogger<BasicCrud> _logger;

        BasicCrud(DatabaseAccess databaseAccess, ILogger<BasicCrud> logger)
        {
            _dbAccess = databaseAccess;
            _logger = logger;
        }


        /// <summary>
        /// Checks if a specific value exists in a database table column
        /// </summary>
        /// <param name="tableName">Table name to search in</param>
        /// <param name="columnName">Column to check</param>
        /// <param name="valueToCheck">The value to check for</param>
        /// <returns>True if the value exists, false otherwise</returns>
        public bool CheckIfValueExists(string tableName, string columnName, string valueToCheck)
        {

            _logger?.LogInformation("Checking if {value} is present in table: {table}, column: {column}",
                valueToCheck, tableName, columnName);
            try
            {

                // Using parametarized query to avoid SQL injections
                var count = _dbAccess.ExecuteScalar<int>(
                    $"SELECT * FROM {tableName} WHERE {columnName} = @Value",
                    new SqlParameter("@Value", valueToCheck)
                );

                bool exists = count > 0;
                _logger?.LogDebug("Value {value} in {table}.{column} exists: {exists}",
                    valueToCheck, tableName, columnName, exists);
                return exists;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error checking if value {value} exists in {table}.{column}",
                    valueToCheck, tableName, columnName);
                return false;
            }
        }

        /// <summary>
        /// An overload specifically for checking existence for users
        /// </summary>
        /// <param name="email">Email to check</param>
        /// <returns>True if email exists, false otherwise</returns>
        public bool CheckIfValueExists(string valueToCheck)
        {
            return CheckIfValueExists("Customers", "Email", valueToCheck);
        }


        // Method to check if product exists
        public bool CheckIfProductExists(int productId)
        {
            // Verify product exists and get details
            var productData = _dbAccess.ExecuteQuery(
                "SELECT ProductId, Name, Price, Stock FROM Products WHERE ProductId = @ProductId",
                new SqlParameter("@ProductId", productId)
            );


            if (productData.Rows.Count == 0)
            {
                _logger?.LogError("Product with ID {ProductId} not found", productId);
                return false;
            }

            return true;
        }
    }


}