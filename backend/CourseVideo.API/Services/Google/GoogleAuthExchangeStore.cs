using System.Security.Cryptography;
using CourseVideo.API.DTOs.Auth;
using Microsoft.Extensions.Caching.Memory;

namespace CourseVideo.API.Services.Google;

public class GoogleAuthExchangeStore
{
    private static readonly TimeSpan Expiry = TimeSpan.FromMinutes(2);
    private readonly IMemoryCache _cache;

    public GoogleAuthExchangeStore(IMemoryCache cache)
    {
        _cache = cache;
    }

    public string Store(AuthResponse response)
    {
        var exchangeToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        _cache.Set(BuildKey(exchangeToken), response, Expiry);
        return exchangeToken;
    }

    public AuthResponse? Take(string exchangeToken)
    {
        if (string.IsNullOrWhiteSpace(exchangeToken) || !_cache.TryGetValue<AuthResponse>(BuildKey(exchangeToken), out var response))
        {
            return null;
        }

        _cache.Remove(BuildKey(exchangeToken));
        return response;
    }

    private static string BuildKey(string exchangeToken) => $"google-auth-exchange:{exchangeToken}";
}
