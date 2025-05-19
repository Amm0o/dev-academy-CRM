

using Microsoft.Data.SqlClient;
using System.Data;
using CRM.Models;

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

        public DataTable GetProductData(int productId)
        {
            try
            {
                var productData = _dbAccess.ExecuteQuery(
                    "SELECT ProductId, Name, Price, Stock FROM Products WHERE ProductId = @ProductId",
                    new SqlParameter("@ProductId", productId)
                );

                _logger.LogInformation("Retrieved productId:{productId} from db", productId);

                return productData;
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Could get product {productId} from db", productId);
                return null;
            }

        }

        public bool InsertOrder(Order order)
        {
            try
            {
                _logger.LogInformation("Inserting into db order with GUID: {orderGuid}", order.OrderGuid);
                var orderId = _dbAccess.ExecuteScalar<Int32>(
                @"INSERT INTO Orders (OrderGuid, CustomerId, UserNameOrder, OrderDescription, TotalAmount, Status)
                VALUES (@OrderGuid, @CustomerId, @UserNameOrder, @OrderDescription, @TotalAmount, @Status);",
                new SqlParameter("@OrderGuid", order.OrderGuid),
                new SqlParameter("@CustomerId", order.CustomerId),
                new SqlParameter("@UserNameOrder", order.UserNameOrder),
                new SqlParameter("@OrderDescription", order.OrderDescription ?? string.Empty),
                new SqlParameter("@TotalAmount", order.TotalAmount),
                new SqlParameter("@Status", order.Status));

                _logger.LogInformation("Insert order {orderGuid} successfully", order.OrderGuid);

                _logger.LogInformation("Inserting order items for order ID: {orderId}", orderId);

                foreach (var item in order.Items)
                {
                    _dbAccess.ExecuteNonQuery(
                        @"INSERT INTO OrderItems (OrderId, ProductId, Quantity, UnitPrice)
                        VALUES (@OrderId, @ProductId, @Quantity, @UnitPrice);
                        
                        -- Update Product Stock
                        UPDATE Products
                        SET Stock = Stock - @Quantity
                        WHERE ProductId = @ProductId;",
                        new SqlParameter("@OrderId", orderId),
                        new SqlParameter("@ProductId", item.ProductId),
                        new SqlParameter("@Quantity", item.Quantity),
                        new SqlParameter("@UnitPrice", item.UnitPrice)
                    );
                }

                _logger.LogInformation("Successfully inserted {count} order items for order ID: {orderId}",
                order.Items.Count, orderId);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Failed to log the order: {orderGuid}", order.OrderGuid);
                return false;
            }



        }

        public DataTable GetOrderFromGuid(Guid orderGuid)
        {
            try
            {
                _logger.LogInformation("Retrieving order: {orderGuid} from db", orderGuid);
                var orderData = _dbAccess.ExecuteQuery(
                    @"SELECT o.OrderId, o.OrderGuid, o.CustomerId, o.UserNameOrder, 
                    o.OrderDescription, o.OrderDate, o.Status, o.TotalAmount
                    FROM Orders o 
                    WHERE o.OrderGuid = @OrderGuid",
                    new SqlParameter("@OrderGuid", orderGuid)
                );

                return orderData;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve order from db, message: {exMessage}", ex.Message);
                return null;
            }
        }


        public DataTable GetAllOrederItems(int orderId)
        {
            try
            {
                _logger.LogInformation("Getting orderItems associated with orderId: {orderId}", orderId);

                var orderItems = _dbAccess.ExecuteQuery(
                    @"SELECT oi.OrderItemId, oi.ProductId, p.Name AS ProductName, 
                      oi.Quantity, oi.UnitPrice, (oi.Quantity * oi.UnitPrice) AS LineTotal
                      FROM OrderItems oi
                      JOIN Products p ON oi.ProductId = p.ProductId
                      WHERE oi.OrderId = @OrderId",
                        new SqlParameter("@OrderId", orderId)
                );

                return orderItems;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get orderItems for orderId: {orderId} with exception message: {exMessage}", orderId, ex.Message);
                return null;
            }
        }

        public bool CustomerExists(int customerId)
        {
            try
            {
                _logger.LogInformation("Checking if customer with ID: {customerId} exists", customerId);
                int exists = _dbAccess.ExecuteScalar<int>(
                    @"SELECT TOP 1 1 FROM Customers WHERE CustomerId = @CustomerId",
                    new SqlParameter("@CustomerId", customerId)
                );

                bool customerExists = (exists == 1);
                _logger.LogInformation("Customer with ID: {customerId} exists: {exists}", customerId, customerExists);
                return customerExists;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to check if customer with id: {customerId} exists: {exMessage}",
                    customerId, ex.Message);
                return false;
            }
        }

        public List<object> GetAllOrderForCustomer(int customerId)
        {
            try
            {

                _logger.LogInformation("Getting all orders for customer with customerId {customerId}", customerId);
                var orderData = _dbAccess.ExecuteQuery(
                @"SELECT OrderId, OrderGuid, UserNameOrder, OrderDate, Status, TotalAmount
                    FROM Orders 
                    WHERE CustomerId = @CustomerId
                    ORDER BY OrderDate DESC",
                new SqlParameter("@CustomerId", customerId));

                var orders = new List<object>();
                foreach (DataRow row in orderData.Rows)
                {
                    orders.Add(new
                    {
                        OrderId = Convert.ToInt32(row["OrderId"]),
                        OrderGuid = Guid.Parse(row["OrderGuid"].ToString()),
                        UserNameOrder = row["UserNameOrder"].ToString(),
                        OrderDate = Convert.ToDateTime(row["OrderDate"]),
                        Status = row["Status"].ToString(),
                        TotalAmount = Convert.ToDecimal(row["TotalAmount"])
                    });
                }

                _logger.LogInformation("Got all order for customer with id: {customerId}", customerId);
                return orders;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get all orders for customer with customerId: {customerId} with message: ", customerId, ex.Message);
                throw; // Rethrow exception
            }

        }
    }


}