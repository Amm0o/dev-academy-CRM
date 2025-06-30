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


        // Endpoint to retrieve all products
        [HttpGet]
        public IActionResult GetAllProducts()
        {
            try
            {
                _logger.LogInformation("Got request to grab all products from db!");

                var allProducts = _basicCrud.GetAllProducts();

                if (allProducts == null)
                {
                    _logger.LogWarning("Query to db returned no products");
                    return StatusCode(404, "No products were found!");
                }

                // Convert product dataRow to Product model
                var products = new List<Product>();
                foreach (DataRow row in allProducts.Rows)
                {
                    try
                    {
                        var product = Product.FromDatabase(row);
                        products.Add(product);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to parse product from DataRow to Product Model");
                        return BadRequest("Failed to parse product");
                    }
                }

                // Convert to products API response
                var productsResponse = products.Select(p => p.ToApiResponse()).ToList();

                _logger.LogInformation("Successfully got all products from DB and parse it to user model");
                return Ok(new
                {
                    message = "Successfully got all the products from DB and parsed them",
                   data = productsResponse 
                });

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error occurred while getting all the products");
                return StatusCode(500, "Unexpected error happened while retriving all the products");
            }
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


                // Get product data and convert to Product model
                var productData = _basicCrud.GetProductData(productId);
                
                if (productData.Rows.Count == 0)
                {
                    _logger.LogError("No data returned for product {pId}", productId);
                    return NotFound($"Product {productId} was not found");
                }

                var productRow = productData.Rows[0];
                var product = Product.FromDatabase(productRow);

                _logger.LogInformation("Retrieved product {name} from db", product.ProductName);
                return Ok(product.ToApiResponse());
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

                product.Id = productId;

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

                // Check if product exists
                if (!_basicCrud.CheckIfProductExists(productId))
                {
                    _logger.LogWarning("Trying to update product {id} failed because it does not exist");
                    return NotFound($"Product with {productId} was not update because it does not exist in db!");
                }

                // Get existing GUID to preserve it
                var existingData = _basicCrud.GetProductData(productId);
                var existingRow = existingData.Rows[0];
                var existingGuid = Guid.Parse(existingRow["ProductGuid"]?.ToString() ?? Guid.NewGuid().ToString());

                // Create updated product with existing GUID
                var product = new Product(
                    request.Name,
                    request.Description,
                    request.Price,
                    request.Stock,
                    request.Category,
                    existingGuid
                );

                product.Id = productId;

                bool success = _basicCrud.UpdateProduct(product, productId);

                if (!success)
                {
                    _logger.LogCritical("Failed to update product with id {id}, with new data {product}", productId, product);
                    return BadRequest("Failed to update product");
                }


                _logger.LogInformation("Updated product sucessfully: {name} - {id}", product.ProductName, productId);
                return Ok(new
                {
                    message = "Updated product successfully",
                    data = product
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