namespace CRM.Infra;

using System;
using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging; // To do setup logging

public class DatabaseAccess
{

    private readonly string _connectionString;

    public DatabaseAccess(string connectionString)
    {
        if (string.IsNullOrEmpty(connectionString))
        {
            throw new ArgumentException("Not valid the connection string");
        }

        // Since connection string is not empty assign it
        _connectionString = connectionString;
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

    }

    // To do:
    // Add Execute query
    // Add Execute Scalar
}