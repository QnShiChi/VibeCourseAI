using System.Net.Http.Headers;
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

    public async Task<byte[]?> FetchImageForSlideAsync(string keyword)
    {
        if (string.IsNullOrWhiteSpace(keyword)) return null;

        var unsplashKey = _configuration["UNSPLASH_API_KEY"];
        if (!string.IsNullOrEmpty(unsplashKey))
        {
            _logger.LogInformation($"Trying Unsplash for {keyword}");
            try
            {
                var url = $"https://api.unsplash.com/search/photos?query={Uri.EscapeDataString(keyword)}&per_page=1";
                var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Add("Authorization", $"Client-ID {unsplashKey}");
                request.Headers.Add("User-Agent", "Mozilla/5.0");

                var response = await _httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();
                var json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                var results = doc.RootElement.GetProperty("results");
                if (results.GetArrayLength() > 0)
                {
                    var imgUrl = results[0].GetProperty("urls").GetProperty("regular").GetString();
                    if (imgUrl != null)
                    {
                        return await _httpClient.GetByteArrayAsync(imgUrl);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Unsplash failed for {keyword}: {ex.Message}");
            }
        }

        var pexelsKey = _configuration["PEXELS_API_KEY"];
        if (!string.IsNullOrEmpty(pexelsKey))
        {
            _logger.LogInformation($"Trying Pexels for {keyword}");
            try
            {
                var url = $"https://api.pexels.com/v1/search?query={Uri.EscapeDataString(keyword)}&per_page=1";
                var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Add("Authorization", pexelsKey);
                request.Headers.Add("User-Agent", "Mozilla/5.0");

                var response = await _httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();
                var json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                var photos = doc.RootElement.GetProperty("photos");
                if (photos.GetArrayLength() > 0)
                {
                    var imgUrl = photos[0].GetProperty("src").GetProperty("medium").GetString();
                    if (imgUrl != null)
                    {
                        return await _httpClient.GetByteArrayAsync(imgUrl);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Pexels failed for {keyword}: {ex.Message}");
            }
        }

        var pixabayKey = _configuration["PIXABAY_API_KEY"];
        if (!string.IsNullOrEmpty(pixabayKey))
        {
            _logger.LogInformation($"Trying Pixabay for {keyword}");
            try
            {
                var url = $"https://pixabay.com/api/?key={pixabayKey}&q={Uri.EscapeDataString(keyword)}&image_type=photo&per_page=3";
                var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Add("User-Agent", "Mozilla/5.0");

                var response = await _httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();
                var json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                var hits = doc.RootElement.GetProperty("hits");
                if (hits.GetArrayLength() > 0)
                {
                    var imgUrl = hits[0].GetProperty("webformatURL").GetString();
                    if (imgUrl != null)
                    {
                        return await _httpClient.GetByteArrayAsync(imgUrl);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Pixabay failed for {keyword}: {ex.Message}");
            }
        }

        try
        {
            _logger.LogInformation($"Trying Pollinations AI for {keyword}");
            var url = $"https://image.pollinations.ai/prompt/{Uri.EscapeDataString(keyword)}_abstract_education_style_flat?width=480&height=520&nologo=true";
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("User-Agent", "Mozilla/5.0");

            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();
            var data = await response.Content.ReadAsByteArrayAsync();
            await Task.Delay(1500); // Sleep briefly to avoid aggressive rate limiting
            return data;
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"Pollinations AI failed for {keyword}: {ex.Message}");
        }

        _logger.LogInformation($"Falling back to LoremFlickr for {keyword}");
        try
        {
            var simpleKeyword = keyword.Split(' ').FirstOrDefault() ?? "education";
            var url = $"https://loremflickr.com/480/520/{Uri.EscapeDataString(simpleKeyword)},abstract/all";
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("User-Agent", "Mozilla/5.0");

            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsByteArrayAsync();
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"LoremFlickr failed for {keyword}: {ex.Message}");
        }

        _logger.LogInformation($"Final fallback to dummy text image for {keyword}");
        try
        {
            var url = $"https://dummyimage.com/480x520/1e1e2f/7b61ff.png&text={Uri.EscapeDataString(keyword)}";
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("User-Agent", "Mozilla/5.0");

            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsByteArrayAsync();
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"Dummy fallback failed for {keyword}: {ex.Message}");
            return null;
        }
    }
}
