using System;
using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;



namespace CRM.Infra {
    public class DatabaseAccess
    {
        private readonly string _connectionString;
        private readonly ILogger<DatabaseAccess> _logger;

        public DatabaseAccess(string connectionString, ILogger<DatabaseAccess> logger = null)
        {
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new ArgumentException("Not valid the connection string");
            }

            // Since connection string is not empty assign it
            _connectionString = connectionString;
            _logger = logger;
        }

        /// <summary>
        /// Executes SQL commands that don't return data rows (INSERT, UPDATE, DELETE, CREATE TABLE, etc.).
        /// This method is used for Data Definition Language (DDL) commands like CREATE/ALTER/DROP operations
        /// and Data Manipulation Language (DML) operations that modify data.
        /// The method typically returns the number of rows affected for DML statements,
        /// while DDL statements usually return -1 or 0 depending on the provider.
        /// Note: The current implementation doesn't return this value.
        /// </summary>
        /// <param name="query">The SQL command to execute</param>
        /// <param name="parameters">Optional SQL parameters to use with the command (prevents SQL injection)</param>
        public void ExecuteNonQuery(string query, params SqlParameter[] parameters)
        {
            _logger?.LogInformation("Executing non-query: {Query}", query);

            using (var connection = new SqlConnection(_connectionString))
            {
                using (var command = new SqlCommand(query, connection))
                {
                    if (parameters != null)
                    {
                        command.Parameters.AddRange(parameters);
                    }

                    connection.Open();
                    command.ExecuteNonQuery();
                }
            }

            _logger?.LogInformation("Executed non-query: {Query}", query);

        }

        // To do:
        /// <summary>
        /// Executes a SQL query that returns a result set.
        /// This method is used for SELECT statements and other queries that return data rows.
        /// </summary>
        /// <param name="query">The SQL query to execute</param>
        /// <param name="parameters">Optional SQL parameters to use with the query (prevents SQL injection)</param>
        /// <returns>A DataTable containing the query results</returns>

        // Add Execute Scalar
    }
}

