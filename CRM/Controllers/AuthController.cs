using CRM.Infra;
using Microsoft.AspNetCore.Mvc;
using BCrypt.Net;
using CRM.Infra.Authentication;
using CRM.Models;
using Microsoft.AspNetCore.Authorization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;


namespace CRM.Controllers
{

    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {

        private readonly BasicCrud _basicCrud;
        private readonly ILogger<AuthController> _logger;
        private readonly JwtService _jwtService;
        private readonly ITokenBlacklistService _tokenBlacklistService;

        public AuthController(BasicCrud basicCrud, ILogger<AuthController> logger, JwtService jwtService, ITokenBlacklistService tokenBlacklistService)
        {
            _basicCrud = basicCrud;
            _logger = logger;
            _jwtService = jwtService;
            _tokenBlacklistService = tokenBlacklistService;
        }

        public class LoginRequest
        {
            public string Email { get; set; }
            public string Password { get; set; }
        }


        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginRequest request)
        {
            try
            {
                _logger.LogInformation("Starting login flow for user {email}", request.Email);
                if (string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.Password))
                {
                    _logger.LogError("No user email or password were provided");
                    return BadRequest("Email and password are required");
                }

                // Check if user exists
                if (!_basicCrud.CheckIfValueExists("Users", "Email", request.Email))
                {
                    _logger.LogWarning("Login Attempt made with non-existent email address {email}", request.Email);
                    return Unauthorized("Invalid email address provided");
                }

                // Get userData from DB.
                User userData = _basicCrud.GetUserModelByEmail(request.Email);

                if (userData == null)
                {
                    _logger.LogError("Failed to get user data from db for user {email}", request.Email);
                    return BadRequest($"Failed to get user data from db for {request.Email}");
                }

                // Verify password
                if (!BCrypt.Net.BCrypt.Verify(request.Password, userData.Password))
                {
                    _logger.LogWarning("Failed loging attempt incorrect password provided for user {email}", request.Email);
                    return Unauthorized("Invalid Email or password");
                }

                // Generate the token JWT token
                string token = _jwtService.GenerateToken(userData.Id, userData.Email, userData.Name, userData.Role.ToString());

                _logger.LogInformation("User {email} logged in successfully", request.Email);

                return Ok(new
                {
                    Token = token,
                    User = new
                    {
                        Id = userData.Id,
                        Name = userData.Name,
                        Email = userData.Email,
                        Role = userData.Role.ToString()
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login for email: {Email}", request.Email);
                return StatusCode(500, "An error occurred during login");
            }
        }


        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            try
            {

                _logger.LogInformation("Starting log out controller flow");
                // Get token from request
                var token = HttpContext.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();
                if (string.IsNullOrEmpty(token))
                {
                    _logger.LogError("Failed to logout since no token was provided");
                    return BadRequest("No Token Provided");
                }

                // Parse token to get jti and expiry
                var handler = new JwtSecurityTokenHandler();
                var jsonToken = handler.ReadJwtToken(token);

                var jti = jsonToken.Claims.FirstOrDefault(x => x.Type == JwtRegisteredClaimNames.Jti)?.Value;
                var expiry = jsonToken.ValidTo;

                if (string.IsNullOrEmpty(jti))
                {
                    _logger.LogError("Failed logout due to invalid token");
                    return BadRequest("Invalid token passed for logout");
                }

                // Blacklist the token
                await _tokenBlacklistService.BlackListTokenAsync(jti, expiry);

                // Get user info for logging

                var userEmail = HttpContext.User.FindFirst(ClaimTypes.Email)?.Value;
                var userId = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                _logger.LogInformation("User {Email} (ID: {UserId}) logged out successfully", userEmail, userId);

                return Ok(new { Message = "Logged out successfully" });

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during logout");
                return StatusCode(500, "An error occurred during logout");
            }
        }
    }

}