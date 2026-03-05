# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

**STBWeb** is a migration of [www.stb.com.mk](https://www.stb.com.mk) (Stopanska Banka AD Skopje) to **Umbraco 15.4.4 / ASP.NET Core 9**. The reference crawl of the original site lives in `reference/stb-crawl/` (1,893 files) and is the source of truth for content structure. `reference/site-analysis.md` documents the full URL sitemap, document type schema, and external dependencies.

## Commands

```bash
# Run the development server (auto-installs Umbraco on first run)
dotnet run

# Build only
dotnet build

# Restore packages
dotnet restore

# Publish
dotnet publish -c Release
```

**URLs:**
- Site: `http://localhost:9023` / `https://localhost:44381`
- Backoffice: `https://localhost:44381/umbraco` (admin@stb.com.mk / Admin1234!)

There are no test projects in this repository.

## Architecture

### Code-First Document Types

All Umbraco content types are defined in C# and created via `Migrations/DocumentTypeMigration.cs`. The handler runs on every app start and is **fully idempotent** — it checks for existence before creating. It hooks into `UmbracoApplicationStartingNotification` via `DocumentTypeMigrationComposer : IComposer`.

Adding a new document type means adding a method in `DocumentTypeMigrationHandler` and calling it from `HandleAsync`. Never create document types through the backoffice UI, as they won't survive a fresh database.

### Document Types

**Element types** (BlockList blocks): `heroSlide`, `featureIconLink`, `faqItem`, `documentLink`, `personProfile`, `promoItem`, `newsTickerItem`

**Compositions** (mixed into page types): `seoComposition`, `heroBannerComposition`, `sidebarComposition`

**Page document types**: `HomePage`, `SectionRoot`, `ProductDetailPage`, `ProductListingPage`, `NewsListingPage`, `NewsDetailPage`, `GenericContentPage`, `ExchangeRatePage`, `LocationMapPage`, `CalculatorsPage`, `CorporateGovernancePage`, `SubBrandHomePage`

### Views

Each document type has a corresponding `.cshtml` in `/Views/`. All pages inherit `_Layout.cshtml`; TOPSI and GoldenClub sub-brand pages use `_TopsiLayout.cshtml` / `_GoldenClubLayout.cshtml`.

Shared partials are in `/Views/Partials/`. BlockList element renderers are in `/Views/Partials/blocklist/` — the file name must match the document type alias (e.g., `HeroSlide.cshtml` for the `heroSlide` element type).

Standard view pattern:
```csharp
@inherits Umbraco.Cms.Web.Common.Views.UmbracoViewPage
@{
    var value = Model.Value<Type>("propertyAlias");
}
```

### Localization

The site supports two cultures: `mk` (Macedonian, root `/`) and `en` (English, prefix `/en/`). Umbraco handles culture-based routing automatically. All page document types should have culture variants enabled.

### Static Assets

All static files are in `wwwroot/`. The main stylesheet is `wwwroot/css/app.css` (241 KB). Sub-brand themes: `topsi.css` and `GoldenClub.css`. Vendor JS is in `wwwroot/assets/js/` and `wwwroot/js/`; custom scripts are in `wwwroot/scripts/`. No build pipeline — assets are served as-is.

## Implementation Phases

| Phase | Status | Description |
|-------|--------|-------------|
| 0 | ✅ | Fresh Umbraco 15.4.4 / .NET 9 project |
| 1A | ✅ | Static assets copied to wwwroot |
| 1B | ✅ | Document types via code-first migration |
| 1C | ✅ | Master layout and shared partial views |
| 1D | ✅ | Page template views and BlockList partials |
| 1E | ⏳ | Content tree seeding (150+ pages, 2 cultures) |
| 1F | ⏳ | Media migration (470 PDFs, 627 images) |

When resuming after a gap, read `reference/PROGRESS.md` to find the exact last completed action.

## Key Conventions

- **Property aliases** use `camelCase` in C# definitions and Razor (`Model.Value<T>("myAlias")`).
- **Images** are stored in Umbraco media and resolved via `IPublishedContent.Url()` to `/media/[id]/filename`.
- **Links** use `MultiUrlPicker` — support internal content links, external URLs, and mailto.
- **BlockList** blocks are rendered by matching the element type alias to a file in `/Views/Partials/blocklist/`; `default.cshtml` is the fallback.
- **RazorCompileOnBuild is disabled** (`false` in `.csproj`) — Razor views use runtime compilation via Umbraco's InMemoryAuto models mode.
- The database is SQLite during development (`umbraco/Data/Umbraco.sqlite.db`), auto-created on first run.
