using System.Text.Json;

namespace CourseVideo.API.Services.Video;

public class ImageProvider : IImageProvider
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ImageProvider> _logger;

    public ImageProvider(HttpClient httpClient, IConfiguration configuration, ILogger<ImageProvider> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<byte[]?> FetchImageForSlideAsync(string keyword, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(keyword)) return null;

        var unsplashKey = _configuration["UNSPLASH_API_KEY"];
        var pexelsKey = _configuration["PEXELS_API_KEY"];
        var pixabayKey = _configuration["PIXABAY_API_KEY"];

        if (string.IsNullOrWhiteSpace(unsplashKey) &&
            string.IsNullOrWhiteSpace(pexelsKey) &&
            string.IsNullOrWhiteSpace(pixabayKey))
        {
            _logger.LogInformation("No image provider API key configured. Rendering slide without remote illustration for keyword '{Keyword}'.", keyword);
            return null;
        }

        if (!string.IsNullOrEmpty(unsplashKey))
        {
            _logger.LogInformation("Trying Unsplash for {Keyword}", keyword);
            try
            {
                var url = $"https://api.unsplash.com/search/photos?query={Uri.EscapeDataString(keyword)}&per_page=1";
                var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Add("Authorization", $"Client-ID {unsplashKey}");
                request.Headers.Add("User-Agent", "Mozilla/5.0");

                var response = await _httpClient.SendAsync(request, cancellationToken);
                response.EnsureSuccessStatusCode();
                var json = await response.Content.ReadAsStringAsync(cancellationToken);
                using var doc = JsonDocument.Parse(json);
                var results = doc.RootElement.GetProperty("results");
                if (results.GetArrayLength() > 0)
                {
                    var imgUrl = results[0].GetProperty("urls").GetProperty("regular").GetString();
                    if (imgUrl != null)
                    {
                        return await _httpClient.GetByteArrayAsync(imgUrl, cancellationToken);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Unsplash failed for {Keyword}", keyword);
            }
        }

        if (!string.IsNullOrEmpty(pexelsKey))
        {
            _logger.LogInformation("Trying Pexels for {Keyword}", keyword);
            try
            {
                var url = $"https://api.pexels.com/v1/search?query={Uri.EscapeDataString(keyword)}&per_page=1";
                var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Add("Authorization", pexelsKey);
                request.Headers.Add("User-Agent", "Mozilla/5.0");

                var response = await _httpClient.SendAsync(request, cancellationToken);
                response.EnsureSuccessStatusCode();
                var json = await response.Content.ReadAsStringAsync(cancellationToken);
                using var doc = JsonDocument.Parse(json);
                var photos = doc.RootElement.GetProperty("photos");
                if (photos.GetArrayLength() > 0)
                {
                    var imgUrl = photos[0].GetProperty("src").GetProperty("medium").GetString();
                    if (imgUrl != null)
                    {
                        return await _httpClient.GetByteArrayAsync(imgUrl, cancellationToken);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Pexels failed for {Keyword}", keyword);
            }
        }

        if (!string.IsNullOrEmpty(pixabayKey))
        {
            _logger.LogInformation("Trying Pixabay for {Keyword}", keyword);
            try
            {
                var url = $"https://pixabay.com/api/?key={pixabayKey}&q={Uri.EscapeDataString(keyword)}&image_type=photo&per_page=3";
                var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Add("User-Agent", "Mozilla/5.0");

                var response = await _httpClient.SendAsync(request, cancellationToken);
                response.EnsureSuccessStatusCode();
                var json = await response.Content.ReadAsStringAsync(cancellationToken);
                using var doc = JsonDocument.Parse(json);
                var hits = doc.RootElement.GetProperty("hits");
                if (hits.GetArrayLength() > 0)
                {
                    var imgUrl = hits[0].GetProperty("webformatURL").GetString();
                    if (imgUrl != null)
                    {
                        return await _httpClient.GetByteArrayAsync(imgUrl, cancellationToken);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Pixabay failed for {Keyword}", keyword);
            }
        }

        _logger.LogInformation("No remote illustration available for keyword '{Keyword}'. Rendering slide without image.", keyword);
        return null;
    }
}
