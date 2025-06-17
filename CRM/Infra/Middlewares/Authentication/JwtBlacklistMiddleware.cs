using CRM.Infra.Authentication;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace CRM.Infra.Middlewares.Authentication
{
    public class JwtBlacklistMiddleware
    {
        private readonly ILogger<JwtBlacklistMiddleware> _logger;
        private readonly RequestDelegate _next;
        private readonly ITokenBlacklistService _blacklistService;


        public JwtBlacklistMiddleware(RequestDelegate next, ITokenBlacklistService blacklistService, ILogger<JwtBlacklistMiddleware> logger)
        {
            _next = next;
            _blacklistService = blacklistService;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var token = context.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();

            if (!string.IsNullOrEmpty(token))
            {
                var jti = GetJtiFromToken(token);
                if (!string.IsNullOrEmpty(jti) && await _blacklistService.IsTokenBlacklistedAsync(jti))
                {
                    _logger.LogWarning("Blacklisted token attempted to be used: {Jti}", jti);
                    context.Response.StatusCode = 401;
                    await context.Response.WriteAsync("Token has been invalidated");
                    return;
                }
            }

            await _next(context);
        }

        private string GetJtiFromToken(string token)
        {
            try
            {
                var handler = new JwtSecurityTokenHandler();
                var jsonToken = handler.ReadJwtToken(token);
                return jsonToken.Claims.FirstOrDefault(x => x.Type == JwtRegisteredClaimNames.Jti)?.Value;
            }
            catch
            {
                return null;
            }
        }
    }
}