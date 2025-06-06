using CRM.Infra;
using CRM.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data;
using System.ComponentModel.DataAnnotations;

namespace CRM.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CartController : ControllerBase
    {
        private readonly BasicCrud _basicCrud;
        private readonly ILogger<CartController> _logger;


        // TO DO: Add checks to ensure both of the parameters are never null
        public CartController(BasicCrud basicCrud, ILogger<CartController> logger)
        {
            _basicCrud = basicCrud ?? throw new ArgumentNullException(nameof(basicCrud));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }


        // GET: api/cart/{userId}
        [HttpGet("{userId}")]
        public IActionResult GetCart(int userId)
        {
            try
            {
                // Validate that the user exists
                if (!_basicCrud.CustomerExists(userId))
                {
                    _logger.LogWarning("User {UserId} not found when attempting to get cart", userId);
                    return NotFound($"User with ID {userId} not found");
                }

                // Get cart or return empty if not exists
                var cart = _basicCrud.GetUserCart(userId);
                return Ok(cart);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving cart for user {UserId}", userId);
                return StatusCode(500, "An error occurred while retrieving the cart");
            }
        }


        // POST: api/cart/add
        [HttpPost("add")]
        public IActionResult AddToCart([FromBody] CartItemRequest request)
        {
            try
            {
                _logger.LogInformation("Received the following payload for route add to cart:\n {payload}", request);
                // Request validation
                if (request.UserId <= 0 || request.ProductId <= 0 || request.Quantity <= 0)
                {
                    _logger.LogError("Tried to add to cart using negative values for UserId, ProductId, and Quantity");
                    return BadRequest("UserId, ProductId, and Quantity must be positive values");
                }

                // Check if product exists and there's enough stock
                var productData = _basicCrud.GetProductData(request.ProductId);
                if (productData == null || productData.Rows.Count == 0)
                {
                    _logger.LogError("Could not find product {productId}", request.ProductId);
                    return NotFound($"Product with ID {request.ProductId} not found");
                }

                var productRow = productData.Rows[0];
                int stock = Convert.ToInt32(productRow["Stock"]);
                decimal price = Convert.ToDecimal(productRow["Price"]);

                if (stock < request.Quantity)
                {
                    _logger.LogWarning("Insufficient stock for product ID {ProductId}. Requested: {Quantity}, Available: {Stock}",
                            request.ProductId, request.Quantity, stock);
                    return BadRequest($"Not enough stock available. Requested: {request.Quantity}, Available: {stock}");
                }

                bool success = _basicCrud.AddItemToCart(request.UserId, request.ProductId, request.Quantity, price);

                if (!success)
                {
                    _logger.LogInformation("Failed to add item to cart for user {userId}", request.UserId);
                    return StatusCode(500, "Failed to add item to cart");
                }

                // If we succed return updated Cart
                var updatedCart = _basicCrud.GetUserCart(request.UserId);
                return Ok(updatedCart);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding product {ProductId} to cart for user {UserId}", 
                    request.ProductId, request.UserId);
                return StatusCode(500, "An error occurred while adding the item to cart");
            }
        }

        //api/cart/update
        [HttpPut("update")]
        public IActionResult UpdateCartItem([FromBody] CartItemRequest request)
        {
            try
            {
                // Implement quantity update logic
                if (request.UserId <= 0 || request.ProductId <= 0)
                {
                    _logger.LogError("REF User: {userId} - Request came in to update cartItem with negative values for ProductId: {productId} or UserId: {UserId}", request.UserId, request.ProductId, request.UserId);
                    return BadRequest("UserId and ProductId must be positive values");
                }

                if (request.Quantity < 0)
                {
                    _logger.LogError("REF User: {userId} - Request came in to update cart with negative value for quantity: {quantity}", request.UserId, request.Quantity);
                    return BadRequest("Quantity cannot be negative");
                }

                bool success = _basicCrud.UpdateCartItem(request.UserId, request.ProductId, request.Quantity);
                if (!success)
                {
                    return NotFound("Cart Item not found");
                }

                _logger.LogInformation("Updated cart succesfully for user, {userId}", request.UserId);
                // Return updated cart
                var updatedCart = _basicCrud.GetUserCart(request.UserId);
                return Ok(updatedCart);

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating product {ProductId} quantity in cart for user {UserId}",
                    request.ProductId, request.UserId);
                return StatusCode(500, "An error occurred while updating the cart item");
            }
        }

        // DELETE: api/cart/{userid}/item/{productId} -- Deletes a product from cart
        [HttpDelete("{userId}/item/{productId}")]
        public IActionResult RemoveCartItem(int userId, int productId)
        {
            try
            {
                bool success = _basicCrud.RemoveCartItem(userId, productId);
                if (!success)
                {
                    _logger.LogError("Error deleting productId {productId} for cart associated with customer {userId}", productId, userId);
                    return NotFound("Cart Item not found");
                }

                return Ok(new { Message = "Item removed from cart successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing product {ProductId} from cart for user {UserId}",
                    productId, userId);
                return StatusCode(500, "An error occurred while removing the item from cart");
            }
        }

        // DELETE api/cart/{userId} -- Clears entire cart
        [HttpDelete("{userId}")]
        public IActionResult ClearCart(int userId)
        {
            try
            {
                _logger.LogInformation("Clearing car for user {user}", userId);

                bool success = _basicCrud.ClearCart(userId);
                if (!success)
                {
                    _logger.LogError("Failed to clear cart for user {user} NOT FOUND", userId);
                    return NotFound($"Cart for user {userId} could not be found");
                }

                return Ok(new { Message = "Cart cleared successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing cart for user {UserId}", userId);
                return StatusCode(500, "An error occurred while clearing the cart");
            }
        }

            
        public class CartItemRequest
        {
            [Range(1, int.MaxValue, ErrorMessage = "UserId must be a positive value")]
            public int UserId { get; set; }
            
            [Range(1, int.MaxValue, ErrorMessage = "ProductId must be a positive value")]
            public int ProductId { get; set; }
            
            [Range(1, int.MaxValue, ErrorMessage = "Quantity must be a positive value")]
            public int Quantity { get; set; }
        }

    }
}