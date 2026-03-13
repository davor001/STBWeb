using Umbraco.Cms.Core;
using System.IO;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.Strings;
using Microsoft.Extensions.Logging;
using Umbraco.Cms.Core.Serialization;
using Umbraco.Cms.Core.PropertyEditors;

namespace STBWeb.Migrations;

public class DocumentTypeMigrationComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        // Use UmbracoApplicationStartedNotification (not Starting) so that
        // Umbraco's built-in data types (Block List, etc.) are fully seeded first.
        builder.AddNotificationAsyncHandler<UmbracoApplicationStartedNotification, DocumentTypeMigrationHandler>();
        builder.AddNotificationAsyncHandler<UmbracoApplicationStartedNotification, ContentSeederHandler>();
        builder.AddNotificationAsyncHandler<UmbracoApplicationStartedNotification, MediaSeederHandler>();
        builder.AddNotificationAsyncHandler<UmbracoApplicationStartedNotification, HomepageContentImporterHandler>();
    }
}

/// <summary>
/// Creates all Umbraco document types, compositions, and element types for the STB website.
/// Runs on application start; fully idempotent.
/// </summary>
public class DocumentTypeMigrationHandler : INotificationAsyncHandler<UmbracoApplicationStartedNotification>
{
    private readonly IContentTypeService _contentTypeService;
    private readonly IDataTypeService _dataTypeService;
    private readonly IShortStringHelper _shortStringHelper;
    private readonly ILogger<DocumentTypeMigrationHandler> _logger;
    private readonly ITemplateService _templateService;

    // Cached data types (populated in HandleAsync)
    private IDataType? _dtTextstring;
    private IDataType? _dtTextarea;
    private IDataType? _dtRichText;
    private IDataType? _dtNumeric;
    private IDataType? _dtTrueFalse;
    private IDataType? _dtDatetime;
    private IDataType? _dtMediaPicker;
    private IDataType? _dtMultiUrlPicker;
    private IDataType? _dtContentPicker;
    private IDataType? _dtDropdown;
    private IDataType? _dtBlockList;
    private IDataType? _dtHeroSlidesBlockList;
    private IDataType? _dtFeatureIconLinksBlockList;
    private IDataType? _dtDigitalChannelsBlockList;

    private readonly IConfigurationEditorJsonSerializer _configurationEditorJsonSerializer;
    private readonly PropertyEditorCollection _propertyEditorCollection;

    public DocumentTypeMigrationHandler(
        IContentTypeService contentTypeService,
        IDataTypeService dataTypeService,
        IShortStringHelper shortStringHelper,
        ILogger<DocumentTypeMigrationHandler> logger,
        IConfigurationEditorJsonSerializer configurationEditorJsonSerializer,
        PropertyEditorCollection propertyEditorCollection,
        ITemplateService templateService)
    {
        _contentTypeService = contentTypeService;
        _dataTypeService = dataTypeService;
        _shortStringHelper = shortStringHelper;
        _logger = logger;
        _configurationEditorJsonSerializer = configurationEditorJsonSerializer;
        _propertyEditorCollection = propertyEditorCollection;
        _templateService = templateService;
    }

    public async Task HandleAsync(UmbracoApplicationStartedNotification notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("STBWeb: Starting document type migration...");
        try
        {
            await ResolveDataTypesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "STBWeb: Failed to resolve data types — aborting migration.");
            return;
        }

        try
        {
            // ----------------------------------------------------------------
            // ELEMENT TYPES
            // ----------------------------------------------------------------
            await EnsureElementTypeAsync("heroSlide", "Hero Slide", new[]
            {
                P("desktopImage",    "Desktop Image",    _dtMediaPicker!),
                P("mobileImage",     "Mobile Image",     _dtMediaPicker!),
                P("slideHeading",    "Slide Heading",    _dtTextstring!),
                P("slideSubheading", "Slide Subheading", _dtTextstring!),
                P("slideCtaText",    "Slide CTA Text",   _dtTextstring!),
                P("slideCtaUrl",     "Slide CTA URL",    _dtMultiUrlPicker!),
            });

            // Create a dedicated Block List data type configured to allow heroSlide blocks.
            // The generic _dtBlockList has no allowed blocks configured, so backoffice editors
            // cannot add or edit slides without this dedicated, configured data type.
            _dtHeroSlidesBlockList = await EnsureConfiguredBlockListDataTypeAsync(
                "Hero Slides Block List", "heroSlide");

            await EnsureElementTypeAsync("featureIconLink", "Feature Icon Link", new[]
            {
                P("iconMdiClass", "Icon MDI Class", _dtTextstring!),
                P("label",        "Label",          _dtTextstring!),
                P("iconColor",    "Icon Color",     _dtTextstring!),
                P("linkUrl",      "Link URL",       _dtMultiUrlPicker!),
            });

            _dtFeatureIconLinksBlockList = await EnsureConfiguredBlockListDataTypeAsync(
                "Feature Icon Links Block List", "featureIconLink");

            await EnsureElementTypeAsync("faqItem", "FAQ Item", new[]
            {
                P("question", "Question", _dtTextstring!),
                P("answer",   "Answer",   _dtRichText!),
            });

            await EnsureElementTypeAsync("documentLink", "Document Link", new[]
            {
                P("docLabel", "Label", _dtTextstring!),
                P("docFile",  "File",  _dtMediaPicker!),
            });

            await EnsureElementTypeAsync("personProfile", "Person Profile", new[]
            {
                P("photo",     "Photo",     _dtMediaPicker!),
                P("role",      "Role",      _dtTextstring!),
                P("fullName",  "Full Name", _dtTextstring!),
                P("biography", "Biography", _dtRichText!),
            });

            await EnsureElementTypeAsync("promoItem", "Promo Item", new[]
            {
                P("promoTitle", "Title", _dtTextstring!),
                P("promoLink",  "Link",  _dtMultiUrlPicker!),
                P("promoImage", "Image", _dtMediaPicker!),
            });

            await EnsureElementTypeAsync("newsTickerItem", "News Ticker Item", new[]
            {
                P("tickerText", "Text", _dtTextstring!),
                P("tickerLink", "Link", _dtMultiUrlPicker!),
            });

            await EnsureElementTypeAsync("digitalChannel", "Digital Channel", new[]
            {
                P("channelTitle",       "Title",          _dtTextstring!),
                P("channelIcon",        "Icon MDI Class", _dtTextstring!),
                P("channelDescription", "Description",    _dtTextstring!),
                P("channelUrl",         "URL",            _dtMultiUrlPicker!),
            });

            _dtDigitalChannelsBlockList = await EnsureConfiguredBlockListDataTypeAsync(
                "Digital Channels Block List", "digitalChannel");

            // ----------------------------------------------------------------
            // COMPOSITIONS
            // ----------------------------------------------------------------
            await EnsureCompositionAsync("seoComposition", "SEO Composition", new[]
            {
                P("seoTitle",       "SEO Title",       _dtTextstring!),
                P("seoDescription", "SEO Description", _dtTextarea!),
                P("ogImage",        "OG Image",        _dtMediaPicker!),
            });

            await EnsureCompositionAsync("heroBannerComposition", "Hero Banner Composition", new[]
            {
                P("heroBannerImage",      "Hero Banner Image",      _dtMediaPicker!),
                P("heroBannerHeading",    "Hero Banner Heading",    _dtTextstring!),
                P("heroBannerSubheading", "Hero Banner Subheading", _dtTextstring!),
                P("heroCtaText",          "Hero CTA Text",          _dtTextstring!),
                P("heroCtaLink",          "Hero CTA Link",          _dtMultiUrlPicker!),
            });

            await EnsureCompositionAsync("sidebarComposition", "Sidebar Composition", new[]
            {
                P("showSidebar",         "Show Sidebar",          _dtTrueFalse!),
                P("sidebarRelatedLinks", "Sidebar Related Links", _dtMultiUrlPicker!),
                P("sidebarPromoItems",   "Sidebar Promo Items",   _dtBlockList!),
            });

            // ----------------------------------------------------------------
            // DOCUMENT TYPES
            // Each creation is wrapped individually so that a failure in one
            // (e.g. UnauthorizedAccessException from file rename) does not
            // prevent the remaining document types from being created.
            // ----------------------------------------------------------------
            await SafeEnsureDocTypeAsync("homePage", "Home Page",
                compositions: ["seoComposition"],
                allowedAtRoot: true,
                allowedChildren: ["sectionRoot", "newsListingPage", "subBrandHomePage", "genericContentPage"],
                properties:
                [
                    P("heroSlides",       "Hero Slides",       _dtHeroSlidesBlockList ?? _dtBlockList!),
                    P("featureIconLinks", "Feature Icon Links", _dtBlockList!),
                    P("applyOnlineImage", "Apply Online Image", _dtMediaPicker!),
                    P("applyOnlineUrl",   "Apply Online URL",   _dtMultiUrlPicker!),
                    P("promoBanner1",     "Promo Banner 1",     _dtContentPicker!),
                    P("promoBanner2",     "Promo Banner 2",     _dtContentPicker!),
                ]);

            await SafeEnsureDocTypeAsync("sectionRoot", "Section Root",
                compositions: ["seoComposition", "heroBannerComposition"],
                allowedAtRoot: false,
                allowedChildren: ["sectionRoot", "productDetailPage", "productListingPage", "genericContentPage", "exchangeRatePage", "locationMapPage", "calculatorsPage", "corporateGovernancePage", "newsListingPage"],
                properties:
                [
                    P("bodyContent", "Body Content", _dtRichText!),
                ]);

            await SafeEnsureDocTypeAsync("productDetailPage", "Product Detail Page",
                compositions: ["seoComposition", "heroBannerComposition", "sidebarComposition"],
                allowedAtRoot: false,
                allowedChildren: [],
                properties:
                [
                    P("tabProductInfo",     "Tab: Product Info",         _dtRichText!),
                    P("tabPricing",         "Tab: Pricing",              _dtRichText!),
                    P("tabCriteriaAndDocs", "Tab: Criteria & Documents", _dtRichText!),
                    P("tabSecurity",        "Tab: Security",             _dtRichText!),
                    P("tabInterestRates",   "Tab: Interest Rates",       _dtRichText!),
                    P("applyOnlineUrl",     "Apply Online URL",          _dtMultiUrlPicker!),
                    P("faqItems",           "FAQ Items",                 _dtBlockList!),
                ]);

            await SafeEnsureDocTypeAsync("productListingPage", "Product Listing Page",
                compositions: ["seoComposition", "heroBannerComposition", "sidebarComposition"],
                allowedAtRoot: false,
                allowedChildren: ["productDetailPage", "sectionRoot"],
                properties:
                [
                    P("bodyContent", "Body Content", _dtRichText!),
                ]);

            await SafeEnsureDocTypeAsync("newsListingPage", "News Listing Page",
                compositions: ["seoComposition", "heroBannerComposition"],
                allowedAtRoot: false,
                allowedChildren: ["newsDetailPage"],
                properties:
                [
                    P("newsPerPage", "News Per Page", _dtNumeric!),
                ]);

            await SafeEnsureDocTypeAsync("newsDetailPage", "News Detail Page",
                compositions: ["seoComposition"],
                allowedAtRoot: false,
                allowedChildren: [],
                properties:
                [
                    P("publishDate",    "Publish Date",    _dtDatetime!),
                    P("summary",        "Summary",         _dtTextarea!),
                    P("thumbnailImage", "Thumbnail Image", _dtMediaPicker!),
                    P("articleBody",    "Article Body",    _dtRichText!),
                ]);

            await SafeEnsureDocTypeAsync("genericContentPage", "Generic Content Page",
                compositions: ["seoComposition", "heroBannerComposition", "sidebarComposition"],
                allowedAtRoot: false,
                allowedChildren: ["genericContentPage", "sectionRoot"],
                properties:
                [
                    P("bodyContent",       "Body Content",       _dtRichText!),
                    P("documentDownloads", "Document Downloads", _dtBlockList!),
                ]);

            await SafeEnsureDocTypeAsync("exchangeRatePage", "Exchange Rate Page",
                compositions: ["seoComposition", "heroBannerComposition", "sidebarComposition"],
                allowedAtRoot: false,
                allowedChildren: [],
                properties:
                [
                    P("rateApiEndpoint", "Rate API Endpoint", _dtTextstring!),
                ]);

            await SafeEnsureDocTypeAsync("locationMapPage", "Location Map Page",
                compositions: ["seoComposition", "heroBannerComposition"],
                allowedAtRoot: false,
                allowedChildren: [],
                properties:
                [
                    P("mapType",              "Map Type",               _dtDropdown!),
                    P("locationsApiEndpoint", "Locations API Endpoint", _dtTextstring!),
                    P("mapDefaultCenter",     "Map Default Center",     _dtTextstring!),
                ]);

            await SafeEnsureDocTypeAsync("calculatorsPage", "Calculators Page",
                compositions: ["seoComposition", "heroBannerComposition", "sidebarComposition"],
                allowedAtRoot: false,
                allowedChildren: [],
                properties:
                [
                    P("calculatorConfig", "Calculator Config", _dtTextarea!),
                ]);

            await SafeEnsureDocTypeAsync("corporateGovernancePage", "Corporate Governance Page",
                compositions: ["seoComposition", "heroBannerComposition", "sidebarComposition"],
                allowedAtRoot: false,
                allowedChildren: ["genericContentPage"],
                properties:
                [
                    P("managementBoard",  "Management Board",    _dtBlockList!),
                    P("supervisoryBoard", "Supervisory Board",   _dtBlockList!),
                    P("orgChartImage",    "Org Chart Image",     _dtMediaPicker!),
                    P("governanceDocs",   "Governance Documents", _dtBlockList!),
                ]);

            await SafeEnsureDocTypeAsync("subBrandHomePage", "Sub-Brand Home Page",
                compositions: ["seoComposition"],
                allowedAtRoot: false,
                allowedChildren: ["genericContentPage"],
                properties:
                [
                    P("subbrandKey",   "Sub-Brand Key",  _dtDropdown!),
                    P("themeCssClass", "Theme CSS Class", _dtTextstring!),
                    P("logo",          "Logo",            _dtMediaPicker!),
                    P("heroSlides",    "Hero Slides",     _dtBlockList!),
                    P("bodyContent",   "Body Content",    _dtRichText!),
                ]);

            // Ensure the existing homePage.heroSlides property uses the configured data type.
            // This update path runs even when the document type already exists in the DB,
            // so the migration is fully idempotent on subsequent startups.
            await EnsureHomepageHeroSlidesDataTypeAsync();
            await EnsureHomepagePropertyDataTypeAsync("featureIconLinks", _dtFeatureIconLinksBlockList);

            // EnsureHomepageAdditionalPropertiesAsync MUST run before
            // EnsureHomepagePropertyDataTypeAsync("digitalChannels") because
            // "digitalChannels" lives in the "additional" group and is only
            // created by the former. Calling the update first would log
            // "Property 'digitalChannels' not found" and return without effect.
            await EnsureHomepageAdditionalPropertiesAsync();
            await EnsureHomepagePropertyDataTypeAsync("digitalChannels", _dtDigitalChannelsBlockList);

            _logger.LogInformation("STBWeb: Document type migration completed successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "STBWeb: Document type migration failed.");
        }

        await Task.CompletedTask;
    }

    // -----------------------------------------------------------------------
    // Resolve data types by alias (Umbraco 15 removes Constants.DataTypes.Guids)
    // -----------------------------------------------------------------------
    private async Task ResolveDataTypesAsync()
    {
        var all = (await _dataTypeService.GetAllAsync()).ToList();
        _logger.LogInformation("STBWeb: Found {Count} data types in database. Names: {Names}",
            all.Count, string.Join(", ", all.Select(d => $"{d.Name} [{d.EditorAlias}]")));

        // Helper to find by EditorAlias
        IDataType? FindByAlias(string alias) => all.FirstOrDefault(d => d.EditorAlias.Equals(alias, StringComparison.OrdinalIgnoreCase));

        _dtTextstring = FindByAlias(Constants.PropertyEditors.Aliases.TextBox);
        _dtTextarea = FindByAlias(Constants.PropertyEditors.Aliases.TextArea);
        _dtRichText = FindByAlias(Constants.PropertyEditors.Aliases.RichText);
        _dtNumeric = FindByAlias(Constants.PropertyEditors.Aliases.Integer);
        _dtTrueFalse = FindByAlias(Constants.PropertyEditors.Aliases.Boolean);
        _dtDatetime = FindByAlias(Constants.PropertyEditors.Aliases.DateTime);
        _dtMediaPicker = FindByAlias(Constants.PropertyEditors.Aliases.MediaPicker3);
        _dtMultiUrlPicker = FindByAlias(Constants.PropertyEditors.Aliases.MultiUrlPicker);
        _dtContentPicker = FindByAlias(Constants.PropertyEditors.Aliases.ContentPicker);
        _dtDropdown = FindByAlias(Constants.PropertyEditors.Aliases.DropDownListFlexible);

        // Look up by Editor Alias instead of a hardcoded GUID
        var blockListDataTypes = await _dataTypeService.GetByEditorAliasAsync(Umbraco.Cms.Core.Constants.PropertyEditors.Aliases.BlockList);
        var blockListDataType = blockListDataTypes.FirstOrDefault(x => x.Name != null && x.Name.Equals("Block List", StringComparison.InvariantCultureIgnoreCase))
                                ?? blockListDataTypes.FirstOrDefault();

        if (blockListDataType == null)
        {
            _logger.LogWarning("STBWeb: Block List data type not found. Creating it programmatically...");

            // Find the Block List property editor
            if (_propertyEditorCollection.TryGet(Constants.PropertyEditors.Aliases.BlockList, out var blockListEditor))
            {
                var newBlockList = new DataType(blockListEditor, _configurationEditorJsonSerializer, -1)
                {
                    Name = "Block List",
                    DatabaseType = ValueStorageType.Ntext,
                    EditorUiAlias = "Umb.PropertyEditorUi.BlockList",
                };
                await _dataTypeService.CreateAsync(newBlockList, Constants.Security.SuperUserKey);
                blockListDataType = newBlockList;
                _logger.LogInformation("STBWeb: Created 'Block List' data type successfully.");
            }
            else
            {
                _logger.LogError("STBWeb: Block List property editor not found in PropertyEditorCollection. Cannot proceed.");
                throw new InvalidOperationException("Missing Block List property editor.");
            }
        }

        _dtBlockList = blockListDataType;

        // Fix missing EditorUiAlias on the generic Block List (needed by Umbraco 15 Bellissima UI)
        if (blockListDataType != null && string.IsNullOrEmpty(blockListDataType.EditorUiAlias))
        {
            blockListDataType.EditorUiAlias = "Umb.PropertyEditorUi.BlockList";
            await _dataTypeService.UpdateAsync(blockListDataType, Constants.Security.SuperUserKey);
            _logger.LogInformation("STBWeb: Set EditorUiAlias on generic 'Block List' data type (ID {Id}).", blockListDataType.Id);
        }

        // Final fallback validation
        if (_dtTextstring == null || _dtMediaPicker == null)
        {
            _logger.LogWarning("STBWeb: Critical core DataTypes missing. Falling back to first available type to prevent crashes.");
            var fallback = all.FirstOrDefault();
            _dtTextstring ??= fallback;
            _dtTextarea ??= fallback;
            _dtRichText ??= fallback;
            _dtNumeric ??= fallback;
            _dtTrueFalse ??= fallback;
            _dtDatetime ??= fallback;
            _dtMediaPicker ??= fallback;
            _dtMultiUrlPicker ??= fallback;
            _dtContentPicker ??= fallback;
            _dtDropdown ??= fallback;
            _dtBlockList ??= fallback;
        }
    }

    // -----------------------------------------------------------------------
    // Factory: PropertyTypeDefinition
    // -----------------------------------------------------------------------
    private PropDef P(string alias, string name, IDataType dataType) =>
        new(alias, name, dataType);

    // -----------------------------------------------------------------------
    // EnsureElementTypeAsync
    // -----------------------------------------------------------------------
    private async Task EnsureElementTypeAsync(string alias, string name, PropDef[] properties)
    {
        if (_contentTypeService.Get(alias) != null)
        {
            _logger.LogInformation("STBWeb: Element type '{Alias}' already exists.", alias);
            return;
        }

        var ct = new ContentType(_shortStringHelper, -1)
        {
            Alias = alias,
            Name = name,
            IsElement = true,
            AllowedAsRoot = false,
        };

        AddPropertiesTo(ct, properties);
        await _contentTypeService.CreateAsync(ct, Constants.Security.SuperUserKey);
        _logger.LogInformation("STBWeb: Created element type '{Alias}'.", alias);
    }

    // -----------------------------------------------------------------------
    // EnsureCompositionAsync
    // -----------------------------------------------------------------------
    private async Task EnsureCompositionAsync(string alias, string name, PropDef[] properties)
    {
        if (_contentTypeService.Get(alias) != null)
        {
            _logger.LogInformation("STBWeb: Composition '{Alias}' already exists.", alias);
            return;
        }

        var ct = new ContentType(_shortStringHelper, -1)
        {
            Alias = alias,
            Name = name,
            IsElement = false,
            AllowedAsRoot = false,
        };

        AddPropertiesTo(ct, properties);
        await _contentTypeService.CreateAsync(ct, Constants.Security.SuperUserKey);
        _logger.LogInformation("STBWeb: Created composition '{Alias}'.", alias);
    }

    // -----------------------------------------------------------------------
    // SafeEnsureDocTypeAsync — wraps EnsureDocumentTypeAsync so that a
    // failure (e.g. UnauthorizedAccessException from file rename) is logged
    // but does not prevent subsequent document types from being created.
    // -----------------------------------------------------------------------
    private async Task SafeEnsureDocTypeAsync(
        string alias,
        string name,
        string[] compositions,
        bool allowedAtRoot,
        string[] allowedChildren,
        PropDef[] properties)
    {
        try
        {
            await EnsureDocumentTypeAsync(alias, name, compositions, allowedAtRoot, allowedChildren, properties);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "STBWeb: Failed to create document type '{Alias}' — continuing with remaining types.", alias);
        }
    }

    // -----------------------------------------------------------------------
    // EnsureDocumentTypeAsync
    // -----------------------------------------------------------------------
    private async Task EnsureDocumentTypeAsync(
        string alias,
        string name,
        string[] compositions,
        bool allowedAtRoot,
        string[] allowedChildren,
        PropDef[] properties)
    {
        // ── Ensure Template exists ──────────────────────────────────────────
        // Template alias must match the document type alias exactly so that
        // Umbraco resolves Views/{Alias}.cshtml automatically.
        var templateAlias = alias;
        // Template name uses PascalCase display name derived from alias
        var templateName = name;

        var existingTemplate = await _templateService.GetAsync(templateAlias);
        if (existingTemplate == null)
        {
            // Convert alias to PascalCase filename that our source .cshtml files use
            // e.g., "homePage" → "HomePage.cshtml"
            var pascalName = char.ToUpperInvariant(templateAlias[0]) + templateAlias.Substring(1);
            var viewsDir = Path.Combine(Directory.GetCurrentDirectory(), "Views");
            var altViewPath = Path.Combine(viewsDir, $"{pascalName}.cshtml");
            var viewPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "Views", $"{pascalName}.cshtml");
            // Also check for the camelCase filename (in case it was already renamed)
            var camelViewPath = Path.Combine(viewsDir, $"{templateAlias}.cshtml");


            string templateContent = @"@inherits Umbraco.Cms.Web.Common.Views.UmbracoViewPage
@{
    Layout = ""_Layout.cshtml"";
}";
            // Try to read the existing physical .cshtml file so CreateAsync doesn't overwrite it
            string sourceViewPath = null;
            if (System.IO.File.Exists(camelViewPath))
            {
                templateContent = System.IO.File.ReadAllText(camelViewPath);
                sourceViewPath = camelViewPath;
                _logger.LogInformation("STBWeb: Read existing view file for template '{Alias}' from {Path}.", templateAlias, camelViewPath);
            }
            else if (System.IO.File.Exists(altViewPath))
            {
                templateContent = System.IO.File.ReadAllText(altViewPath);
                sourceViewPath = altViewPath;
                _logger.LogInformation("STBWeb: Read existing view file for template '{Alias}' from {Path}.", templateAlias, altViewPath);
            }
            else if (System.IO.File.Exists(viewPath))
            {
                templateContent = System.IO.File.ReadAllText(viewPath);
                sourceViewPath = viewPath;
                _logger.LogInformation("STBWeb: Read existing view file for template '{Alias}' from {Path}.", templateAlias, viewPath);
            }
            else
            {
                _logger.LogWarning("STBWeb: No physical .cshtml found for '{Alias}', using default stub.", templateAlias);
            }

            // FIX: Umbraco's CreateAsync reads the physical .cshtml file from disk
            // and validates its Layout directive. We must temporarily hide the file
            // so CreateAsync uses only our stub content (no Layout reference).
            var expectedPath = Path.Combine(viewsDir, $"{templateAlias}.cshtml");
            string tempPath = expectedPath + ".bak";
            bool hidFile = false;
            if (System.IO.File.Exists(expectedPath))
            {
                try
                {
                    System.IO.File.Move(expectedPath, tempPath, true);
                    hidFile = true;
                    _logger.LogInformation("STBWeb: Temporarily renamed '{Path}' before CreateAsync.", expectedPath);
                }
                catch (Exception moveEx)
                {
                    _logger.LogWarning(moveEx, "STBWeb: Could not rename '{Path}' — proceeding without hiding the file.", expectedPath);
                }
            }

            var template = new Template(_shortStringHelper, templateName, templateAlias);
            template.Content = "@inherits Umbraco.Cms.Web.Common.Views.UmbracoViewPage\n";
            var createResult = await _templateService.CreateAsync(template, Constants.Security.SuperUserKey);
            _logger.LogWarning("STBWeb DIAG: CreateAsync '{Alias}': Success={Success}, Status={Status}",
                templateAlias, createResult.Success, createResult.Status.ToString());

            if (!createResult.Success)
            {
                _logger.LogError("STBWeb DIAG: FAILED '{Alias}' Status: {Status}",
                    templateAlias, createResult.Status.ToString());
            }

            // Now restore the original .cshtml file (overwrite the stub CreateAsync wrote)
            if (hidFile && System.IO.File.Exists(tempPath))
            {
                System.IO.File.Move(tempPath, expectedPath, true);
                _logger.LogInformation("STBWeb: Restored original view file '{Path}'.", expectedPath);
            }
            else if (sourceViewPath != null && !System.IO.File.Exists(expectedPath))
            {
                // Copy from original source if needed
                try
                {
                    System.IO.File.Copy(sourceViewPath, expectedPath, true);
                    _logger.LogInformation("STBWeb: Copied view file to '{Path}'.", expectedPath);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "STBWeb: Could not copy view file to '{Path}'.", expectedPath);
                }
            }

            existingTemplate = await _templateService.GetAsync(templateAlias);
            _logger.LogWarning("STBWeb DIAG: GetAsync '{Alias}' = {Result}",
                templateAlias, existingTemplate != null ? existingTemplate.Alias : "NULL");
        }

        if (_contentTypeService.Get(alias) != null)
        {
            // Document type exists — but ensure its template is assigned
            var existingCt = _contentTypeService.Get(alias);
            if (existingCt != null && existingTemplate != null && existingCt.DefaultTemplate?.Alias != templateAlias)
            {
                existingCt.AllowedTemplates = existingCt.AllowedTemplates
                    .Append(existingTemplate)
                    .DistinctBy(t => t.Alias)
                    .ToList();
                existingCt.SetDefaultTemplate(existingTemplate);
                await _contentTypeService.UpdateAsync(existingCt, Constants.Security.SuperUserKey);
                _logger.LogInformation("STBWeb: Assigned template '{Template}' to existing doc type '{Alias}'.", templateAlias, alias);
            }
            else
            {
                _logger.LogInformation("STBWeb: Document type '{Alias}' already exists with correct template.", alias);
            }
            return;
        }

        var ct = new ContentType(_shortStringHelper, -1)
        {
            Alias = alias,
            Name = name,
            AllowedAsRoot = allowedAtRoot,
            Variations = ContentVariation.Culture,
        };

        // Assign template to new document type
        if (existingTemplate != null)
        {
            ct.AllowedTemplates = new[] { existingTemplate };
            ct.SetDefaultTemplate(existingTemplate);
        }

        foreach (var compAlias in compositions)
        {
            var comp = _contentTypeService.Get(compAlias);
            if (comp != null)
                ct.AddContentType(comp);
            else
                _logger.LogWarning("STBWeb: Composition '{CompAlias}' not found for '{Alias}'.", compAlias, alias);
        }

        AddPropertiesTo(ct, properties);
        await _contentTypeService.CreateAsync(ct, Constants.Security.SuperUserKey);

        if (allowedChildren.Length > 0)
        {
            var allowed = new List<ContentTypeSort>();
            int order = 0;
            foreach (var childAlias in allowedChildren)
            {
                var child = _contentTypeService.Get(childAlias);
                if (child != null)
                    allowed.Add(new ContentTypeSort(child.Key, order++, child.Alias));
            }
            ct.AllowedContentTypes = allowed;
            await _contentTypeService.UpdateAsync(ct, Constants.Security.SuperUserKey);
        }

        _logger.LogInformation("STBWeb: Created document type '{Alias}' with template.", alias);
    }

    // -----------------------------------------------------------------------
    // AddPropertiesTo
    // -----------------------------------------------------------------------
    private void AddPropertiesTo(ContentType ct, PropDef[] properties)
    {
        if (properties.Length == 0) return;

        var group = new PropertyGroup(true)
        {
            Name = "Content",
            Alias = "content",
            SortOrder = 0,
        };

        foreach (var p in properties)
        {
            var pt = new PropertyType(_shortStringHelper, p.DataType, p.Alias)
            {
                Name = p.Name,
            };
            group.PropertyTypes!.Add(pt);
        }

        ct.PropertyGroups.Add(group);
    }

    // -----------------------------------------------------------------------
    // EnsureConfiguredBlockListDataTypeAsync
    // Creates a Block List data type that explicitly lists which element type
    // is allowed. Without this configuration the backoffice Block List picker
    // shows an empty "Add block" menu and editors cannot add or edit blocks.
    // The method is idempotent: it returns the existing data type when a type
    // with the same name already exists.
    // -----------------------------------------------------------------------
    private async Task<IDataType> EnsureConfiguredBlockListDataTypeAsync(
        string name,
        string elementTypeAlias)
    {
        var elementType = _contentTypeService.Get(elementTypeAlias);
        if (elementType == null)
        {
            _logger.LogWarning("STBWeb: Element type '{Alias}' not found; falling back to generic Block List for '{Name}'.", elementTypeAlias, name);
            return _dtBlockList!;
        }

        if (!_propertyEditorCollection.TryGet(Constants.PropertyEditors.Aliases.BlockList, out var blockListEditor))
        {
            _logger.LogError("STBWeb: Block List property editor not found; falling back to generic Block List for '{Name}'.", name);
            return _dtBlockList!;
        }

        // Build the typed BlockListConfiguration, then use the editor's own
        // ConfigurationEditor.FromConfigurationObject() to produce the exact
        // ConfigurationData dictionary that Umbraco can round-trip correctly.
        // Previous approach used raw dictionaries which broke deserialization
        // (string GUIDs vs typed Guid, wrong property casing, etc.).
        var typedConfig = new BlockListConfiguration
        {
            Blocks = new[]
            {
                new BlockListConfiguration.BlockConfiguration
                {
                    ContentElementTypeKey = elementType.Key,
                    SettingsElementTypeKey = null,
                }
            },
            UseSingleBlockMode = false,
        };

        var configEditor = blockListEditor.GetConfigurationEditor();
        var correctConfig = configEditor.FromConfigurationObject(typedConfig, _configurationEditorJsonSerializer);

        _logger.LogInformation(
            "STBWeb: Built Block List config for '{Name}' via FromConfigurationObject. Keys=[{Keys}], ElementKey={ElementKey}",
            name,
            string.Join(", ", correctConfig.Keys),
            elementType.Key);

        // Search by name only — this catches data types that were created without a
        // proper editor (showing "Select a property editor" in the backoffice, which
        // means EditorAlias is empty/wrong and a name-AND-alias filter would miss them).
        var all = await _dataTypeService.GetAllAsync();
        var existing = all.FirstOrDefault(d =>
            d.Name != null &&
            d.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

        if (existing != null)
        {
            // Check whether the editor alias is correct (catches broken/unconfigured data types).
            var editorIsCorrect = existing.EditorAlias.Equals(
                Constants.PropertyEditors.Aliases.BlockList, StringComparison.OrdinalIgnoreCase);

            // Fix wrong/empty editor alias in-place so the DB record's ID/Key stays the
            // same and any property already pointing at it is automatically fixed.
            if (!editorIsCorrect && existing is DataType concreteDataType)
            {
                _logger.LogInformation("STBWeb: Block List data type '{Name}' has wrong/empty editor; reassigning Block List editor.", name);
                concreteDataType.Editor = blockListEditor;
            }

            // Use ToConfigurationObject to read the typed config back and verify
            // the correct element type is registered as an allowed block.
            var existingConfig = configEditor.ToConfigurationObject(
                existing.ConfigurationData ?? new Dictionary<string, object>(),
                _configurationEditorJsonSerializer) as BlockListConfiguration;

            var hasCorrectBlock = existingConfig?.Blocks != null
                && existingConfig.Blocks.Any(b => b.ContentElementTypeKey == elementType.Key);

            // Fix missing EditorUiAlias (required by Umbraco 15 Bellissima UI)
            var needsUiAlias = string.IsNullOrEmpty(existing.EditorUiAlias);

            if (editorIsCorrect && hasCorrectBlock && !needsUiAlias)
            {
                _logger.LogInformation("STBWeb: Block List data type '{Name}' already configured with '{ElementType}'.", name, elementTypeAlias);
                return existing;
            }

            if (needsUiAlias)
            {
                existing.EditorUiAlias = "Umb.PropertyEditorUi.BlockList";
                _logger.LogInformation("STBWeb: Setting EditorUiAlias on '{Name}'.", name);
            }

            if (!hasCorrectBlock)
            {
                _logger.LogInformation(
                    "STBWeb: Block List data type '{Name}' exists but missing '{ElementType}' block (found {Count} blocks); updating config.",
                    name, elementTypeAlias, existingConfig?.Blocks?.Length ?? 0);
                existing.ConfigurationData = correctConfig;
            }

            await _dataTypeService.UpdateAsync(existing, Constants.Security.SuperUserKey);
            _logger.LogInformation("STBWeb: Fixed '{Name}' Block List data type — editor and blocks config updated for '{ElementType}'.", name, elementTypeAlias);
            return existing;
        }

        // No data type with this name exists at all — create it fresh.
        var dataType = new DataType(blockListEditor, _configurationEditorJsonSerializer, -1)
        {
            Name = name,
            DatabaseType = ValueStorageType.Ntext,
            ConfigurationData = correctConfig,
            // Umbraco 15's Bellissima UI requires the EditorUiAlias to render the
            // correct property editor component. Without it the backoffice shows the
            // property as read-only with no controls.
            EditorUiAlias = "Umb.PropertyEditorUi.BlockList",
        };

        await _dataTypeService.CreateAsync(dataType, Constants.Security.SuperUserKey);
        _logger.LogInformation("STBWeb: Created '{Name}' Block List data type allowing '{ElementType}'.", name, elementTypeAlias);
        return dataType;
    }

    // -----------------------------------------------------------------------
    // EnsureHomepagePropertyDataTypeAsync
    // Generic helper: updates any named property on homePage to use a given
    // data type. No-op if the property already uses that data type.
    // -----------------------------------------------------------------------
    private async Task EnsureHomepagePropertyDataTypeAsync(string propertyAlias, IDataType? targetDataType)
    {
        if (targetDataType == null) return;

        var homePage = _contentTypeService.Get("homePage");
        if (homePage == null) return;

        IPropertyType? pt = null;
        foreach (var group in homePage.PropertyGroups)
        {
            pt = group.PropertyTypes?.FirstOrDefault(p => p.Alias == propertyAlias);
            if (pt != null) break;
        }

        if (pt == null)
        {
            _logger.LogWarning("STBWeb: Property '{Alias}' not found on homePage — cannot assign data type (id={Id} key={Key}).",
                propertyAlias, targetDataType.Id, targetDataType.Key);
            return;
        }

        if (pt.DataTypeKey == targetDataType.Key)
        {
            _logger.LogInformation("STBWeb: homePage.{Alias} already uses correct data type (id={Id} key={Key}).",
                propertyAlias, targetDataType.Id, targetDataType.Key);
            return;
        }

        _logger.LogInformation("STBWeb: homePage.{Alias} — reassigning data type from key={OldKey} to id={NewId} key={NewKey}.",
            propertyAlias, pt.DataTypeKey, targetDataType.Id, targetDataType.Key);
        pt.DataTypeId = targetDataType.Id;
        pt.DataTypeKey = targetDataType.Key;
        await _contentTypeService.UpdateAsync(homePage, Constants.Security.SuperUserKey);
        _logger.LogInformation("STBWeb: homePage.{Alias} — data type assignment saved (id={Id} key={Key}).",
            propertyAlias, targetDataType.Id, targetDataType.Key);
    }

    // -----------------------------------------------------------------------
    // EnsureHomepageAdditionalPropertiesAsync
    // Adds homepage text-label, digital-channel, and footer properties that
    // may not exist yet (idempotent: checks existence before adding).
    // -----------------------------------------------------------------------
    private async Task EnsureHomepageAdditionalPropertiesAsync()
    {
        var homePage = _contentTypeService.Get("homePage");
        if (homePage == null) return;

        var existing = homePage.PropertyGroups
            .SelectMany(g => g.PropertyTypes ?? Enumerable.Empty<IPropertyType>())
            .Select(p => p.Alias)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var toAdd = new List<(string groupAlias, string groupName, int sortOrder, PropDef prop)>();

        void AddIfMissing(string groupAlias, string groupName, int sortOrder, string alias, string name, IDataType dt)
        {
            if (!existing.Contains(alias))
                toAdd.Add((groupAlias, groupName, sortOrder, P(alias, name, dt)));
        }

        // ── Core content properties (may be missing if first CreateAsync was partial) ─
        AddIfMissing("content", "Content", 0, "heroSlides",       "Hero Slides",        _dtHeroSlidesBlockList ?? _dtBlockList!);
        AddIfMissing("content", "Content", 0, "featureIconLinks", "Feature Icon Links",  _dtFeatureIconLinksBlockList ?? _dtBlockList!);
        AddIfMissing("content", "Content", 0, "applyOnlineImage", "Apply Online Image",  _dtMediaPicker!);
        AddIfMissing("content", "Content", 0, "applyOnlineUrl",   "Apply Online URL",    _dtMultiUrlPicker!);
        AddIfMissing("content", "Content", 0, "promoBanner1",     "Promo Banner 1",      _dtContentPicker!);
        AddIfMissing("content", "Content", 0, "promoBanner2",     "Promo Banner 2",      _dtContentPicker!);

        // ── Section text labels ───────────────────────────────────────────
        AddIfMissing("homepageLabels", "Homepage Labels", 10, "whatINeedTitle",        "What I Need Section Title",    _dtTextstring!);
        AddIfMissing("homepageLabels", "Homepage Labels", 10, "applyOnlineTitle",      "Apply Online Card Title",      _dtTextstring!);
        AddIfMissing("homepageLabels", "Homepage Labels", 10, "applyOnlineButtonText", "Apply Online Button Text",     _dtTextstring!);
        AddIfMissing("homepageLabels", "Homepage Labels", 10, "exchangeRateTitle",     "Exchange Rate Section Title",  _dtTextstring!);
        AddIfMissing("homepageLabels", "Homepage Labels", 10, "exchangeRateAllLabel",  "Exchange Rate All Link Label", _dtTextstring!);
        AddIfMissing("homepageLabels", "Homepage Labels", 10, "exchangeRateAllUrl",    "Exchange Rate All Link URL",   _dtMultiUrlPicker!);
        AddIfMissing("homepageLabels", "Homepage Labels", 10, "latestNewsTitle",       "Latest News Section Title",    _dtTextstring!);
        AddIfMissing("homepageLabels", "Homepage Labels", 10, "latestNewsAllLabel",    "Latest News All Link Label",   _dtTextstring!);
        AddIfMissing("homepageLabels", "Homepage Labels", 10, "newsReadMoreLabel",     "News Read More Label",         _dtTextstring!);

        // ── Digital Channels block list ───────────────────────────────────
        AddIfMissing("digitalChannelsGroup", "Digital Channels", 20, "digitalChannels",     "Digital Channels",           _dtDigitalChannelsBlockList ?? _dtBlockList!);
        AddIfMissing("digitalChannelsGroup", "Digital Channels", 20, "digitalChannelsTitle","Digital Channels Section Title", _dtTextstring!);

        // ── Promo Banners — two fixed banners, each with title / image / link ────────
        AddIfMissing("promoBanner1Group", "Promo Banner 1", 25, "promoBanner1Title", "Title", _dtTextstring!);
        AddIfMissing("promoBanner1Group", "Promo Banner 1", 25, "promoBanner1Image", "Image", _dtMediaPicker!);
        AddIfMissing("promoBanner1Group", "Promo Banner 1", 25, "promoBanner1Link",  "Link",  _dtMultiUrlPicker!);
        AddIfMissing("promoBanner2Group", "Promo Banner 2", 26, "promoBanner2Title", "Title", _dtTextstring!);
        AddIfMissing("promoBanner2Group", "Promo Banner 2", 26, "promoBanner2Image", "Image", _dtMediaPicker!);
        AddIfMissing("promoBanner2Group", "Promo Banner 2", 26, "promoBanner2Link",  "Link",  _dtMultiUrlPicker!);

        // ── Footer settings ───────────────────────────────────────────────
        AddIfMissing("footerSettings", "Footer Settings", 30, "footerColumn1Title",   "Footer Column 1 Title",    _dtTextstring!);
        AddIfMissing("footerSettings", "Footer Settings", 30, "footerColumn1Links",   "Footer Column 1 Links",    _dtMultiUrlPicker!);
        AddIfMissing("footerSettings", "Footer Settings", 30, "footerColumn2Title",   "Footer Column 2 Title",    _dtTextstring!);
        AddIfMissing("footerSettings", "Footer Settings", 30, "footerColumn2Links",   "Footer Column 2 Links",    _dtMultiUrlPicker!);
        AddIfMissing("footerSettings", "Footer Settings", 30, "footerColumn3Title",   "Footer Column 3 Title",    _dtTextstring!);
        AddIfMissing("footerSettings", "Footer Settings", 30, "footerColumn3Links",   "Footer Column 3 Links",    _dtMultiUrlPicker!);
        AddIfMissing("footerSettings", "Footer Settings", 30, "footerColumn4Title",   "Footer Column 4 Title",    _dtTextstring!);
        AddIfMissing("footerSettings", "Footer Settings", 30, "footerColumn4Links",   "Footer Column 4 Links",    _dtMultiUrlPicker!);
        AddIfMissing("footerSettings", "Footer Settings", 30, "footerLogo",           "Footer Logo",              _dtMediaPicker!);
        AddIfMissing("footerSettings", "Footer Settings", 30, "footerCopyrightLinks", "Footer Copyright Links",   _dtMultiUrlPicker!);
        AddIfMissing("footerSettings", "Footer Settings", 30, "footerSocialLinks",    "Footer Social Links",      _dtMultiUrlPicker!);

        if (toAdd.Count == 0)
        {
            _logger.LogInformation("STBWeb: homePage additional properties already all present.");
            return;
        }

        // Group properties by their target property group
        var byGroup = toAdd.GroupBy(x => x.groupAlias);
        foreach (var grp in byGroup)
        {
            var first = grp.First();
            var group = homePage.PropertyGroups.FirstOrDefault(g => g.Alias == grp.Key);
            if (group == null)
            {
                group = new PropertyGroup(true)
                {
                    Name = first.groupName,
                    Alias = grp.Key,
                    SortOrder = first.sortOrder,
                };
                homePage.PropertyGroups.Add(group);
            }

            foreach (var (_, _, _, prop) in grp)
            {
                var pt = new PropertyType(_shortStringHelper, prop.DataType, prop.Alias) { Name = prop.Name };
                group.PropertyTypes!.Add(pt);
            }
        }

        await _contentTypeService.UpdateAsync(homePage, Constants.Security.SuperUserKey);
        _logger.LogInformation("STBWeb: Added {Count} additional properties to homePage.", toAdd.Count);
    }

    // -----------------------------------------------------------------------
    // EnsureHomepageHeroSlidesDataTypeAsync
    // Updates the heroSlides property on the existing homePage document type
    // to use the dedicated "Hero Slides Block List" data type. This runs on
    // every startup but is a no-op when already up-to-date.
    // -----------------------------------------------------------------------
    private async Task EnsureHomepageHeroSlidesDataTypeAsync()
    {
        if (_dtHeroSlidesBlockList == null)
        {
            return;
        }

        var homePage = _contentTypeService.Get("homePage");
        if (homePage == null)
        {
            return;
        }

        IPropertyType? heroSlidesPt = null;
        foreach (var group in homePage.PropertyGroups)
        {
            heroSlidesPt = group.PropertyTypes?.FirstOrDefault(p => p.Alias == "heroSlides");
            if (heroSlidesPt != null)
            {
                break;
            }
        }

        if (heroSlidesPt == null)
        {
            _logger.LogWarning("STBWeb: heroSlides property not found on homePage; cannot update data type.");
            return;
        }

        if (heroSlidesPt.DataTypeKey == _dtHeroSlidesBlockList.Key)
        {
            _logger.LogInformation("STBWeb: homePage.heroSlides already uses 'Hero Slides Block List'.");
            return;
        }

        heroSlidesPt.DataTypeId = _dtHeroSlidesBlockList.Id;
        heroSlidesPt.DataTypeKey = _dtHeroSlidesBlockList.Key;
        await _contentTypeService.UpdateAsync(homePage, Constants.Security.SuperUserKey);
        _logger.LogInformation("STBWeb: Updated homePage.heroSlides to use 'Hero Slides Block List' data type.");
    }

    private record PropDef(string Alias, string Name, IDataType DataType);
}
