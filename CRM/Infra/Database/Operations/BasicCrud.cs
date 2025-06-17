using Microsoft.Data.SqlClient;
using System.Data;
using CRM.Models;

namespace CRM.Infra
{
    public class BasicCrud
    {

        private readonly DatabaseAccess _dbAccess;
        private readonly ILogger<BasicCrud> _logger;

        public BasicCrud(DatabaseAccess databaseAccess, ILogger<BasicCrud> logger)
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
            return CheckIfValueExists("Users", "Email", valueToCheck);
        }


        // Method to check if product exists
        public bool CheckIfProductExists(int productId)
        {
            // Verify product exists and get details
            var productData = _dbAccess.ExecuteQuery(
                "SELECT ProductId, Name, Price, Stock, CreatedAt, UpdatedAt FROM Products WHERE ProductId = @ProductId",
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
                    "SELECT ProductId, Name, Price, Stock, CreatedAt, UpdatedAt FROM Products WHERE ProductId = @ProductId",
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

        public int InsertOrder(Order order)
        {
            try
            {
                _logger.LogInformation("Inserting into db order with GUID: {orderGuid}", order.OrderGuid);
                var orderId = _dbAccess.ExecuteScalar<int>(
                    @"INSERT INTO Orders (OrderGuid, UserId, UserNameOrder, OrderDescription, TotalAmount, Status)
                    VALUES (@OrderGuid, @CustomerId, @UserNameOrder, @OrderDescription, @TotalAmount, @Status);
                    SELECT SCOPE_IDENTITY();",
                    new SqlParameter("@OrderGuid", order.OrderGuid),
                    new SqlParameter("@CustomerId", order.CustomerId),
                    new SqlParameter("@UserNameOrder", order.UserNameOrder),
                    new SqlParameter("@OrderDescription", order.OrderDescription ?? string.Empty),
                    new SqlParameter("@TotalAmount", order.TotalAmount),
                    new SqlParameter("@Status", order.Status)
                );

                _logger.LogInformation("Inserted order {orderGuid} with ID: {orderId}", order.OrderGuid, orderId);

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

                return orderId;
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Failed to log the order: {orderGuid}", order.OrderGuid);
                throw; // To propagate the exception
            }



        }

        public DataTable GetOrderFromGuid(Guid orderGuid)
        {
            try
            {
                _logger.LogInformation("Retrieving order: {orderGuid} from db", orderGuid);
                var orderData = _dbAccess.ExecuteQuery(
                    @"SELECT o.OrderId, o.OrderGuid, o.UserId, o.UserNameOrder, 
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

        public bool CustomerExists(int userId)
        {
            try
            {
                _logger.LogInformation("Checking if customer with ID: {customerId} exists", userId);
                int exists = _dbAccess.ExecuteScalar<int>(
                    @"SELECT TOP 1 1 FROM Users WHERE UserId = @UserId",
                    new SqlParameter("@UserId", userId)
                );

                bool customerExists = (exists == 1);
                _logger.LogInformation("Customer with ID: {customerId} exists: {exists}", userId, customerExists);
                return customerExists;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to check if customer with id: {customerId} exists: {exMessage}",
                    userId, ex.Message);
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
                    WHERE UserId = @CustomerId
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


        public DataTable GetUserFromId(int id)
        {
            try
            {
                _logger.LogInformation("Querying DB for user with ID: {id}", id);

                var userData = _dbAccess.ExecuteQuery(
                     "SELECT UserId, Name, Email, CreatedAt FROM Users WHERE UserId = @UserId",
                     new SqlParameter("@UserId", id)
                 );

                return userData;
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed to get user by Id: {id}", id);
                // Throw to bubble up the exception
                throw;
            }
        }


        public bool RegisterUser(User user, string passowordHash)
        {
            try
            {
                _logger.LogInformation("Inserting user into db");

                _dbAccess.ExecuteNonQuery(
                    @"INSERT INTO Users (Name, Email, PasswordHash, Role,CreatedAt) 
                    VALUES (@Name, @Email, @PasswordHash, @Role,GETDATE());
                    SELECT SCOPE_IDENTITY();",
                    new Microsoft.Data.SqlClient.SqlParameter("@Name", user.Name),
                    new Microsoft.Data.SqlClient.SqlParameter("@Email", user.Email),
                    new Microsoft.Data.SqlClient.SqlParameter("@PasswordHash", passowordHash),
                    new Microsoft.Data.SqlClient.SqlParameter("@Role", user.Role)
                );

                _logger.LogInformation("Inserted user into db");

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, "Failed to insert user into db");
                return false;
            }
        }

        public bool DeleteUser(int userId)
        {
            try
            {
                _logger.LogInformation("Removing user with {id} from db", userId);

                int rowsAffected = _dbAccess.ExecuteNonQueryReturn(
                    @"DELETE FROM CartItems WHERE CartId IN (SELECT CartId FROM Carts WHERE UserId = @UserId);
                    DELETE FROM Carts WHERE UserId = @UserId;
                    DELETE FROM OrderItems WHERE OrderId IN (SELECT OrderId FROM Orders WHERE UserId = @UserId);
                    DELETE FROM Orders WHERE UserId = @UserId;
                    DELETE FROM Users WHERE UserId = @UserId",
                    new SqlParameter("@UserId", userId)
                );

                if (rowsAffected == 0)
                {
                    _logger.LogError("Failed to delete user with id {id}", userId);
                    return false;
                }

                _logger.LogInformation("Successfully deleted user with id {id}", userId);
                return true;

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while executing the DELETE SQL query to remove the user from db");
                return false;
            }
        }

        public int GetUserIdFromMail(string email)
        {
            try
            {
                var userId = _dbAccess.ExecuteScalar<int>(
                    $"SELECT UserId FROM Users WHERE Email = @email",
                    new SqlParameter("@email", email)
                );

                return userId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, "Failed to get userId using email {email}", email);
                return -1;
            }

        }


        public Carts GetUserCart(int userId)
        {
            try
            {
                _logger.LogInformation("Getting Cart for userId: {userId}", userId);

                // First check if the user has a cart
                var cartData = _dbAccess.ExecuteQuery(
                    @"SELECT CartId, UserId, CreatedAt, UpdatedAt
                    FROM Carts WHERE UserId = @UserId",
                    new SqlParameter("@UserId", userId)
                );

                // If now CART exists create one
                if (cartData.Rows.Count == 0)
                {
                    _logger.LogInformation("No cart existed for userId {userId}, created an empty one", userId);
                    return new Carts(userId);
                }

                // Build cart object
                var cartRow = cartData.Rows[0];
                var cart = new Carts(userId)
                {
                    CreatedAt = Convert.ToDateTime(cartRow["CreatedAt"]),
                    UpdatedAt = Convert.ToDateTime(cartRow["UpdatedAt"])
                };

                int cartId = Convert.ToInt32(cartRow["CartId"]);

                // Get cartItems
                var cartItemsData = _dbAccess.ExecuteQuery(
                    @"SELECT ci.CartItemId, ci.ProductId, ci.Quantity, ci.UnitPrice, p.Name AS ProductName
                    FROM CartItems ci
                    JOIN Products p on ci.ProductId = p.ProductId
                    WHERE ci.CartId = @CartId",
                    new SqlParameter("@CartId", cartId)
                );

                // Add the items to the cart object
                foreach (DataRow item in cartItemsData.Rows)
                {
                    cart.Items.Add(new CartItems(
                        cartId,
                        Convert.ToInt32(item["ProductId"]),
                        Convert.ToInt32(item["Quantity"]),
                        Convert.ToInt32(item["UnitPrice"])
                    ));
                }

                _logger.LogInformation("Sucessfully build cart object and returned it for user: {userId}", userId);

                return cart;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting cart for user {userId}", userId);
                throw;
            }
        }


        public bool AddItemToCart(int userId, int productId, int quantity, decimal unitPrice)
        {
            try
            {
                _logger.LogInformation("Adding product {ProductId} to cart for user {UserId}", productId, userId);

                // Get or create a cart
                int cartId = EnsureCartExists(userId);

                var existingItem = _dbAccess.ExecuteQuery(
                    @"SELECT CartItemId, Quantity FROM CartItems 
                    WHERE CartId = @CartId AND ProductId = @ProductId",
                    new SqlParameter("@CartId", cartId),
                    new SqlParameter("@ProductId", productId)
                );

                if (existingItem.Rows.Count > 0)
                {

                    // Update existing item
                    int currentQuantity = Convert.ToInt32(existingItem.Rows[0]["Quantity"]);
                    int newQuantity = currentQuantity + quantity;

                    _dbAccess.ExecuteNonQuery(
                        @"UPDATE CartItems SET Quantity = @Quantity 
                        WHERE CartId = @CartId AND ProductId = @ProductId",
                        new SqlParameter("@CartId", cartId),
                        new SqlParameter("@ProductId", productId),
                        new SqlParameter("@Quantity", newQuantity)
                    );
                }
                else
                {
                    // Add new item
                    _dbAccess.ExecuteNonQuery(
                        @"INSERT INTO CartItems (CartId, ProductId, Quantity, UnitPrice) 
                        VALUES (@CartId, @ProductId, @Quantity, @UnitPrice)",
                        new SqlParameter("@CartId", cartId),
                        new SqlParameter("@ProductId", productId),
                        new SqlParameter("@Quantity", quantity),
                        new SqlParameter("@UnitPrice", unitPrice)
                    );
                }

                // Update cart timestamp
                _dbAccess.ExecuteNonQuery(
                    "UPDATE Carts SET UpdatedAt = GETDATE() WHERE CartId = @CartId",
                    new SqlParameter("@CartId", cartId)
                );

                _logger.LogInformation("Added product {ProductId} to cart for user {UserId}", productId, userId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to add product {ProductId} to cart for user {UserId}", productId, userId);
                return false;
            }
        }

        public bool UpdateCartItem(int userId, int productId, int quantity)
        {
            try
            {
                _logger.LogInformation("Updating product {ProductId} quantity to {Quantity} in cart for user {UserId}",
                    productId, quantity, userId);


                // Get CartId -- return false 
                int cartId = GetCartId(userId);
                if (cartId == -1)
                {
                    _logger.LogInformation("Could not find any cart for userId {userId} - failed to update the cart", userId);
                    return false;
                }
                else if (cartId == -2)
                {
                    _logger.LogInformation("Failed to return cart for userId {userId} - failed to update the cart", userId);
                    return false;
                }

                // If quantity is 0 or less then remove it
                if (quantity <= 0)
                {
                    _logger.LogInformation("Removing product {productId} from user {userId} cart since quantity {quantity} is < 0", productId, userId, quantity);
                    return RemoveCartItem(userId, productId);
                }

                // Update Item Quantity
                int rowsAffected = _dbAccess.ExecuteNonQueryReturn(
                    @"UPDATE CartItems
                    SET Quantity = @Quantity
                    WHERE CartId = @CartId AND ProductId = @ProductId",
                    new SqlParameter("@CartId", cartId),
                    new SqlParameter("@ProductId", productId),
                    new SqlParameter("@Quantity", quantity)
                );

                if (rowsAffected == 0)
                {
                    _logger.LogWarning("No item found with ProductId {ProductId} in cart for user {UserId}",
                        productId, userId);
                    return false;
                }

                // Update cart timestamp
                _dbAccess.ExecuteNonQuery(
                    @"UPDATE Carts SET UpdatedAt = GETDATE() WHERE CartId = @CartId",
                    new SqlParameter("@CartId", cartId)
                );
                _logger.LogInformation("Updated quantity for product {ProductId} in cart for user {UserId}",
                    productId, userId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update product {ProductId} in cart for user {UserId}",
                    productId, userId);
                return false;
            }
        }


        public bool RemoveCartItem(int userId, int productId)
        {
            try
            {
                _logger.LogInformation("Removing product {ProductId} from cart for user {UserId}",
                    productId, userId);

                // Get Cart ID -- return false if cart does not exist
                var cartData = _dbAccess.ExecuteQuery(
                    @"SELECT CartId FROM Carts WHERE UserId = @UserId",
                    new SqlParameter("@UserId", userId)
                );

                if (cartData.Rows.Count == 0)
                {
                    _logger.LogWarning("Cannot remove item - cart not found for user {UserId}", userId);
                    return false;
                }

                int cartId = Convert.ToInt32(cartData.Rows[0]["CartId"]);

                // Now Delete the item
                int rowsDeleted = _dbAccess.ExecuteNonQueryReturn(
                    @"DELETE FROM CartItems 
                    WHERE CartId = @CartId AND ProductId = @ProductId",
                    new SqlParameter("@CartId", cartId),
                    new SqlParameter("@ProductId", productId)
                );

                if (rowsDeleted == 0)
                {
                    _logger.LogInformation("No item found with ProductId {ProductId} in cart for user {UserId}",
                        productId, userId);
                    return false;
                }


                // Update cart timtestamp
                _dbAccess.ExecuteNonQuery(
                    @"UPDATE Carts SET UpdatedAt = GETDATE() WHERE CartId = @CartId",
                    new SqlParameter("@CartId", cartId)
                );

                _logger.LogInformation("Removed product {ProductId} from cart for user {UserId}",
                    productId, userId);
                return true;

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to remove product {ProductId} from cart for user {UserId}",
                    productId, userId);
                return false;
            }
        }


        public bool ClearCart(int userId)
        {
            try
            {
                _logger.LogInformation("Clearing cart for user {UserId}", userId);

                // Get CartId -- return false 
                int cartId = GetCartId(userId);
                if (cartId == -1)
                {
                    _logger.LogInformation("Could not find any cart for userId {userId}", userId);
                    return false;
                }
                else if (cartId == -2)
                {
                    _logger.LogInformation("Failed to return cart for userId {userId}", userId);
                    return false;
                }

                // Delete all items in the cart
                _dbAccess.ExecuteNonQuery(
                    @"DELETE FROM CartItems WHERE CartId = @CartId",
                    new SqlParameter("@CartId", cartId)
                );

                // Update Cart timestamp
                _dbAccess.ExecuteNonQuery(
                    @"UPDATE Carts SET UpdatedAt = GETDATE() WHERE CartId = @CartId",
                    new SqlParameter("@CartId", cartId)
                );

                _logger.LogInformation("Cleared all items from cart for user {UserId}", userId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to clear cart for user {UserId}", userId);
                return false;
            }
        }

        private int GetCartId(int userId)
        {
            try
            {
                _logger.LogInformation("Getting CartId for user {userId}", userId);
                DataTable cartData = _dbAccess.ExecuteQuery(
                    @"SELECT CartId FROM Carts WHERE UserId = @UserId",
                    new SqlParameter("@UserId", userId)
                );

                if (cartData.Rows.Count == 0)
                {
                    _logger.LogInformation("Could not find any cart for user {userId}", userId);
                    return -1;
                }

                return Convert.ToInt32(cartData.Rows[0]["CartId"]);

            }
            catch (Exception ex)
            {
                _logger.LogError("Failed to retrieve the cartId for user {userId}", userId);
                return -2;
            }
        }

        private int EnsureCartExists(int userId)
        {
            var cartData = _dbAccess.ExecuteQuery(
                "SELECT CartId FROM Carts WHERE UserId = @UserId",
                new SqlParameter("@UserId", userId)
            );

            if (cartData.Rows.Count > 0)
            {
                return Convert.ToInt32(cartData.Rows[0]["CartId"]);
            }

            // Create new cart
            return _dbAccess.ExecuteScalar<int>(
                @"INSERT INTO Carts (UserId, CreatedAt, UpdatedAt) 
                VALUES (@UserId, GETDATE(), GETDATE());
                SELECT SCOPE_IDENTITY();",
                new SqlParameter("@UserId", userId)
            );
        }


        public bool InsertProduct(Product product)
        {
            try
            {
                _logger.LogInformation("Adding following product {product} to db", product);

                _dbAccess.ExecuteNonQuery(
                    @"INSERT INTO Products (Name, Description, Category, Price, Stock, ProductGuid, CreatedAt, UpdatedAt)
                    VALUES (@Name, @Description, @Category, @Price, @Stock, @ProductGuid, GETDATE(), GETDATE())",
                    new SqlParameter("@Name", product.ProductName),
                    new SqlParameter("@Description", product.ProductDescription),
                    new SqlParameter("@Category", product.ProductCategory),
                    new SqlParameter("@Price", product.ProductPrice),
                    new SqlParameter("@Stock", product.ProductQuantity),
                    new SqlParameter("@ProductGuid", product.ProductGuid)
                );

                _logger.LogInformation("Added product {name} to db", product.ProductName);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to add product {product} to db", product);
                return false;
            }
        }

        // Update product in DB -- To do create a new method to update single fields
        public bool UpdateProduct(Product product, int productId)
        {
            try
            {
                _logger.LogInformation("Updating product with name {name} and id {id}", product.ProductName, productId);
                int rowsAffected = _dbAccess.ExecuteNonQueryReturn(@"
            UPDATE Products 
            SET Name = @Name,
                Description = @Description,
                Category = @Category,
                Price = @Price,
                Stock = @Stock,
                UpdatedAt = GETDATE()
            WHERE ProductId = @ProductId",
                    new SqlParameter("@Name", product.ProductName),
                    new SqlParameter("@Description", product.ProductDescription),
                    new SqlParameter("@Category", product.ProductCategory),
                    new SqlParameter("@Price", product.ProductPrice),
                    new SqlParameter("@Stock", product.ProductQuantity),
                    new SqlParameter("@ProductId", productId)
                );

                if (rowsAffected == 0)
                {
                    _logger.LogWarning("No product found with id {id} to update", productId);
                    return false;
                }

                _logger.LogInformation("Successfully updated product {name} - {id}", product.ProductName, productId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update product {name} - {id}", product.ProductName, productId);
                return false;
            }
        }

        public int GetProductIdFromGuid(Guid productGuid)
        {
            try
            {
                _logger.LogInformation("Retrieving ProductId for ProductGuid: {productGuid}", productGuid);
                var productId = _dbAccess.ExecuteScalar<int>(
                    "SELECT ProductId FROM Products WHERE ProductGuid = @ProductGuid",
                    new SqlParameter("@ProductGuid", productGuid)
                );
                return productId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve ProductId for ProductGuid: {productGuid}", productGuid);
                return -1;
            }
        }


        /// <summary>
        /// Retrieves user data by email address
        /// </summary>
        /// <param name="email">Email address to search for</param>
        /// <returns>DataTable containing user data, or empty DataTable if not found</returns>
        public DataTable GetUserByEmail(string email)
        {
            try
            {
                _logger?.LogInformation("Retrieving user data for email: {Email}", email);

                var userData = _dbAccess.ExecuteQuery(
                    @"SELECT UserId, Name, Email, PasswordHash, Role, UserCreateTime, UserUpdateTime 
                    FROM Users 
                    WHERE Email = @Email",
                    new SqlParameter("@Email", email)
                );

                _logger?.LogInformation("Retrieved {RowCount} user records for email: {Email}", 
                    userData.Rows.Count, email);

                return userData;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error retrieving user data for email: {Email}", email);
                return new DataTable(); // Return empty DataTable on error
            }
        }

        /// <summary>
        /// Retrieves user data by user ID
        /// </summary>
        /// <param name="userId">User ID to search for</param>
        /// <returns>DataTable containing user data, or empty DataTable if not found</returns>
        public DataTable GetUserById(int userId)
        {
            try
            {
                _logger?.LogInformation("Retrieving user data for ID: {UserId}", userId);

                var userData = _dbAccess.ExecuteQuery(
                    @"SELECT UserId, Name, Email, PasswordHash, Role, UserCreateTime, UserUpdateTime 
                    FROM Users 
                    WHERE UserId = @UserId",
                    new SqlParameter("@UserId", userId)
                );

                _logger?.LogInformation("Retrieved {RowCount} user records for ID: {UserId}", 
                    userData.Rows.Count, userId);

                return userData;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error retrieving user data for ID: {UserId}", userId);
                return new DataTable(); // Return empty DataTable on error
            }
        }

        /// <summary>
        /// Converts DataTable row to User model object
        /// </summary>
        /// <param name="userRow">DataRow containing user data</param>
        /// <returns>User object or null if conversion fails</returns>
        public User? ConvertToUserModel(DataRow userRow)
        {
            try
            {
                if (userRow == null)
                {
                    _logger?.LogWarning("Cannot convert null DataRow to User model");
                    return null;
                }

                // Extract data from DataRow
                int userId = Convert.ToInt32(userRow["UserId"]);
                string name = userRow["Name"]?.ToString() ?? string.Empty;
                string email = userRow["Email"]?.ToString() ?? string.Empty;
                string passwordHash = userRow["PasswordHash"]?.ToString() ?? string.Empty;
                
                // Parse role - assuming it's stored as string in DB
                UserRole role = UserRole.Regular; // Default value
                if (userRow["Role"] != null && userRow["Role"] != DBNull.Value)
                {
                    string roleString = userRow["Role"].ToString();
                    if (Enum.TryParse<UserRole>(roleString, true, out UserRole parsedRole))
                    {
                        role = parsedRole;
                    }
                }

                // Parse timestamps if they exist
                DateTime? createTime = null;
                DateTime? updateTime = null;

                if (userRow["UserCreateTime"] != null && userRow["UserCreateTime"] != DBNull.Value)
                {
                    createTime = Convert.ToDateTime(userRow["UserCreateTime"]);
                }

                if (userRow["UserUpdateTime"] != null && userRow["UserUpdateTime"] != DBNull.Value)
                {
                    updateTime = Convert.ToDateTime(userRow["UserUpdateTime"]);
                }

                // Create User object
                var user = new User(userId,name, email, passwordHash, role);

                
                _logger?.LogDebug("Successfully converted DataRow to User model for email: {Email}", email);
                return user;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error converting DataRow to User model");
                return null;
            }
        }

        /// <summary>
        /// Gets a User model object by email
        /// </summary>
        /// <param name="email">Email address to search for</param>
        /// <returns>User object or null if not found</returns>
        public User? GetUserModelByEmail(string email)
        {
            try
            {
                var userData = GetUserByEmail(email);
                
                if (userData.Rows.Count == 0)
                {
                    _logger?.LogInformation("No user found with email: {Email}", email);
                    return null;
                }

                return ConvertToUserModel(userData.Rows[0]);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error getting User model by email: {Email}", email);
                return null;
            }
        }

        /// <summary>
        /// Gets a User model object by ID
        /// </summary>
        /// <param name="userId">User ID to search for</param>
        /// <returns>User object or null if not found</returns>
        public User? GetUserModelById(int userId)
        {
            try
            {
                var userData = GetUserById(userId);
                
                if (userData.Rows.Count == 0)
                {
                    _logger?.LogInformation("No user found with ID: {UserId}", userId);
                    return null;
                }

                return ConvertToUserModel(userData.Rows[0]);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error getting User model by ID: {UserId}", userId);
                return null;
            }
        }
    }
}