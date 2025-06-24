using CRM.Infra;
using CRM.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CRM.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class SetupController : BaseAuthController
    {
        private readonly BasicCrud _basicCrud;
        private readonly ILogger<SetupController> _logger;

        public SetupController(BasicCrud basicCrud, ILogger<SetupController> logger) : base(logger)
        {
            _basicCrud = basicCrud;
            _logger = logger;
        }

        [HttpPost("{email}")]
        [Authorize(Roles = "Admin")]
        public IActionResult PromoteToAdmin(string email)
        {
            try
            {
                _logger.LogInformation("Initiating flow to promote user with email {email} to admin", email);

                if (string.IsNullOrEmpty(email))
                {
                    _logger.LogError("Failed to promote user since email is not valid");
                    return BadRequest("Failed to promote user due to invalid email being provided");
                }

                // Check if user we are trying to promote exists
                if (!_basicCrud.CheckIfValueExists("Users", "Email", email))
                {
                    _logger.LogWarning("User does not exist in db therefore cannot be promorted");
                    return StatusCode(404, "User not found in db could not be promoted");
                }

                // Promote user
                if (!_basicCrud.PromoteUserToAdmin(email))
                {
                    _logger.LogError("Failed to promote user {email} to admin", email);
                    return StatusCode(500, $"Failed to promote user {email} to admin");
                }

                return Ok(new
                {
                    Message = "Update user: " + email + " to admin"
                });
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Unexpcted error occured while promoting user {email} to admin", email);
                return StatusCode(500, $"Failed to promote user {email} to admin");
            }
        }



    }
}