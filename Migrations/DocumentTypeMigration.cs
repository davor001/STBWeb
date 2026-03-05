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

            await EnsureElementTypeAsync("featureIconLink", "Feature Icon Link", new[]
            {
                P("iconMdiClass", "Icon MDI Class", _dtTextstring!),
                P("label",        "Label",          _dtTextstring!),
                P("iconColor",    "Icon Color",     _dtTextstring!),
                P("linkUrl",      "Link URL",       _dtMultiUrlPicker!),
            });

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
            // ----------------------------------------------------------------
            await EnsureDocumentTypeAsync("homePage", "Home Page",
                compositions: ["seoComposition"],
                allowedAtRoot: true,
                allowedChildren: ["sectionRoot", "newsListingPage", "subBrandHomePage", "genericContentPage"],
                properties:
                [
                    P("heroSlides",       "Hero Slides",       _dtBlockList!),
                    P("featureIconLinks", "Feature Icon Links", _dtBlockList!),
                    P("applyOnlineImage", "Apply Online Image", _dtMediaPicker!),
                    P("applyOnlineUrl",   "Apply Online URL",   _dtMultiUrlPicker!),
                    P("promoBanner1",     "Promo Banner 1",     _dtContentPicker!),
                    P("promoBanner2",     "Promo Banner 2",     _dtContentPicker!),
                ]);

            await EnsureDocumentTypeAsync("sectionRoot", "Section Root",
                compositions: ["seoComposition", "heroBannerComposition"],
                allowedAtRoot: false,
                allowedChildren: ["sectionRoot", "productDetailPage", "productListingPage", "genericContentPage", "exchangeRatePage", "locationMapPage", "calculatorsPage", "corporateGovernancePage", "newsListingPage"],
                properties:
                [
                    P("bodyContent", "Body Content", _dtRichText!),
                ]);

            await EnsureDocumentTypeAsync("productDetailPage", "Product Detail Page",
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

            await EnsureDocumentTypeAsync("productListingPage", "Product Listing Page",
                compositions: ["seoComposition", "heroBannerComposition", "sidebarComposition"],
                allowedAtRoot: false,
                allowedChildren: ["productDetailPage", "sectionRoot"],
                properties:
                [
                    P("bodyContent", "Body Content", _dtRichText!),
                ]);

            await EnsureDocumentTypeAsync("newsListingPage", "News Listing Page",
                compositions: ["seoComposition", "heroBannerComposition"],
                allowedAtRoot: false,
                allowedChildren: ["newsDetailPage"],
                properties:
                [
                    P("newsPerPage", "News Per Page", _dtNumeric!),
                ]);

            await EnsureDocumentTypeAsync("newsDetailPage", "News Detail Page",
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

            await EnsureDocumentTypeAsync("genericContentPage", "Generic Content Page",
                compositions: ["seoComposition", "heroBannerComposition", "sidebarComposition"],
                allowedAtRoot: false,
                allowedChildren: ["genericContentPage", "sectionRoot"],
                properties:
                [
                    P("bodyContent",       "Body Content",       _dtRichText!),
                    P("documentDownloads", "Document Downloads", _dtBlockList!),
                ]);

            await EnsureDocumentTypeAsync("exchangeRatePage", "Exchange Rate Page",
                compositions: ["seoComposition", "heroBannerComposition", "sidebarComposition"],
                allowedAtRoot: false,
                allowedChildren: [],
                properties:
                [
                    P("rateApiEndpoint", "Rate API Endpoint", _dtTextstring!),
                ]);

            await EnsureDocumentTypeAsync("locationMapPage", "Location Map Page",
                compositions: ["seoComposition", "heroBannerComposition"],
                allowedAtRoot: false,
                allowedChildren: [],
                properties:
                [
                    P("mapType",              "Map Type",               _dtDropdown!),
                    P("locationsApiEndpoint", "Locations API Endpoint", _dtTextstring!),
                    P("mapDefaultCenter",     "Map Default Center",     _dtTextstring!),
                ]);

            await EnsureDocumentTypeAsync("calculatorsPage", "Calculators Page",
                compositions: ["seoComposition", "heroBannerComposition", "sidebarComposition"],
                allowedAtRoot: false,
                allowedChildren: [],
                properties:
                [
                    P("calculatorConfig", "Calculator Config", _dtTextarea!),
                ]);

            await EnsureDocumentTypeAsync("corporateGovernancePage", "Corporate Governance Page",
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

            await EnsureDocumentTypeAsync("subBrandHomePage", "Sub-Brand Home Page",
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
                System.IO.File.Move(expectedPath, tempPath, true);
                hidFile = true;
                _logger.LogInformation("STBWeb: Temporarily renamed '{Path}' before CreateAsync.", expectedPath);
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

    private record PropDef(string Alias, string Name, IDataType DataType);
}