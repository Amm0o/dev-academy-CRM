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

    public class UserRegistrationRequest
    {
        public string Name { get; set; }
        public string Email { get; set; }
        public string Password { get; set; } // Plain password from client
    }

    [HttpPost("register")] 
    public IActionResult RegisterUser([FromBody] UserRegistrationRequest request)
    {
            // Example post request
            // {
            //     "name": "John Doe",
            //     "email": "john.doe@example.com",
            //     "password": "securePassword123"
            // }
            try {

                // Validate
                if (string.IsNullOrEmpty(request.Name) || string.IsNullOrEmpty(request.Email) || 
                    string.IsNullOrWhiteSpace(request.Password)) {
                    return BadRequest("User name email and password must not be empty");
                }

                // Check if email already exists in DB
                if (_basicCrud.CheckIfValueExists(request.Email)) {
                    _logger?.LogInformation("User emails {email} was already registered", request.Email);
                    return Conflict("User email was already registered");
                }


                // Proceed with saving the user since the user is not present in the DB

                // Hash password
                string hashedPassword = BCrypt.Net.BCrypt.HashPassword(request.Password);
                        // Create User object with hashed password
                var user = new User(request.Name, request.Email, hashedPassword);

                // Insert into DB
                // Insert user into database
                _basicCrud.RegisterUser(user, hashedPassword);

                var userId = _basicCrud.GetUserIdFromMail(user.Email);

                if (userId == -1)
                {
                    _logger.LogError("Failed to get userId using email: {email}", user.Email);
                    return StatusCode(500, "Failed to get userId using email");
                }

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
                _logger.LogError(ex, "Error registering user: {Email}", request.Email);
                return StatusCode(500, "An error occurred while registering the user");
            }
        }

        [HttpGet("{id}")]
        public IActionResult GetUserById(int id)
        {
            try
            {
                // Query the database for the user
                try
                {

                    var userTable = _basicCrud.GetUserFromId(id);
                    if (userTable.Rows.Count == 0)
                    {
                        _logger.LogInformation("Did not find any user with Id: {id}", id);
                        return NotFound($"User with ID {id} not found");
                    }

                    // Create user object from the data row
                    var userRow = userTable.Rows[0];
                    var user = new
                    {
                        Id = Convert.ToInt32(userRow["UserId"]),
                        Name = userRow["Name"].ToString(),
                        Email = userRow["Email"].ToString(),
                        CreatedAt = Convert.ToDateTime(userRow["CreatedAt"])
                    };

                    _logger.LogInformation("Retrieved user: {userdata}", userTable);

                    return Ok(user);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex.Message, "Failed to get user by ID", id);
                    return StatusCode(500, "An error occured while getting user by Id");
                }


            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user with ID: {UserId}", id);
                return StatusCode(500, "An error occurred while retrieving the user");
            }
        }
    }
}