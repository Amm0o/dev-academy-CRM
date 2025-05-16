using CRM.Infra;
using CRM.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;

namespace CRM.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OrderController : ControllerBase
    {
        private readonly DatabaseAccess _dbAccess;
        private readonly ILogger<OrderController> _logger;
        private readonly BasicCrud _basicCrud;

        // Constructor
        public OrderController(DatabaseAccess databaseAccess, ILogger<OrderController> logger, BasicCrud basicCrud)
        {
            _dbAccess = databaseAccess ?? throw new ArgumentNullException(nameof(databaseAccess));
            _logger = logger;
            _basicCrud = basicCrud ?? throw new ArgumentNullException(nameof(basicCrud));
        }

        // DTO for order creation
        public class CreateOrderRequest
        {
            public string UserNameOrder { get; set; }
            public int CustomerId { get; set; }
            public string OrderDescription { get; set; }
            public List<OrderItemDto> Items { get; set; } = new List<OrderItemDto>();

            public class OrderItemDto
            {
                public int ProductId { get; set; }
                public int Quantity { get; set; }
            }
        }

        [HttpPost]
        public IActionResult CreateOrder([FromBody] CreateOrderRequest request)
        {
            try
            {
                // Validate basic request data
                if (string.IsNullOrWhiteSpace(request.UserNameOrder) || request.CustomerId <= 0 || request.Items.Count == 0)
                {
                    _logger?.LogError("Invalid order request: Missing required fields");
                    return BadRequest("Order must contain a valid username, customer ID, and at least one item");
                }

                if (!_basicCrud.CheckIfValueExists("Customers", "Email", request.UserNameOrder))
                {
                    _logger?.LogError("Customer with ID {CustomerId} does not exist", request.CustomerId);
                    return NotFound($"Customer with ID {request.CustomerId} not found");
                }


                // Create new Order object
                var order = new Order(
                    request.UserNameOrder,
                    request.CustomerId,
                    request.OrderDescription
                );

                // Validate and add items to order
                decimal orderTotal = 0;
                foreach (var item in request.Items)
                {
                    
                    // Validate that the product exists
                    if (!_basicCrud.CheckIfProductExists(item.ProductId))
                    {
                        _logger?.LogError("Product with ID {ProductId} not found", item.ProductId);
                        return NotFound($"Product with ID {item.ProductId} not found");
                    }

                    var productRow = productData.Rows[0];
                    int stock = Convert.ToInt32(productRow["Stock"]);
                    decimal price = Convert.ToDecimal(productRow["Price"]);
                    string productName = productRow["Name"].ToString();

                    // Check if product has enough stock
                    if (stock < item.Quantity)
                    {
                        _logger?.LogError("Not enough stock for product {ProductName} (ID: {ProductId}). Requested: {Quantity}, Available: {Stock}",
                            productName, item.ProductId, item.Quantity, stock);
                        return BadRequest($"Not enough stock for product {productName}. Requested: {item.Quantity}, Available: {stock}");
                    }

                    // Add item to order
                    order.AddItem(item.ProductId, item.Quantity, price);
                    orderTotal += price * item.Quantity;
                }

                // Begin transaction
                using (var connection = new SqlConnection(_dbAccess.GetConnectionString()))
                {
                    connection.Open();
                    using (var transaction = connection.BeginTransaction())
                    {
                        try
                        {
                            // Insert order record
                            var orderId = _dbAccess.ExecuteScalar<int>(
                                @"INSERT INTO Orders (OrderGuid, CustomerId, UserNameOrder, OrderDescription, TotalAmount, Status)
                                  VALUES (@OrderGuid, @CustomerId, @UserNameOrder, @OrderDescription, @TotalAmount, @Status);
                                  SELECT SCOPE_IDENTITY();",
                                new SqlParameter("@OrderGuid", order.OrderGuid),
                                new SqlParameter("@CustomerId", order.CustomerId),
                                new SqlParameter("@UserNameOrder", order.UserNameOrder),
                                new SqlParameter("@OrderDescription", order.OrderDescription ?? string.Empty),
                                new SqlParameter("@TotalAmount", order.TotalAmount),
                                new SqlParameter("@Status", order.Status)
                            );

                            // Insert order items
                            foreach (var item in order.Items)
                            {
                                _dbAccess.ExecuteNonQuery(
                                    @"INSERT INTO OrderItems (OrderId, ProductId, Quantity, UnitPrice)
                                      VALUES (@OrderId, @ProductId, @Quantity, @UnitPrice);
                                      
                                      -- Update product stock
                                      UPDATE Products 
                                      SET Stock = Stock - @Quantity
                                      WHERE ProductId = @ProductId;",
                                    new SqlParameter("@OrderId", orderId),
                                    new SqlParameter("@ProductId", item.ProductId),
                                    new SqlParameter("@Quantity", item.Quantity),
                                    new SqlParameter("@UnitPrice", item.UnitPrice)
                                );
                            }

                            // Commit transaction
                            transaction.Commit();

                            _logger?.LogInformation("Order {OrderGuid} created successfully for customer {CustomerId}", 
                                order.OrderGuid, order.CustomerId);

                            // Return success with order details
                            return CreatedAtAction(
                                nameof(GetOrder), 
                                new { orderGuid = order.OrderGuid.ToString() }, 
                                new { 
                                    OrderGuid = order.OrderGuid, 
                                    CustomerId = order.CustomerId,
                                    TotalAmount = order.TotalAmount,
                                    Status = order.Status,
                                    ItemCount = order.Items.Count
                                }
                            );
                        }
                        catch (Exception ex)
                        {
                            // Rollback on error
                            transaction.Rollback();
                            _logger?.LogError(ex, "Failed to create order: {Message}", ex.Message);
                            return StatusCode(500, "An error occurred while processing the order");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error in CreateOrder: {Message}", ex.Message);
                return StatusCode(500, "An unexpected error occurred");
            }
        }

        [HttpGet("{orderGuid}")]
        public IActionResult GetOrder(Guid orderGuid)
        {
            try
            {
                // Query order details
                var orderData = _dbAccess.ExecuteQuery(
                    @"SELECT o.OrderId, o.OrderGuid, o.CustomerId, o.UserNameOrder, 
                      o.OrderDescription, o.OrderDate, o.Status, o.TotalAmount
                      FROM Orders o 
                      WHERE o.OrderGuid = @OrderGuid",
                    new SqlParameter("@OrderGuid", orderGuid)
                );

                if (orderData.Rows.Count == 0)
                {
                    return NotFound($"Order with GUID {orderGuid} not found");
                }

                var orderRow = orderData.Rows[0];
                int orderId = Convert.ToInt32(orderRow["OrderId"]);

                // Query order items
                var itemsData = _dbAccess.ExecuteQuery(
                    @"SELECT oi.OrderItemId, oi.ProductId, p.Name AS ProductName, 
                      oi.Quantity, oi.UnitPrice, (oi.Quantity * oi.UnitPrice) AS LineTotal
                      FROM OrderItems oi
                      JOIN Products p ON oi.ProductId = p.ProductId
                      WHERE oi.OrderId = @OrderId",
                    new SqlParameter("@OrderId", orderId)
                );

                // Build order items list
                var items = new List<object>();
                foreach (DataRow itemRow in itemsData.Rows)
                {
                    items.Add(new
                    {
                        OrderItemId = Convert.ToInt32(itemRow["OrderItemId"]),
                        ProductId = Convert.ToInt32(itemRow["ProductId"]),
                        ProductName = itemRow["ProductName"].ToString(),
                        Quantity = Convert.ToInt32(itemRow["Quantity"]),
                        UnitPrice = Convert.ToDecimal(itemRow["UnitPrice"]),
                        LineTotal = Convert.ToDecimal(itemRow["LineTotal"])
                    });
                }

                // Return complete order
                var order = new
                {
                    OrderId = Convert.ToInt32(orderRow["OrderId"]),
                    OrderGuid = Guid.Parse(orderRow["OrderGuid"].ToString()),
                    CustomerId = Convert.ToInt32(orderRow["CustomerId"]),
                    UserNameOrder = orderRow["UserNameOrder"].ToString(),
                    OrderDescription = orderRow["OrderDescription"].ToString(),
                    OrderDate = Convert.ToDateTime(orderRow["OrderDate"]),
                    Status = orderRow["Status"].ToString(),
                    TotalAmount = Convert.ToDecimal(orderRow["TotalAmount"]),
                    Items = items
                };

                return Ok(order);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error retrieving order {OrderGuid}: {Message}", orderGuid, ex.Message);
                return StatusCode(500, "An error occurred while retrieving the order");
            }
        }

        [HttpGet("customer/{customerId}")]
        public IActionResult GetOrdersByCustomer(int customerId)
        {
            try
            {
                // Verify customer exists
                var customerExists = _dbAccess.ExecuteScalar<int>(
                    "SELECT COUNT(*) FROM Customers WHERE CustomerId = @CustomerId",
                    new SqlParameter("@CustomerId", customerId)
                );

                if (customerExists == 0)
                {
                    return NotFound($"Customer with ID {customerId} not found");
                }

                // Get orders
                var orderData = _dbAccess.ExecuteQuery(
                    @"SELECT OrderId, OrderGuid, UserNameOrder, OrderDate, Status, TotalAmount
                      FROM Orders 
                      WHERE CustomerId = @CustomerId
                      ORDER BY OrderDate DESC",
                    new SqlParameter("@CustomerId", customerId)
                );

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

                return Ok(orders);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error getting orders for customer {CustomerId}: {Message}", customerId, ex.Message);
                return StatusCode(500, "An error occurred while retrieving orders");
            }
        }
    }
}