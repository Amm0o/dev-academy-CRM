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
        

        public int ExecuteNonQueryReturn(string query, params SqlParameter[] parameters)
        {
            _logger?.LogInformation("Executing non-query: {Query}", query);
            int rowsAffected = 0;

            using (var connection = new SqlConnection(_connectionString))
            {
                using (var command = new SqlCommand(query, connection))
                {
                    if (parameters != null)
                    {
                        command.Parameters.AddRange(parameters);
                    }

                    connection.Open();
                    rowsAffected = command.ExecuteNonQuery(); // Capture the return value
                }
            }

            _logger?.LogInformation("Executed non-query: {Query}, affected {RowCount} rows", query, rowsAffected);
            return rowsAffected;
        }


        /// <summary>
        /// Executes a SQL query that returns a result set.
        /// This method is used for SELECT statements and other queries that return data rows.
        /// </summary>
        /// <param name="query">The SQL query to execute</param>
        /// <param name="parameters">Optional SQL parameters to use with the query (prevents SQL injection)</param>
        /// <returns>A DataTable containing the query results</returns>
        public DataTable ExecuteQuery(string query, params SqlParameter[] parameters)
        {
            _logger?.LogInformation("Executing query: {Query}", query);

            DataTable result = new DataTable();

            using (var connection = new SqlConnection(_connectionString))
            {
                using (var command = new SqlCommand(query, connection))
                {
                    if (parameters != null)
                    {
                        command.Parameters.AddRange(parameters);
                    }

                    connection.Open();

                    using (var adapter = new SqlDataAdapter(command))
                    {
                        adapter.Fill(result);
                    }
                }
            }

            _logger?.LogInformation("Executed query: {Query}, returned {RowCount} rows", query, result.Rows.Count);
            return result;
        }

        /// <summary>
        /// Executes a SQL query that returns a single value.
        /// This method is ideal for aggregate functions like COUNT, SUM, AVG, etc.,
        /// or for queries that should return exactly one value.
        /// </summary>
        /// <typeparam name="T">The expected return type</typeparam>
        /// <param name="query">The SQL query to execute</param>
        /// <param name="parameters">Optional SQL parameters to use with the query (prevents SQL injection)</param>
        /// <returns>The first column of the first row in the result set, or default(T) if no rows</returns>
        public T ExecuteScalar<T>(string query, params SqlParameter[] parameters)
        {
            _logger?.LogInformation("Executing scalar query: {Query}", query);
            
            using (var connection = new SqlConnection(_connectionString))
            {
                using (var command = new SqlCommand(query, connection))
                {
                    if (parameters != null)
                    {
                        command.Parameters.AddRange(parameters);
                    }

                    connection.Open();
                    var result = command.ExecuteScalar();
                    
                    if (result == null || result == DBNull.Value)
                    {
                        _logger?.LogInformation("Scalar query returned null: {Query}", query);
                        return default;
                    }
                    
                    try
                    {
                        T typedResult = (T)Convert.ChangeType(result, typeof(T));
                        _logger?.LogInformation("Scalar query returned value of type {Type}: {Query}", typeof(T).Name, query);
                        return typedResult;
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogError(ex, "Failed to convert scalar result to type {Type}: {Query}", typeof(T).Name, query);
                        return default;
                    }
                }
            }
        }
    }
}

