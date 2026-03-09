using System.Text.Json.Serialization;

namespace STBWeb.Models;

// Temporary homepage verification fallback contract backed by scraped-data/homepage.json.
public sealed class HomepageFallbackData
{
    [JsonPropertyName("heroSlides")]
    public List<HomepageFallbackHeroSlide> HeroSlides { get; init; } = [];

    [JsonPropertyName("featureIconLinks")]
    public List<HomepageFallbackFeatureIconLink> FeatureIconLinks { get; init; } = [];

    [JsonPropertyName("applyOnline")]
    public HomepageFallbackApplyOnline ApplyOnline { get; init; } = new();

    [JsonPropertyName("promoBanners")]
    public List<HomepageFallbackPromoBanner> PromoBanners { get; init; } = [];

    [JsonPropertyName("latestNews")]
    public List<HomepageFallbackNewsItem> LatestNews { get; init; } = [];
}

public sealed class HomepageFallbackHeroSlide
{
    [JsonPropertyName("slideHeading")]
    public string SlideHeading { get; init; } = string.Empty;

    [JsonPropertyName("slideSubheading")]
    public string SlideSubheading { get; init; } = string.Empty;

    [JsonPropertyName("slideCtaText")]
    public string SlideCtaText { get; init; } = string.Empty;

    [JsonPropertyName("slideCtaUrl")]
    public string SlideCtaUrl { get; init; } = string.Empty;

    [JsonPropertyName("desktopImageUrl")]
    public string DesktopImageUrl { get; init; } = string.Empty;

    [JsonPropertyName("mobileImageUrl")]
    public string MobileImageUrl { get; init; } = string.Empty;

    [JsonPropertyName("desktopImageLocalPath")]
    public string DesktopImageLocalPath { get; init; } = string.Empty;

    [JsonPropertyName("mobileImageLocalPath")]
    public string MobileImageLocalPath { get; init; } = string.Empty;
}

public sealed class HomepageFallbackFeatureIconLink
{
    [JsonPropertyName("label")]
    public string Label { get; init; } = string.Empty;

    [JsonPropertyName("linkUrl")]
    public string LinkUrl { get; init; } = string.Empty;

    [JsonPropertyName("iconMdiClass")]
    public string IconMdiClass { get; init; } = string.Empty;

    [JsonPropertyName("iconColor")]
    public string IconColor { get; init; } = string.Empty;
}

public sealed class HomepageFallbackApplyOnline
{
    [JsonPropertyName("imageUrl")]
    public string ImageUrl { get; init; } = string.Empty;

    [JsonPropertyName("imageLocalPath")]
    public string ImageLocalPath { get; init; } = string.Empty;

    [JsonPropertyName("targetUrl")]
    public string TargetUrl { get; init; } = string.Empty;
}

public sealed class HomepageFallbackPromoBanner
{
    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("targetUrl")]
    public string TargetUrl { get; init; } = string.Empty;

    [JsonPropertyName("imageUrl")]
    public string ImageUrl { get; init; } = string.Empty;

    [JsonPropertyName("imageLocalPath")]
    public string ImageLocalPath { get; init; } = string.Empty;
}

public sealed class HomepageFallbackNewsItem
{
    [JsonPropertyName("title")]
    public string Title { get; init; } = string.Empty;

    [JsonPropertyName("url")]
    public string Url { get; init; } = string.Empty;

    [JsonPropertyName("publishDate")]
    public string PublishDate { get; init; } = string.Empty;

    [JsonPropertyName("excerpt")]
    public string Excerpt { get; init; } = string.Empty;

    [JsonPropertyName("thumbnailUrl")]
    public string ThumbnailUrl { get; init; } = string.Empty;

    [JsonPropertyName("thumbnailLocalPath")]
    public string ThumbnailLocalPath { get; init; } = string.Empty;
}
