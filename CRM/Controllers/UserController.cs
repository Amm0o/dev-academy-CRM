using CRM.Infra;
using CRM.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using BCrypt.Net;
using Microsoft.Data.SqlClient;
using Microsoft.AspNetCore.Authorization; // Add this for BCrypt

namespace CRM.Controllers {

    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // Protect all endpoints from User controller
    public class UserController : ControllerBase
    {


        private readonly DatabaseAccess _dbAccess;
        private readonly ILogger<UserController> _logger;
        private readonly BasicCrud _basicCrud;

        // DI Injecting dbAccess and logger
        public UserController(DatabaseAccess dbAccess, ILogger<UserController> logger, BasicCrud basicCrud)
        {
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
        [AllowAnonymous] // Allow registration without authentication
        public IActionResult RegisterUser([FromBody] UserRegistrationRequest request)
        {
            // Example post request
            // {
            //     "name": "John Doe",
            //     "email": "john.doe@example.com",
            //     "password": "securePassword123"
            // }
            try
            {

                // Validate
                if (string.IsNullOrEmpty(request.Name) || string.IsNullOrEmpty(request.Email) ||
                    string.IsNullOrWhiteSpace(request.Password))
                {
                    return BadRequest("User name email and password must not be empty");
                }

                // Check if email already exists in DB
                // Check if email already exists in Users table
                if (_basicCrud.CheckIfValueExists("Users", "Email", request.Email))
                {
                    _logger?.LogInformation("User email {email} was already registered", request.Email);
                    return Conflict("User email was already registered");
                }


                // Proceed with saving the user since the user is not present in the DB

                // Hash password
                string hashedPassword = BCrypt.Net.BCrypt.HashPassword(request.Password);
                // Create User object with hashed password
                var user = new User(request.Name, request.Email, hashedPassword, UserRole.Regular);

                // Insert into DB
                // Insert user into database
                _basicCrud.RegisterUser(user, hashedPassword);

                var userId = _basicCrud.GetUserIdFromMail(user.Email);

                if (userId == -1)
                {
                    _logger.LogError("Failed to get userId using email: {email}", user.Email);
                    return StatusCode(500, "Failed to get userId using email");
                }

                _logger.LogInformation("Successfuly retuner id {id} for user email {email} after user creation", userId, user.Email);

                // Create response object
                var createdUser = new
                {
                    Id = userId,
                    Name = user.Name,
                    Email = user.Email,
                    Role = user.Role,
                    CreatedAt = DateTime.UtcNow
                };

                _logger.LogInformation($"User created: {user.Email} with ID: {userId}");

                return CreatedAtAction(nameof(GetUserById), new { id = userId }, createdUser);

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error registering user: {Email}", request.Email);
                return StatusCode(500, "An error occurred while registering the user");
            }
        }

        [HttpDelete("{userId}")]
        public IActionResult DeleteUser(int userId)
        {
            try
            {
                _logger.LogInformation("Attempting to delete user with ID {id}", userId);
                // Validation
                if (userId <= 0)
                {
                    _logger.LogError("Invalid userId {id} provided", userId);
                    return BadRequest("User ID is always a positive integer > 0");
                }

                var UserTableData = _basicCrud.GetUserFromId(userId);
                if (UserTableData.Rows.Count != 1)
                {
                    _logger.LogError("User with id {id} does not exist in db we cannot delete it", userId);
                    return NotFound($"User with id {userId}, is not present in db");
                }

                bool success = _basicCrud.DeleteUser(userId);
                if (!success)
                {
                    _logger.LogError("Failed to delete user with id {id}from DB!", userId);
                    return StatusCode(500, $"Failed to delete user with id {userId} from db!");
                }

                _logger.LogInformation("Delete user with Id {userId} from db!", userId);
                return Ok(new { Message = $"Delete user with {UserTableData.Rows[0]["Email"]}" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error occurred while deleting user with id {id}", userId);
                return StatusCode(500, $"Unexpected error occurred while deleting user with id {userId}");
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

        [HttpGet("email/{email}")]
        public IActionResult GetUserByEmail(string email)
        {
            try
            {

                _logger.LogInformation("Attempting to get user with email {email}", email);
                // Check if email is empty
                if (string.IsNullOrWhiteSpace(email))
                {
                    _logger.LogError("Email address must no be empty");
                    return BadRequest("Email must not be empty");
                }

                var userId = _basicCrud.GetUserIdFromMail(email);

                if (userId == -1)
                {
                    _logger.LogError("User with email {email} could not be found in db", email);
                    return NotFound($"User with email {email} was not found");
                }

                var userDataTable = _basicCrud.GetUserFromId(userId);

                if (userDataTable.Rows.Count != 1)
                {
                    _logger.LogError("No user found with email {email}", email);
                    return NotFound($"No user found with email {email}");
                }

                var userRow = userDataTable.Rows[0];

                var user = new
                {
                    Id = Convert.ToInt32(userRow["UserId"]),
                    Name = userRow["Name"].ToString(),
                    Email = userRow["Email"].ToString(),
                    CreatedAt = Convert.ToDateTime(userRow["CreatedAt"])
                };

                _logger.LogInformation("Retrieved user with email {email}", user.Email);

                return Ok(new
                {
                    Message = $"Found user in db with email {user.Email}",
                    Data = user
                });

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error occured while getting user with emai {email} from DB", email);
                return StatusCode(500, $"Unexpected error occured while retrieving user with email {email}");
            }
        }
    }
}