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
    // Bump this string whenever homepage content/media needs to be re-imported on Azure.
    // The importer is idempotent per version: it runs once, stores this key in the DB,
    // and skips on every subsequent startup until the version is changed.
    // v3: re-run after previous import crashed on missing subBrandHomePage document type,
    // leaving heroSlides empty on the Doma node.
    private const string ImportVersion = "homepage-v3";

    private const string DigitalChannelsImportStateKey = "STBWeb.DigitalChannelsImport.Version";
    // v3: fix block list JSON — lowercase "layout" key and populated "expose" array so
    //     blocks are visible in the backoffice editor and persist correctly after save.
    private const string DigitalChannelsImportVersion = "digital-channels-v3";

    private const string PromoBannersImportStateKey = "STBWeb.PromoBannersImport.Version";
    // v2: seed flat promoBanner1Title/Image/Link and promoBanner2Title/Image/Link properties
    //     instead of the Block List approach that was reverted.
    private const string PromoBannersImportVersion = "promo-banners-v2";
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

        try
        {
            ImportDigitalChannels();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "STBWeb: Digital channels importer failed.");
        }

        try
        {
            ImportPromoBanners();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "STBWeb: Promo banners importer failed.");
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

    // -----------------------------------------------------------------------
    // ImportDigitalChannels
    // Seeds the four default digital channel cards into the homePage content
    // node so editors can manage them from the backoffice. Runs at most once
    // (guarded by DigitalChannelsImportVersion). Skips if the property already
    // contains content, so existing editor-managed data is never overwritten.
    // -----------------------------------------------------------------------
    private void ImportDigitalChannels()
    {
        if (_keyValueService.GetValue(DigitalChannelsImportStateKey) == DigitalChannelsImportVersion)
        {
            _logger.LogInformation("STBWeb: Digital channels import already completed, skipping.");
            return;
        }

        var home = _contentService.GetRootContent().FirstOrDefault(x => x.ContentType.Alias == "homePage");
        if (home == null)
        {
            _logger.LogWarning("STBWeb: Could not find homePage for digital channels import.");
            return;
        }

        // Skip if an editor has already populated the field.
        var existingValue = home.GetValue<string>("digitalChannels");
        if (!string.IsNullOrWhiteSpace(existingValue) && existingValue.TrimStart().StartsWith("{", StringComparison.Ordinal))
        {
            _logger.LogInformation("STBWeb: digitalChannels property already has content, skipping seed.");
            _keyValueService.SetValue(DigitalChannelsImportStateKey, DigitalChannelsImportVersion);
            return;
        }

        var digitalChannelType = _contentTypeService.Get("digitalChannel");
        if (digitalChannelType == null)
        {
            _logger.LogWarning("STBWeb: digitalChannel element type not found; digital channels import skipped.");
            return;
        }

        var blockValue = BuildDigitalChannelsBlockValue(digitalChannelType.Key);
        if (string.IsNullOrWhiteSpace(blockValue))
        {
            return;
        }

        // digitalChannels is an invariant property — set it directly without a culture.
        home.SetValue("digitalChannels", blockValue);

        var saveResult = _contentService.Save(home);
        if (!saveResult.Success)
        {
            _logger.LogError("STBWeb: Failed to save digital channels. Result: {@Result}", saveResult.Result);
            return;
        }

        var publishResult = _contentService.Publish(home, new[] { "*" });
        if (!publishResult.Success)
        {
            _logger.LogError("STBWeb: Failed to publish digital channels. Result: {@Result}", publishResult.Result);
            return;
        }

        _keyValueService.SetValue(DigitalChannelsImportStateKey, DigitalChannelsImportVersion);
        _logger.LogInformation("STBWeb: Digital channels seeded into Umbraco (4 cards).");
    }

    // -----------------------------------------------------------------------
    // ImportPromoBanners
    // Seeds the Topsi / Golden Club promo banners as two fixed sets of flat
    // properties (promoBanner1Title/Image/Link and promoBanner2Title/Image/Link)
    // on homePage. Runs once (version-guarded). Skips if both title fields are
    // already populated so editor changes are never overwritten.
    // -----------------------------------------------------------------------
    private void ImportPromoBanners()
    {
        if (_keyValueService.GetValue(PromoBannersImportStateKey) == PromoBannersImportVersion)
        {
            _logger.LogInformation("STBWeb: Promo banners import already completed, skipping.");
            return;
        }

        var home = _contentService.GetRootContent().FirstOrDefault(x => x.ContentType.Alias == "homePage");
        if (home == null)
        {
            _logger.LogWarning("STBWeb: Could not find homePage for promo banners import.");
            return;
        }

        // Skip if an editor has already populated the fields.
        var existing1 = home.GetValue<string>("promoBanner1Title");
        var existing2 = home.GetValue<string>("promoBanner2Title");
        if (!string.IsNullOrWhiteSpace(existing1) && !string.IsNullOrWhiteSpace(existing2))
        {
            _logger.LogInformation("STBWeb: promoBanner1Title/2Title already have content, skipping seed.");
            _keyValueService.SetValue(PromoBannersImportStateKey, PromoBannersImportVersion);
            return;
        }

        var homepageData = _homepageFallbackService.GetData();
        if (!homepageData.PromoBanners.Any())
        {
            _logger.LogInformation("STBWeb: No promo banners in fallback data, skipping seed.");
            _keyValueService.SetValue(PromoBannersImportStateKey, PromoBannersImportVersion);
            return;
        }

        var mediaRoot = GetOrCreateMediaFolder(HomepageMediaFolderName, -1);

        var bannerDefs = new[]
        {
            (Index: 1, MediaName: "Promo Banner TOPSI",       Keyword: "topsi"),
            (Index: 2, MediaName: "Promo Banner Golden Club", Keyword: "golden"),
        };

        var seededCount = 0;

        foreach (var def in bannerDefs)
        {
            var promo = homepageData.PromoBanners
                .FirstOrDefault(p => p.Name.Contains(def.Keyword, StringComparison.OrdinalIgnoreCase))
                ?? (def.Index <= homepageData.PromoBanners.Count ? homepageData.PromoBanners[def.Index - 1] : null);

            if (promo == null)
            {
                continue;
            }

            var titleAlias = $"promoBanner{def.Index}Title";
            var imageAlias = $"promoBanner{def.Index}Image";
            var linkAlias  = $"promoBanner{def.Index}Link";

            // Title (invariant)
            if (home.Properties.Any(p => p.Alias == titleAlias))
            {
                home.SetValue(titleAlias, promo.Name);
            }

            // Image (invariant) — reuse image imported by ImportHomepageContent
            var mediaItem = _mediaService
                .GetPagedChildren(mediaRoot.Id, 0, 200, out _, null, null)
                .FirstOrDefault(x => x.Name.InvariantEquals(def.MediaName))
                ?? ImportImageMedia(mediaRoot.Id, def.MediaName, promo.ImageLocalPath);

            if (mediaItem != null && home.Properties.Any(p => p.Alias == imageAlias))
            {
                home.SetValue(imageAlias, BuildSingleMediaPickerValue(mediaItem));
            }

            // Link (invariant)
            var targetUrl = NormalizeImportedUrl(promo.TargetUrl);
            if (!string.IsNullOrWhiteSpace(targetUrl) && home.Properties.Any(p => p.Alias == linkAlias))
            {
                var linkJson = JsonConvert.SerializeObject(new[]
                {
                    new { name = promo.Name, target = (string?)null, udi = (string?)null, url = targetUrl, queryString = (string?)null }
                });
                home.SetValue(linkAlias, linkJson);
            }

            seededCount++;
        }

        if (seededCount == 0)
        {
            _keyValueService.SetValue(PromoBannersImportStateKey, PromoBannersImportVersion);
            return;
        }

        var saveResult = _contentService.Save(home);
        if (!saveResult.Success)
        {
            _logger.LogError("STBWeb: Failed to save promo banners. Result: {@Result}", saveResult.Result);
            return;
        }

        var publishResult = _contentService.Publish(home, new[] { "*" });
        if (!publishResult.Success)
        {
            _logger.LogError("STBWeb: Failed to publish promo banners. Result: {@Result}", publishResult.Result);
            return;
        }

        _keyValueService.SetValue(PromoBannersImportStateKey, PromoBannersImportVersion);
        _logger.LogInformation("STBWeb: Promo banners seeded into Umbraco ({Count} banners).", seededCount);
    }

    private string BuildDigitalChannelsBlockValue(Guid digitalChannelContentTypeKey)
    {
        var channels = new[]
        {
            (
                Title:       "e-banking",
                Icon:        "mdi-laptop-chromebook",
                Description: "Достапен 24/7 за безбедно, брзо и лесно управување со вашите финансии",
                Url:         "/naselenie/digitalno-bankarstvo/e-banking/"
            ),
            (
                Title:       "m-banking",
                Icon:        "mdi-cellphone-iphone",
                Description: "БЕСПЛАТНА Android и iOS апликација за 24/7 банка во вашиот мобилен",
                Url:         "/naselenie/digitalno-bankarstvo/m-banking/"
            ),
            (
                Title:       "Брзи плаќања - Topsi Pay",
                Icon:        "mdi-coin",
                Description: "сервис за БРЗИ плаќања кон пријателите на Facebook или контактите во мобилниот телефон",
                Url:         "/naselenie/digitalno-bankarstvo/brzi-plakjanja-topsi-pay/"
            ),
            (
                Title:       "E-Commerce",
                Icon:        "mdi-cart-outline",
                Description: "Сервис за е-commerce на Стопанска Банка е наменет за правни лица кои имаат сметка во Банката или планираат да отворат",
                Url:         "/pravni-lica/digitalno-bankarstvo/e-commerce/"
            ),
        };

        var blocks = new List<BlockListItemValue>();
        foreach (var ch in channels)
        {
            var linkJson = JsonConvert.SerializeObject(new[]
            {
                new { name = ch.Title, target = (string?)null, udi = (string?)null, url = ch.Url, queryString = (string?)null }
            });

            blocks.Add(new BlockListItemValue(
                Guid.NewGuid(),
                digitalChannelContentTypeKey,
                new object[]
                {
                    CreateBlockPropertyValue("channelTitle",       ch.Title,       "Umbraco.TextBox"),
                    CreateBlockPropertyValue("channelIcon",        ch.Icon,        "Umbraco.TextBox"),
                    CreateBlockPropertyValue("channelDescription", ch.Description, "Umbraco.TextBox"),
                    CreateBlockPropertyValue("channelUrl",         linkJson,       "Umbraco.MultiUrlPicker"),
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

        // Umbraco 15 block list JSON format requirements:
        //  - "layout" key must be lowercase (System.Text.Json is case-sensitive on read)
        //  - Every block that should be visible must appear in "expose" with exposed:true.
        //    Blocks absent from expose are treated as soft-deleted; the backoffice editor
        //    hides them, so saves from an apparently-empty editor discard them and the
        //    newly-added block never survives the round-trip.
        //  - layout items need only contentKey (no UDI fields in v15)
        //  - contentData items need only contentTypeKey + key + values (no UDI in v15)
        var payload = new
        {
            layout = new Dictionary<string, object?>
            {
                [Constants.PropertyEditors.Aliases.BlockList] = items
                    .Select(item => new { contentKey = item.Key })
                    .ToList()
            },
            contentData = items.Select(item => new
            {
                contentTypeKey = item.ContentTypeKey,
                key = item.Key,
                values = item.Values
            }).ToList(),
            settingsData = Array.Empty<object>(),
            expose = items.Select(item => new
            {
                contentKey = item.Key,
                culture    = (string?)null,
                segment    = (string?)null,
                exposed    = true
            }).ToList()
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
        try
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

            // Guard: if the document type doesn't exist yet, don't crash the whole import
            if (_contentTypeService.Get("subBrandHomePage") == null)
            {
                _logger.LogWarning("STBWeb: 'subBrandHomePage' document type not found — skipping sub-brand node '{Name}'.", invariantName);
                return null;
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
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "STBWeb: Failed to get/create sub-brand node '{Name}' — continuing without it.", invariantName);
            return null;
        }
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
