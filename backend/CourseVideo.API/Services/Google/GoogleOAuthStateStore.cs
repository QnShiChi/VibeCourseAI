using System.Security.Cryptography;
using Microsoft.Extensions.Caching.Memory;

namespace CourseVideo.API.Services.Google;

public class GoogleOAuthStateStore
{
    private static readonly TimeSpan Expiry = TimeSpan.FromMinutes(10);
    private readonly IMemoryCache _cache;

    public GoogleOAuthStateStore(IMemoryCache cache)
    {
        _cache = cache;
    }

    public string Create()
    {
        var state = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        _cache.Set(BuildKey(state), true, Expiry);
        return state;
    }

    public bool Consume(string state)
    {
        if (string.IsNullOrWhiteSpace(state) || !_cache.TryGetValue(BuildKey(state), out _))
        {
            return false;
        }

        _cache.Remove(BuildKey(state));
        return true;
    }

    private static string BuildKey(string state) => $"google-oauth-state:{state}";
}
