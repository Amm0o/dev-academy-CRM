using CRM.Infra;
using CRM.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using BCrypt.Net;
using Microsoft.Data.SqlClient; // Add this for BCrypt

namespace CRM.Controllers {

    [ApiController]
    [Route("api/[controller]")]
    public class UserController : ControllerBase {


        private readonly DatabaseAccess _dbAccess;
        private readonly ILogger<UserController> _logger;
        private readonly BasicCrud _basicCrud;

        // DI Injecting dbAccess and logger
        public UserController (DatabaseAccess dbAccess, ILogger<UserController> logger, BasicCrud basicCrud) {
            _logger = logger; // Store the injected logger
            _dbAccess = dbAccess; 
            _basicCrud = basicCrud;
        }


        [HttpPost("register")] 
        public IActionResult RegisterUser([FromBody] User user)
        {
            // Example post request
            // {
            //     "name": "John Doe",
            //     "email": "john.doe@example.com",
            //     "password": "securePassword123"
            // }
            try {

                // Validate User input 
                if (string.IsNullOrEmpty(user.Name) || string.IsNullOrEmpty(user.Email) || string.IsNullOrWhiteSpace(user.PasswordHash)) {

                    return BadRequest("User name email and password must not be empty");
                }

                // Check if email already exists in DB
                if (_basicCrud.CheckIfValueExists(user.Email)) {
                    _logger?.LogInformation("User emails {email} was already registered", user.Email);
                    return Conflict("User email was already registered");
                }


                // Proceed with saving the user since the user is not present in the DB

                // Hash password
                string hashedPassword = BCrypt.Net.BCrypt.HashPassword(user.PasswordHash);

                // Insert into DB
                // Insert user into database
                _dbAccess.ExecuteNonQuery(
                    @"INSERT INTO Users (Name, Email, PasswordHash, CreatedAt) 
                    VALUES (@Name, @Email, @PasswordHash, GETDATE());
                    SELECT SCOPE_IDENTITY();",
                    new Microsoft.Data.SqlClient.SqlParameter("@Name", user.Name),
                    new Microsoft.Data.SqlClient.SqlParameter("@Email", user.Email),
                    new Microsoft.Data.SqlClient.SqlParameter("@PasswordHash", hashedPassword)
                );

                var userId = _dbAccess.ExecuteScalar<int>(
                    $"SELECT UserId FROM Users WHERE Email = @email",
                    new SqlParameter("@email", user.Email)
                );

                // Create response object
                var createdUser = new {
                    Id = userId,
                    Name = user.Name,
                    Email = user.Email,
                    CreatedAt = DateTime.UtcNow
                };

                _logger.LogInformation($"User created: {user.Email} with ID: {userId}");

                return CreatedAtAction(nameof(GetUserById), new {id = userId}, createdUser);

            } catch (Exception ex) {
                _logger.LogError(ex, "Error registering user: {Email}", user.Email);
                return StatusCode(500, "An error occurred while registering the user");
            }
        }

        [HttpGet("{id}")]
        public IActionResult GetUserById(int id)
        {
            try
            {
                // Query the database for the user
                var userTable = _dbAccess.ExecuteQuery(
                    "SELECT UserId, Name, Email, CreatedAt FROM Users WHERE UserId = @UserId",
                    new Microsoft.Data.SqlClient.SqlParameter("@UserId", id)
                );

                if (userTable.Rows.Count == 0)
                {
                    return NotFound($"User with ID {id} not found");
                }

                // Create user object from the data row
                var userRow = userTable.Rows[0];
                var user = new {
                    Id = Convert.ToInt32(userRow["UserId"]),
                    Name = userRow["Name"].ToString(),
                    Email = userRow["Email"].ToString(),
                    CreatedAt = Convert.ToDateTime(userRow["CreatedAt"])
                };

                return Ok(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user with ID: {UserId}", id);
                return StatusCode(500, "An error occurred while retrieving the user");
            }
        }
    }
}