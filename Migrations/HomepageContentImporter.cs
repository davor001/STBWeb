using System.IO;
using Newtonsoft.Json;
using STBWeb.Services;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.IO;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.PropertyEditors;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Strings;
using Umbraco.Cms.Core.Web;
using Umbraco.Extensions;

namespace STBWeb.Migrations;

/// <summary>
/// Imports the scraped homepage fallback into real Umbraco content/media once,
/// so editors can take over management in the backoffice afterwards.
/// </summary>
public class HomepageContentImporterHandler : INotificationAsyncHandler<UmbracoApplicationStartedNotification>
{
    private const string ImportStateKey = "STBWeb.HomepageImport.Version";
    private const string ImportVersion = "homepage-v1";
    private const string HomepageMediaFolderName = "Homepage Import";
    private const string MkCulture = "mk";
    private const string EnCulture = "en";

    private readonly HomepageFallbackService _homepageFallbackService;
    private readonly IContentService _contentService;
    private readonly IContentTypeService _contentTypeService;
    private readonly IMediaService _mediaService;
    private readonly IKeyValueService _keyValueService;
    private readonly MediaFileManager _mediaFileManager;
    private readonly MediaUrlGeneratorCollection _mediaUrlGenerators;
    private readonly IShortStringHelper _shortStringHelper;
    private readonly IContentTypeBaseServiceProvider _contentTypeBaseServiceProvider;
    private readonly IUmbracoContextFactory _umbracoContextFactory;
    private readonly IWebHostEnvironment _webHostEnvironment;
    private readonly ILogger<HomepageContentImporterHandler> _logger;

    public HomepageContentImporterHandler(
        HomepageFallbackService homepageFallbackService,
        IContentService contentService,
        IContentTypeService contentTypeService,
        IMediaService mediaService,
        IKeyValueService keyValueService,
        MediaFileManager mediaFileManager,
        MediaUrlGeneratorCollection mediaUrlGenerators,
        IShortStringHelper shortStringHelper,
        IContentTypeBaseServiceProvider contentTypeBaseServiceProvider,
        IUmbracoContextFactory umbracoContextFactory,
        IWebHostEnvironment webHostEnvironment,
        ILogger<HomepageContentImporterHandler> logger)
    {
        _homepageFallbackService = homepageFallbackService;
        _contentService = contentService;
        _contentTypeService = contentTypeService;
        _mediaService = mediaService;
        _keyValueService = keyValueService;
        _mediaFileManager = mediaFileManager;
        _mediaUrlGenerators = mediaUrlGenerators;
        _shortStringHelper = shortStringHelper;
        _contentTypeBaseServiceProvider = contentTypeBaseServiceProvider;
        _umbracoContextFactory = umbracoContextFactory;
        _webHostEnvironment = webHostEnvironment;
        _logger = logger;
    }

    public Task HandleAsync(UmbracoApplicationStartedNotification notification, CancellationToken cancellationToken)
    {
        try
        {
            ImportHomepageContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "STBWeb: Homepage content importer failed.");
        }

        return Task.CompletedTask;
    }

    private void ImportHomepageContent()
    {
        if (_keyValueService.GetValue(ImportStateKey) == ImportVersion)
        {
            _logger.LogInformation("STBWeb: Homepage content import already completed ({Version}), skipping.", ImportVersion);
            return;
        }

        var homepageData = _homepageFallbackService.GetData();
        if (!homepageData.HeroSlides.Any() &&
            !homepageData.FeatureIconLinks.Any() &&
            homepageData.ApplyOnline is null &&
            !homepageData.PromoBanners.Any())
        {
            _logger.LogInformation("STBWeb: Homepage fallback JSON is empty, import skipped.");
            return;
        }

        var home = _contentService.GetRootContent().FirstOrDefault(x => x.ContentType.Alias == "homePage");
        if (home == null)
        {
            _logger.LogWarning("STBWeb: Could not find the Doma homePage node. Homepage import skipped.");
            return;
        }

        var heroSlideType = _contentTypeService.Get("heroSlide");
        var featureIconLinkType = _contentTypeService.Get("featureIconLink");
        if (heroSlideType == null || featureIconLinkType == null)
        {
            _logger.LogWarning("STBWeb: Homepage block element types are missing. Homepage import skipped.");
            return;
        }

        var routeMap = BuildPublishedRouteMap();
        var mediaRoot = GetOrCreateMediaFolder(HomepageMediaFolderName, -1);
        var homeMedia = ImportHomepageMedia(homepageData, mediaRoot.Id);
        var heroBlockValue = BuildHeroSlidesBlockValue(homepageData, heroSlideType.Key, homeMedia, routeMap);
        var featureBlockValue = BuildFeatureIconLinksBlockValue(homepageData, featureIconLinkType.Key, routeMap);

        var topsiNode = GetOrCreateSubBrandNode(home, "Topsi", "Topsi", "TOPSI", "topsi");
        var goldenClubNode = GetOrCreateSubBrandNode(home, "Goldenclub", "Goldenclub", "GoldenClub", "goldenclub");

        if (homeMedia.TopsiPromoMedia != null)
        {
            AssignSubBrandLogo(topsiNode, "topsi", homeMedia.TopsiPromoMedia);
        }

        if (homeMedia.GoldenClubPromoMedia != null)
        {
            AssignSubBrandLogo(goldenClubNode, "goldenclub", homeMedia.GoldenClubPromoMedia);
        }

        if (!string.IsNullOrWhiteSpace(heroBlockValue))
        {
            SetPropertyValue(home, "heroSlides", heroBlockValue, MkCulture, EnCulture);
        }

        if (!string.IsNullOrWhiteSpace(featureBlockValue))
        {
            SetPropertyValue(home, "featureIconLinks", featureBlockValue, MkCulture, EnCulture);
        }

        if (homeMedia.ApplyOnlineMedia != null)
        {
            SetPropertyValue(home, "applyOnlineImage", BuildSingleMediaPickerValue(homeMedia.ApplyOnlineMedia), MkCulture, EnCulture);
        }

        var applyOnlineLinkValue = BuildMultiUrlPickerValue(
            homepageData.ApplyOnline?.TargetUrl,
            "Apply Online",
            routeMap);
        if (!string.IsNullOrWhiteSpace(applyOnlineLinkValue))
        {
            SetPropertyValue(home, "applyOnlineUrl", applyOnlineLinkValue, MkCulture, EnCulture);
        }

        if (topsiNode != null)
        {
            SetPropertyValue(home, "promoBanner1", Udi.Create(Constants.UdiEntityType.Document, topsiNode.Key).ToString(), MkCulture, EnCulture);
        }

        if (goldenClubNode != null)
        {
            SetPropertyValue(home, "promoBanner2", Udi.Create(Constants.UdiEntityType.Document, goldenClubNode.Key).ToString(), MkCulture, EnCulture);
        }

        var saveHomeResult = _contentService.Save(home);
        if (!saveHomeResult.Success)
        {
            _logger.LogError("STBWeb: Failed to save Doma homepage import. Result: {@Result}", saveHomeResult.Result);
            return;
        }

        var publishHomeResult = _contentService.Publish(home, new[] { "*" });
        if (!publishHomeResult.Success)
        {
            _logger.LogError("STBWeb: Failed to publish Doma homepage import. Result: {@Result}", publishHomeResult.Result);
            return;
        }

        _keyValueService.SetValue(ImportStateKey, ImportVersion);
        _logger.LogInformation(
            "STBWeb: Homepage content imported into Umbraco. HeroSlides={HeroCount} FeatureIcons={IconCount} PromoNodes={PromoCount}",
            homepageData.HeroSlides.Count,
            homepageData.FeatureIconLinks.Count,
            new[] { topsiNode, goldenClubNode }.Count(x => x != null));
    }

    private Dictionary<string, IPublishedContent> BuildPublishedRouteMap()
    {
        using var contextReference = _umbracoContextFactory.EnsureUmbracoContext();
        var contentCache = contextReference.UmbracoContext.Content;
        if (contentCache == null)
        {
            return new Dictionary<string, IPublishedContent>(StringComparer.OrdinalIgnoreCase);
        }

        return contentCache.GetAtRoot()
            .SelectMany(root => root.DescendantsOrSelf())
            .Select(content => new
            {
                Content = content,
                Url = NormalizeImportedUrl(content.Url(culture: MkCulture))
            })
            .Where(x => !string.IsNullOrWhiteSpace(x.Url))
            .GroupBy(x => GetLookupKey(x.Url))
            .ToDictionary(x => x.Key, x => x.First().Content, StringComparer.OrdinalIgnoreCase);
    }

    private ImportedHomepageMedia ImportHomepageMedia(STBWeb.Models.HomepageFallbackData homepageData, int mediaRootId)
    {
        var importedMedia = new ImportedHomepageMedia();

        for (var index = 0; index < homepageData.HeroSlides.Count; index++)
        {
            var slide = homepageData.HeroSlides[index];
            importedMedia.HeroDesktopMedia[index] = ImportImageMedia(
                mediaRootId,
                $"Hero Slide {index + 1:D2} Desktop",
                slide.DesktopImageLocalPath);
            importedMedia.HeroMobileMedia[index] = ImportImageMedia(
                mediaRootId,
                $"Hero Slide {index + 1:D2} Mobile",
                slide.MobileImageLocalPath);
        }

        importedMedia.ApplyOnlineMedia = ImportImageMedia(
            mediaRootId,
            "Apply Online",
            homepageData.ApplyOnline.ImageLocalPath);

        var topsiPromo = homepageData.PromoBanners.FirstOrDefault(x => x.Name.Contains("topsi", StringComparison.OrdinalIgnoreCase));
        if (topsiPromo != null)
        {
            importedMedia.TopsiPromoMedia = ImportImageMedia(mediaRootId, "Promo Banner TOPSI", topsiPromo.ImageLocalPath);
        }

        var goldenClubPromo = homepageData.PromoBanners.FirstOrDefault(x => x.Name.Contains("golden", StringComparison.OrdinalIgnoreCase));
        if (goldenClubPromo != null)
        {
            importedMedia.GoldenClubPromoMedia = ImportImageMedia(mediaRootId, "Promo Banner Golden Club", goldenClubPromo.ImageLocalPath);
        }

        return importedMedia;
    }

    private string BuildHeroSlidesBlockValue(
        STBWeb.Models.HomepageFallbackData homepageData,
        Guid heroSlideContentTypeKey,
        ImportedHomepageMedia media,
        IReadOnlyDictionary<string, IPublishedContent> routeMap)
    {
        var blocks = new List<BlockListItemValue>();

        for (var index = 0; index < homepageData.HeroSlides.Count; index++)
        {
            var slide = homepageData.HeroSlides[index];
            blocks.Add(new BlockListItemValue(
                Guid.NewGuid(),
                heroSlideContentTypeKey,
                new object[]
                {
                    CreateBlockPropertyValue(
                        "desktopImage",
                        media.HeroDesktopMedia.TryGetValue(index, out var desktopMedia) && desktopMedia != null
                            ? BuildSingleMediaPickerValue(desktopMedia)
                            : null,
                        "Umbraco.MediaPicker3"),
                    CreateBlockPropertyValue(
                        "mobileImage",
                        media.HeroMobileMedia.TryGetValue(index, out var mobileMedia) && mobileMedia != null
                            ? BuildSingleMediaPickerValue(mobileMedia)
                            : null,
                        "Umbraco.MediaPicker3"),
                    CreateBlockPropertyValue("slideHeading", slide.SlideHeading, "Umbraco.TextBox"),
                    CreateBlockPropertyValue("slideSubheading", slide.SlideSubheading, "Umbraco.TextBox"),
                    CreateBlockPropertyValue("slideCtaText", slide.SlideCtaText, "Umbraco.TextBox"),
                    CreateBlockPropertyValue(
                        "slideCtaUrl",
                        BuildMultiUrlPickerValue(slide.SlideCtaUrl, slide.SlideCtaText, routeMap),
                        "Umbraco.MultiUrlPicker")
                }));
        }

        return BuildBlockListValue(blocks);
    }

    private string BuildFeatureIconLinksBlockValue(
        STBWeb.Models.HomepageFallbackData homepageData,
        Guid featureIconLinkContentTypeKey,
        IReadOnlyDictionary<string, IPublishedContent> routeMap)
    {
        var blocks = new List<BlockListItemValue>();

        foreach (var icon in homepageData.FeatureIconLinks)
        {
            blocks.Add(new BlockListItemValue(
                Guid.NewGuid(),
                featureIconLinkContentTypeKey,
                new object[]
                {
                    CreateBlockPropertyValue("iconMdiClass", icon.IconMdiClass, "Umbraco.TextBox"),
                    CreateBlockPropertyValue("label", icon.Label, "Umbraco.TextBox"),
                    CreateBlockPropertyValue("iconColor", icon.IconColor, "Umbraco.TextBox"),
                    CreateBlockPropertyValue(
                        "linkUrl",
                        BuildMultiUrlPickerValue(icon.LinkUrl, icon.Label, routeMap),
                        "Umbraco.MultiUrlPicker")
                }));
        }

        return BuildBlockListValue(blocks);
    }

    private string BuildBlockListValue(IEnumerable<BlockListItemValue> blocks)
    {
        var items = blocks.ToList();
        if (!items.Any())
        {
            return string.Empty;
        }

        // Umbraco 15 block list values are stored with key-based layout references plus
        // per-block values[] entries; the older UDI/raw-property-only shape is not enough.
        var payload = new
        {
            Layout = new Dictionary<string, object?>
            {
                [Constants.PropertyEditors.Aliases.BlockList] = items.Select(item => new
                {
                    contentUdi = (string?)null,
                    settingsUdi = (string?)null,
                    contentKey = item.Key,
                    settingsKey = (Guid?)null
                }).ToList()
            },
            contentData = items.Select(item => new
            {
                udi = (string?)null,
                contentTypeKey = item.ContentTypeKey,
                key = item.Key,
                values = item.Values
            }).ToList(),
            settingsData = Array.Empty<object>(),
            expose = Array.Empty<object>()
        };

        return JsonConvert.SerializeObject(payload, Formatting.None);
    }

    private static object CreateBlockPropertyValue(string alias, object? value, string editorAlias) => new
    {
        editorAlias,
        alias,
        culture = (string?)null,
        segment = (string?)null,
        value
    };

    private string BuildMultiUrlPickerValue(
        string? rawUrl,
        string? fallbackName,
        IReadOnlyDictionary<string, IPublishedContent> routeMap)
    {
        var normalizedUrl = NormalizeImportedUrl(rawUrl);
        if (string.IsNullOrWhiteSpace(normalizedUrl))
        {
            return string.Empty;
        }

        IPublishedContent? linkedContent = null;
        if (normalizedUrl.StartsWith("/", StringComparison.Ordinal))
        {
            routeMap.TryGetValue(GetLookupKey(normalizedUrl), out linkedContent);
        }

        var linkName = !string.IsNullOrWhiteSpace(fallbackName)
            ? fallbackName
            : linkedContent?.Name(MkCulture) ?? linkedContent?.Name ?? normalizedUrl;

        var linkValue = new[]
        {
            new
            {
                name = linkName,
                target = (string?)null,
                udi = linkedContent != null ? Udi.Create(Constants.UdiEntityType.Document, linkedContent.Key).ToString() : null,
                url = normalizedUrl,
                queryString = (string?)null
            }
        };

        return JsonConvert.SerializeObject(linkValue, Formatting.None);
    }

    private static string BuildSingleMediaPickerValue(IMedia media) =>
        Udi.Create(Constants.UdiEntityType.Media, media.Key).ToString();

    private IMedia? ImportImageMedia(int parentId, string mediaName, string? localPath)
    {
        if (string.IsNullOrWhiteSpace(localPath))
        {
            return null;
        }

        var existing = _mediaService
            .GetPagedChildren(parentId, 0, 200, out _, null, null)
            .FirstOrDefault(x => x.Name.InvariantEquals(mediaName));
        if (existing != null)
        {
            return existing;
        }

        var physicalPath = ResolvePhysicalPath(localPath);
        if (!System.IO.File.Exists(physicalPath))
        {
            _logger.LogWarning("STBWeb: Homepage media file not found at {Path}.", physicalPath);
            return null;
        }

        using var stream = System.IO.File.OpenRead(physicalPath);
        var media = _mediaService.CreateMedia(mediaName, parentId, "Image");
        media.SetValue(
            _mediaFileManager,
            _mediaUrlGenerators,
            _shortStringHelper,
            _contentTypeBaseServiceProvider,
            Constants.Conventions.Media.File,
            Path.GetFileName(physicalPath),
            stream);
        _mediaService.Save(media);
        return media;
    }

    private IContent? GetOrCreateSubBrandNode(
        IContent home,
        string invariantName,
        string mkName,
        string enName,
        string subbrandKey)
    {
        var existing = _contentService
            .GetPagedChildren(home.Id, 0, 200, out _, null, null)
            .FirstOrDefault(x =>
                x.ContentType.Alias == "subBrandHomePage" &&
                !string.IsNullOrWhiteSpace(x.Name) &&
                x.Name.InvariantContains(invariantName));

        if (existing != null)
        {
            return existing;
        }

        var subBrand = _contentService.Create(invariantName, home.Id, "subBrandHomePage");
        subBrand.SetCultureName(mkName, MkCulture);
        subBrand.SetCultureName(enName, EnCulture);
        SetPropertyValue(subBrand, "subbrandKey", subbrandKey, MkCulture, EnCulture);

        var saveResult = _contentService.Save(subBrand);
        if (!saveResult.Success)
        {
            _logger.LogWarning("STBWeb: Could not create sub-brand node '{Name}'. Result: {@Result}", invariantName, saveResult.Result);
            return null;
        }

        var publishResult = _contentService.Publish(subBrand, new[] { "*" });
        if (!publishResult.Success)
        {
            _logger.LogWarning("STBWeb: Could not publish sub-brand node '{Name}'. Result: {@Result}", invariantName, publishResult.Result);
        }

        return subBrand;
    }

    private void AssignSubBrandLogo(IContent? subBrandNode, string subbrandKey, IMedia logoMedia)
    {
        if (subBrandNode == null)
        {
            return;
        }

        SetPropertyValue(subBrandNode, "subbrandKey", subbrandKey, MkCulture, EnCulture);
        SetPropertyValue(subBrandNode, "logo", BuildSingleMediaPickerValue(logoMedia), MkCulture, EnCulture);

        var saveResult = _contentService.Save(subBrandNode);
        if (!saveResult.Success)
        {
            _logger.LogWarning("STBWeb: Could not save sub-brand node '{Name}' during homepage import. Result: {@Result}", subBrandNode.Name, saveResult.Result);
            return;
        }

        var publishResult = _contentService.Publish(subBrandNode, new[] { "*" });
        if (!publishResult.Success)
        {
            _logger.LogWarning("STBWeb: Could not publish sub-brand node '{Name}' during homepage import. Result: {@Result}", subBrandNode.Name, publishResult.Result);
        }
    }

    private IMedia GetOrCreateMediaFolder(string name, int parentId)
    {
        IMedia? existing = parentId == -1
            ? _mediaService.GetRootMedia().FirstOrDefault(x => x.Name.InvariantEquals(name))
            : _mediaService.GetPagedChildren(parentId, 0, 200, out _, null, null)
                .FirstOrDefault(x => x.ContentType.Alias.InvariantEquals("Folder") && x.Name.InvariantEquals(name));

        if (existing != null)
        {
            return existing;
        }

        var folder = _mediaService.CreateMedia(name, parentId, "Folder");
        _mediaService.Save(folder);
        return folder;
    }

    private void SetPropertyValue(IContent content, string propertyAlias, object? value, params string[] cultures)
    {
        if (value == null)
        {
            return;
        }

        var property = content.Properties.FirstOrDefault(x => x.Alias == propertyAlias);
        if (property?.PropertyType == null)
        {
            _logger.LogWarning(
                "STBWeb: Property '{Alias}' was not found on content item '{Name}' during homepage import.",
                propertyAlias,
                content.Name);
            return;
        }

        if (property.PropertyType.VariesByCulture())
        {
            var distinctCultures = cultures
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (!distinctCultures.Any())
            {
                _logger.LogWarning(
                    "STBWeb: Property '{Alias}' on '{Name}' varies by culture, but no cultures were supplied.",
                    propertyAlias,
                    content.Name);
                return;
            }

            _logger.LogInformation(
                "STBWeb: Homepage importer writing {Node}.{Alias} as variant for cultures {Cultures}.",
                content.Name,
                propertyAlias,
                string.Join(", ", distinctCultures));

            foreach (var culture in distinctCultures)
            {
                content.SetValue(propertyAlias, value, culture);
            }

            return;
        }

        _logger.LogInformation(
            "STBWeb: Homepage importer writing {Node}.{Alias} as invariant.",
            content.Name,
            propertyAlias);
        content.SetValue(propertyAlias, value);
    }

    private string ResolvePhysicalPath(string localPath)
    {
        var normalizedPath = localPath.Replace("/", Path.DirectorySeparatorChar.ToString())
            .Replace("\\", Path.DirectorySeparatorChar.ToString())
            .TrimStart(Path.DirectorySeparatorChar);
        return Path.Combine(_webHostEnvironment.ContentRootPath, normalizedPath);
    }

    private static string NormalizeImportedUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return string.Empty;
        }

        var trimmed = url.Trim();
        if (trimmed.StartsWith("#", StringComparison.Ordinal))
        {
            return trimmed;
        }

        if (trimmed.StartsWith("javascript:", StringComparison.OrdinalIgnoreCase))
        {
            return string.Empty;
        }

        if (trimmed.StartsWith("//", StringComparison.Ordinal))
        {
            if (Uri.TryCreate($"https:{trimmed}", UriKind.Absolute, out var protocolRelativeUri))
            {
                return IsInternalHost(protocolRelativeUri.Host)
                    ? $"{(string.IsNullOrWhiteSpace(protocolRelativeUri.AbsolutePath) ? "/" : protocolRelativeUri.AbsolutePath)}{protocolRelativeUri.Query}{protocolRelativeUri.Fragment}"
                    : trimmed;
            }

            return string.Empty;
        }

        if (Uri.TryCreate(trimmed, UriKind.Absolute, out var absoluteUri))
        {
            return IsInternalHost(absoluteUri.Host)
                ? $"{(string.IsNullOrWhiteSpace(absoluteUri.AbsolutePath) ? "/" : absoluteUri.AbsolutePath)}{absoluteUri.Query}{absoluteUri.Fragment}"
                : trimmed;
        }

        if (!Uri.TryCreate(trimmed, UriKind.Relative, out _))
        {
            return string.Empty;
        }

        if (trimmed.StartsWith("?", StringComparison.Ordinal))
        {
            return trimmed;
        }

        while (trimmed.StartsWith("./", StringComparison.Ordinal))
        {
            trimmed = trimmed[2..];
        }

        while (trimmed.StartsWith("../", StringComparison.Ordinal))
        {
            trimmed = trimmed[3..];
        }

        return "/" + trimmed.TrimStart('/');
    }

    private static bool IsInternalHost(string host) =>
        host.Equals("stb.com.mk", StringComparison.OrdinalIgnoreCase) ||
        host.Equals("www.stb.com.mk", StringComparison.OrdinalIgnoreCase);

    private static string GetLookupKey(string normalizedUrl)
    {
        var value = normalizedUrl;
        var separatorIndex = value.IndexOfAny(['?', '#']);
        if (separatorIndex >= 0)
        {
            value = value[..separatorIndex];
        }

        if (value.Length > 1)
        {
            value = value.TrimEnd('/');
        }

        return value;
    }

    private sealed record BlockListItemValue(Guid Key, Guid ContentTypeKey, IReadOnlyList<object> Values);

    private sealed class ImportedHomepageMedia
    {
        public Dictionary<int, IMedia?> HeroDesktopMedia { get; } = [];
        public Dictionary<int, IMedia?> HeroMobileMedia { get; } = [];
        public IMedia? ApplyOnlineMedia { get; set; }
        public IMedia? TopsiPromoMedia { get; set; }
        public IMedia? GoldenClubPromoMedia { get; set; }
    }
}
