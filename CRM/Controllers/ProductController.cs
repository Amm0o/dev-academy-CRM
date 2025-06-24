using CRM.Infra;
using CRM.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.AspNetCore.Authorization;


namespace CRM.Controllers
{

    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // Protect all endpoints from ProductController
    public class ProductController : ControllerBase
    {
        private readonly BasicCrud _basicCrud;
        private readonly ILogger<ProductController> _logger;

        public ProductController(BasicCrud basicCrud, ILogger<ProductController> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _basicCrud = basicCrud ?? throw new ArgumentNullException(nameof(basicCrud));
        }

        // GET: api/product/{productId}
        [HttpGet("{productId}")]
        public IActionResult GetProduct(int productId)
        {

            // validate if the product exists
            try
            {
                _logger.LogInformation("Checking if the product {pid} exists", productId);

                if (!_basicCrud.CheckIfProductExists(productId))
                {
                    _logger.LogError("Product {pId} was not found", productId);
                    return NotFound($"Product {productId} was not found in db");
                }

                // Convert data table to object
                var row = _basicCrud.GetProductData(productId);
                var productRow = row.Rows[0];

                var product = new
                {
                    ProductId = Convert.ToInt32(productRow["ProductId"]),
                    ProductName = productRow["Name"].ToString(),
                    ProductPrice = Convert.ToDecimal(productRow["Price"]),
                    ProductStock = Convert.ToInt32(productRow["Stock"]),
                    UpdatedAt = Convert.ToDateTime(productRow["UpdatedAt"]),
                    CreatedAt = Convert.ToDateTime(productRow["CreatedAt"])
                };

                return Ok(product);
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Error while getting product {id}", productId);
                return StatusCode(500, $"Error occured while getting product {productId}");
            }
        }

        [HttpPost("add")]
        [Authorize(Roles = "Admin")]
        public IActionResult AddProduct([FromBody] ProductRequest request)
        {
            try
            {
                if (String.IsNullOrWhiteSpace(request.Name) || String.IsNullOrWhiteSpace(request.Description) || string.IsNullOrWhiteSpace(request.Category)
                    || request.Price <= 0 || request.Stock < 0)
                {
                    _logger.LogError("Failed to add product {product} because data provided was invalid", request);
                    return BadRequest("Data provided was invalid");
                }

                var product = Product.CreateNew(
                    request.Name,
                    request.Description,
                    request.Category,
                    request.Price,
                    request.Stock
                );

                bool success = _basicCrud.InsertProduct(product);

                if (!success)
                {
                    _logger.LogError("Failed to add product to DB!");
                    return StatusCode(500, "Failed to create the product");
                }

                int productId = _basicCrud.GetProductIdFromGuid(product.ProductGuid);
                if (productId == -1)
                {
                    _logger.LogError("Failed to retrieve ProductId for ProductGuid: {guid}", product.ProductGuid);
                    return StatusCode(500, "Failed to retrieve product ID");
                }

                return Ok(new
                {
                    Message = "Product created successfully",
                    ProductName = product.ProductName,
                    ProductGuid = product.ProductGuid,
                    ProductId = productId
                });
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Error while adding product to db {product}", request);
                return StatusCode(500, $"An error occurred while adding product to db");
            }
        }

        // Todo - Create route to update single fields
        [HttpPut("update/{productId}")]
        [Authorize(Roles = "Admin")]
        public IActionResult UpdateProduct([FromBody] ProductRequest request, int productId)
        {
            try
            {
                if (String.IsNullOrWhiteSpace(request.Name) || String.IsNullOrWhiteSpace(request.Description) || string.IsNullOrWhiteSpace(request.Category)
                        || request.Price <= 0 || request.Stock < 0
                    )
                {
                    _logger.LogError("Failed to update product {product} because data provided was invalid", request);
                    return StatusCode(500, "Data provided was invalid for product update");
                }

                var product = Product.CreateNew(
                    request.Name,
                    request.Description,
                    request.Category,
                    request.Price,
                    request.Stock
                );


                bool success = _basicCrud.UpdateProduct(product, productId);

                if (!success)
                {
                    _logger.LogError("Failed to update product {name} - {id}", product.ProductName, productId);
                    return StatusCode(500, $"Failed to update product {product.ProductName} - {productId}");
                }

                _logger.LogInformation("Updated product sucessfully: {name} - {id}", product.ProductName, productId);
                return Ok(new
                {
                    Message = "Updated product successfully",
                    NewProduct = product
                });

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update product {product}", request);
                return StatusCode(500, $"Failed to update the product {request}");
            }
        }

        public class ProductRequest
        {
            public string? Name { get; set; }
            public string? Description { get; set; }
            public string? Category { get; set; }
            public double Price { get; set; }
            public int Stock { get; set; }
        }
    }
}