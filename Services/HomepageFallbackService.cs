using System.Text.Json;
using STBWeb.Models;

namespace STBWeb.Services;

// Temporary homepage verification fallback loader backed by scraped-data/homepage.json.
public sealed class HomepageFallbackService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly string _jsonPath;
    private readonly ILogger<HomepageFallbackService> _logger;
    private HomepageFallbackData? _cachedData;
    private bool _hasLoaded;

    public HomepageFallbackService(IWebHostEnvironment environment, ILogger<HomepageFallbackService> logger)
    {
        _jsonPath = Path.Combine(environment.ContentRootPath, "scraped-data", "homepage.json");
        _logger = logger;
    }

    public HomepageFallbackData GetData()
    {
        if (_hasLoaded)
        {
            return _cachedData ?? new HomepageFallbackData();
        }

        _hasLoaded = true;

        if (!File.Exists(_jsonPath))
        {
            _logger.LogInformation("Homepage fallback JSON not found at {Path}.", _jsonPath);
            _cachedData = new HomepageFallbackData();
            return _cachedData;
        }

        try
        {
            using var stream = File.OpenRead(_jsonPath);
            _cachedData = JsonSerializer.Deserialize<HomepageFallbackData>(stream, JsonOptions) ?? new HomepageFallbackData();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load homepage fallback JSON from {Path}.", _jsonPath);
            _cachedData = new HomepageFallbackData();
        }

        return _cachedData;
    }
}
