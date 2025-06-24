using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CRM.Controllers
{
    public abstract class BaseAuthController : ControllerBase
    {
        protected readonly ILogger<BaseAuthController> _baseLogger;

        protected BaseAuthController(ILogger<BaseAuthController> logger)
        {
            _baseLogger = logger;
        }

        protected (bool isAuthorized, int currentUserId, string errorMessage) CheckResourceOwnership(int resourceUserId)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

            if (!int.TryParse(userIdClaim, out int currentUserId))
            {
                _baseLogger.LogError("Failed to parse current userId from claims");
                return (false, 0, "Invalid User Authentication");
            }

            bool isAdmin = userRole == "Admin";
            bool isOwner = currentUserId == resourceUserId;

            if (!isOwner && !isAdmin)
            {
                _baseLogger.LogWarning("User {CurrentUserId} attempted to access resource owned by {ResourceUserId}",
                    currentUserId, resourceUserId);
                return (false, currentUserId, "You don't have permission to access this resource");
            }

            return (true, currentUserId, null);
        }

        protected IActionResult CheckAuthorizationAndExecute(int resourceUserId, Func<IActionResult> action)
        {
            var (isAuthorized, currentUserId, errorMessage) = CheckResourceOwnership(resourceUserId);

            if (!isAuthorized)
            {
                if (errorMessage == "Invalid User Authentication")
                    return Unauthorized(errorMessage);

                return StatusCode(403, new { message = errorMessage });
            }

            return action();
        }

        protected bool IsCurrentUserAdmin()
        {
            return User.FindFirst(ClaimTypes.Role)?.Value == "Admin";
        }

        protected int? GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(userIdClaim, out int userId) ? userId : null;
        }
    }
}