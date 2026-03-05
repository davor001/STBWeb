# STBWeb Implementation Progress

## Current Status
- **Current Phase:** Phase 3 — CI/CD & Complex Functional Components
- **Status:** COMPLETED
- **Last Updated:** 2026-02-26
- **Status**: Completed Phase 3 (Azure CI/CD, Exchange Rate component, Dynamic Calculators, Branch Locator, Legacy forms, Full-site search). All components mapped and code pushed.
- **Next Steps**: Resolve critical Azure runtime database seeding error, then proceed to Phase 4 (Content Auditing & QA).
- **Blockers**: Azure startup crash `ArgumentOutOfRangeException` due to `ContentSeeder.cs` failing to save the `Home` node. See `reference/HANDOFF.md` for full technical details.

## Phase Checklist
- [x] SUB-PHASE 0: Project Initialization (.NET 9 + Umbraco 15) — commit 569c1bc
- [x] SUB-PHASE 1A: Static Assets — commit 50c1a4b
- [x] SUB-PHASE 1B: Document Types (Code-First) — commit 2b36ab2
- [x] SUB-PHASE 1C: Master Layout & Shared partial Views — commit b365d65
- [x] SUB-PHASE 1D: Page Template Views & BlockList Partials — commit e757e03
- [x] SUB-PHASE 1E: Content Tree Seeding (~110 nodes, mk+en) — commit ad4d9aa
- [x] SUB-PHASE 1F: Media Migration (~850 files copied, IMedia nodes) — commit 9842843
- [x] **A. Restore CI/CD & Base Functionality** (Resolved Media Picker path issues, disabled remote staging locking)
- [x] **B. Generic Content Shell** (Render Exchange Rate page dynamically via mock API responses)
- [x] **C. Calculators Shell** (Render static JSON configs for Loan/Deposit calculators natively)
- [x] **D. Branch & ATM Locator** (Interactive Google Maps integration pinning dynamically rendered marker endpoints)
- [x] **E. Application Forms** (Legacy `.html` pages moved to `ApplicationFormPage.cshtml` integrated within the standard Umbraco form lifecycle using `AppFormSurfaceController`)
- [x] **F. Full-site Search** (Examine `ExternalIndex` indexed implementation pointing `_SearchPanel.cshtml` to `SiteSearchController`)

## Key Paths
- Crawl root: `reference/stb-crawl/www.stb.com.mk/`
- Site analysis: `reference/site-analysis.md`
- .NET SDK: 9.0.311
- Launch URL: https://localhost:44381 (backoffice: /umbraco)
- Backoffice credentials: admin@stb.com.mk / Admin1234!

## Detailed Log
- [2026-02-25] PROGRESS.md created
- [2026-02-25] .NET SDK 9.0.311 confirmed; crawl verified at reference/stb-crawl/www.stb.com.mk/
- [2026-02-25] Phase 0: Fresh Umbraco 15.4.4 / .NET 9 project created (STBWeb.csproj, Program.cs, appsettings*.json)
- [2026-02-25] Phase 1A: 60 static files copied to wwwroot/ (CSS, JS, images, fonts, vendor libs)
- [2026-02-25] Phase 1B: All document types created in Migrations/DocumentTypeMigration.cs — 12 page types, 7 element types, 3 compositions (456 lines, idempotent)
- [2026-02-25] Phase 1C: _Layout.cshtml, _TopsiLayout.cshtml, _GoldenClubLayout.cshtml + 10 shared partials in Views/Partials/
- [2026-02-25] Phase 1D: 15 page template views + 8 BlockList partials in Views/Partials/blocklist/
- [2026-02-25] Phase 1E: ContentSeeder.cs — seeds mk/en languages + ~110 content nodes across full hierarchy; registered in DocumentTypeMigrationComposer after doc type migration
- [2026-02-25] Phase 1F: MediaSeeder.cs — copies ~850 media files (images + PDFs) from crawl to wwwroot/media preserving paths; creates Umbraco media library folder structure + IMedia nodes

## Errors & Fixes
- [2026-02-25] Fix: File ambiguity in MediaSeeder.cs (System.IO.File vs Umbraco.Cms.Core.Models.File) — resolved with `using IOFile = System.IO.File`
- [2026-02-26] Fix: Umbraco 15 breaking changes in `DocumentTypeMigration.cs`. Replaced hardcoded Guids with `Constants.PropertyEditors.Aliases`, injected `IConfigurationEditorJsonSerializer` and `PropertyEditorCollection`, and replaced `SaveAsync` with `CreateAsync`.

## Resume Instructions
If you are a new Claude session reading this file:
1. Read this PROGRESS.md fully
2. Read reference/site-analysis.md for site context and full URL/content map
3. The project is already fully set up — do NOT re-run dotnet new or recreate any existing files
4. Check which phase is marked as current (currently: Phase 2)
5. Continue from "Last Completed Action" above
6. Do NOT redo completed phases

## Phase 2 Notes — Integration & Content Rendering

### Phase 2A: Navigation Rendering
- `_MainNavigation.cshtml` needs to build nav from content tree (IPublishedContent.Children)
- Top-level nav items: Individuals, Legal Entities, Financial Markets, About Bank, Locations
- Each top-level item should show its children as dropdown
- Current section detection: `Model.AncestorsOrSelf().Any(a => a.Id == navItem.Id)`

### Phase 2B: Culture/Language Routing
- `appsettings.json` needs domain/culture mapping: mk → root `/`, en → `/en/`
- Add domain routing in Umbraco: Home node bound to both domains
- Language switcher in `_TopBar.cshtml` should link to culture variant of current page

### Phase 2C: Homepage
- `HomePage.cshtml` needs to render heroSlides BlockList → carousel
- News ticker → query newsListingPage children, render latest N
- Feature icon links → render grid from featureIconLinks BlockList

### Phase 2D: Product Detail Page
- `ProductDetailPage.cshtml` needs tabbed content from tabProductInfo, tabPricing, etc.
- Sidebar: sidebarRelatedLinks + contact card

### Phase 2E: News
- `NewsListingPage.cshtml` → query children (newsDetailPage), render card grid
- `NewsDetailPage.cshtml` → render articleBody + publishDate

### Phase 2F: Shells
- `ExchangeRatePage.cshtml` → date picker + #divExchangeRate placeholder
- `CalculatorsPage.cshtml` → calculator select + placeholder div

## SUB-PHASE 1F Notes (completed)
- Media source: `reference/stb-crawl/www.stb.com.mk/media/[id]/[filename]`
- Destination: `wwwroot/media/[id]/[filename]` (preserves original URLs as static files)
- IMedia nodes created under: STB Media Import > Images / Documents / Content PDFs
- Cyrillic filenames handled via Uri.EscapeDataString in umbracoFile JSON src
