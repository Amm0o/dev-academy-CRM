using System.Collections.Concurrent;

namespace CRM.Infra.Authentication
{
    public interface ITokenBlacklistService
    {
        Task BlackListTokenAsync(string jti, DateTime expiry);
        Task<bool> IsTokenBlacklistedAsync(string jti);
        Task CleanupExpiredTokenAsync();
    }

    public class TokenBlacklistService : ITokenBlacklistService
    {

        // To do: Make this in redis or SQL db instead of in-memory storage
        private readonly ConcurrentDictionary<string, DateTime> _blacklistedTokens = new();
        private readonly ILogger<TokenBlacklistService> _logger;

        public TokenBlacklistService(ILogger<TokenBlacklistService> logger)
        {
            _logger = logger;
        }

        public Task BlackListTokenAsync(string jti, DateTime expiry)
        {
            _blacklistedTokens.TryAdd(jti, expiry);
            _logger.LogInformation("Token {Jti} blacklisted until {Expiry}", jti, expiry);
            return Task.CompletedTask;
        }

        public Task<bool> IsTokenBlacklistedAsync(string jti)
        {
            return Task.FromResult(_blacklistedTokens.ContainsKey(jti));
        }

        public Task CleanupExpiredTokenAsync()
        {
            var now = DateTime.UtcNow;
            var expiredTokens = _blacklistedTokens
                .Where(kvp => kvp.Value < now)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var token in expiredTokens)
            {
                _blacklistedTokens.TryRemove(token, out _);
            }

            _logger.LogInformation("Cleaned up {Count} expired blacklisted tokens", expiredTokens.Count);
            return Task.CompletedTask;
        }
        
    }
}