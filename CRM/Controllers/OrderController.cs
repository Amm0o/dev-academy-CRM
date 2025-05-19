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

                // Checking if the customer making the order exists so we can then associate the order to a customer and it's info
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

                    // Get product from db
                    var productData = _basicCrud.GetProductData(item.ProductId);
                    // Validate that the product exists
                    if ( productData== null)
                    {
                        _logger?.LogError("Product with ID {ProductId} not found", item.ProductId);
                        return NotFound($"Product with ID {item.ProductId} not found");
                    }

                    var productRow = productData.Rows[0];
                    int stock = Convert.ToInt32(productRow["Stock"]);
                    decimal price = Convert.ToDecimal(productRow["Price"]);
                    string? productName = productRow["Name"].ToString();

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

                // Add the order to the database
                try
                {
                    _logger.LogInformation("Initiating flow to store the order in DB");
                    // Insert the Order record
                    _basicCrud.InsertOrder(order);
                    _logger.LogInformation("Inserted the order");

                    return CreatedAtAction(
                                nameof(GetOrder),
                                new { orderGuid = order.OrderGuid.ToString() },
                                new
                                {
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
                    _logger.LogCritical("Failed to insert order: {orderGuid}", order.OrderGuid);
                    _logger?.LogError(ex, "Failed to create order: {Message}", ex.Message);
                    return StatusCode(500, "An error occurred while processing the order");
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
                var orderData = _basicCrud.GetOrderFromGuid(orderGuid);

                if (orderData.Rows.Count == 0)
                {
                    return NotFound($"Order with GUID {orderGuid} not found");
                }

                var orderRow = orderData.Rows[0];
                int orderId = Convert.ToInt32(orderRow["OrderId"]);

                // Query order items
                var itemsData = _basicCrud.GetAllOrederItems(orderId);

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
                var customerExists = _basicCrud.CustomerExists(customerId);

                if (!customerExists)
                {
                    _logger?.LogError("Failed to get customer: {CustomerId}", customerId);
                    return StatusCode(404, "An error occurred while retrieving orders");
                }

                // Get all orders
                var orders = _basicCrud.GetAllOrderForCustomer(customerId);

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