# STB Web — Site Analysis
## www.stb.com.mk — Crawl-Based Reference Document

> **Purpose:** Reference document for the STBWeb redesign / Umbraco 15 migration project.
> Based on static crawl of www.stb.com.mk captured in `reference/stb-crawl/` (722 pages, 2 208 URLs).
> Crawl date: 2026-02-24.

---

## 1. Site Overview

| Attribute | Value |
|---|---|
| Institution | Stopanska Banka AD Skopje |
| SWIFT | STOBMK2X |
| Founded | 1944 |
| Parent | National Bank of Greece (NBG) |
| Primary domain | www.stb.com.mk |
| E-banking | ebank.stb.com.mk |
| E-registration | epin.stb.com.mk |
| Languages | Macedonian (MK — root `/`) + English (EN — `/en/` prefix) |
| Crawled pages | 601 HTML files |
| CSS files | 79 |
| JS files | 91 |
| Images | 627 |
| Documents (PDF/Office) | 470 |

---

## 2. Page Templates / Layouts

Eight distinct page templates are used across the main site, plus three sub-brand / micro-site templates.

### 2.1 `HomePage`
**URL:** `/` and `/en/`

**Unique structural elements:**
- **Full-width Hero Carousel** (`#homeMainSlider`) — Bootstrap carousel, 5 slides. Each slide: desktop image 1600×544, mobile image 500×250, left-aligned caption overlay (`.carousel-caption-left2`) with H3, HR, H4, and CTA button.
- **News Ticker** — horizontal conveyor (jConveyorTicker) of latest news headlines, links to `/novosti/`.
- **"What Do I Need?" Icon Grid** (`.home-what-i-need`) — 9 icon links in colored circles (MDI icons): Locations, Cards, Loans, Insurance, Broker, Calculators, Property Sale, Info Forms, Career.
- **3-column Widget Row** (`.home-block-main`):
  - Apply Online card (image + CTA)
  - Currency Rate mini-table (flag icons + EUR/USD/GBP/CHF buy/sell)
  - Latest News card
- **Promotional Banner Cards** — product/campaign promos (GoldenClub, TOPSI)
- **Mobile App Alert Boxes** (`#box_apple_store`, `#box_google_play`) — dismissible alert banners for iOS/Android app links
- **FAQ Accordion** (Bootstrap `.custom-accordion`)

**Layout:** No sidebar — full-width content only.

---

### 2.2 `ProductDetailPage`
**URLs:** `/naselenie/karticki/kreditni-karticki/[product]/`, `/naselenie/krediti/[type]/[product]/`, `/naselenie/depoziti/[type]/[product]/`, etc.

**Structural elements:**
- **Breadcrumb** (Bootstrap `.breadcrumb`, 4–5 levels deep)
- **Hero Banner** — single image (1600×250), with `.carousel-caption-left2` overlay: H3 section name, HR, H4 product name.
  Mobile: same image or separate 500×250 variant.
- **Tabbed Content** (`.nav-tabs.nav-bordered`) — 2–5 tabs depending on product type:
  - `Za_proizvodot` — Product description (bullet list of benefits, rich text)
  - `Cenovni_uslovi` — Pricing conditions (tables)
  - `Kriteriumi_i_dokumenti` — Eligibility criteria + PDF document downloads
  - `Bezbednost_pri_plakane` — Security info (cards pages only)
  - `Kamatni_stapki` — Interest rates (deposit pages only)
- **Right Sidebar** (col-md-4/xl-3):
  - "Apply Online" CTA button
  - "May Interest You" card (related links list)
  - Contact card (phone icon + phone/email buttons)

**Layout:** 2-column — `col-md-8/col-xl-9` content + `col-md-4/xl-3` sidebar.

---

### 2.3 `ProductListingPage`
**URLs:** `/naselenie/karticki/`, `/naselenie/krediti/`, `/naselenie/osiguruvanje/`, etc.

**Structural elements:**
- Breadcrumb
- Hero Banner (1600×250) with caption
- Content: rich text overview + clickable product banner cards (thumbnail image 500×250 + title + short description → link to product detail)
- Right Sidebar: "May Interest You" links + contact card

---

### 2.4 `NewsListingPage`
**URL:** `/novosti/`, `/en/news/`

**Structural elements:**
- Breadcrumb
- Hero Banner (1600×250, generic news banner)
- **News Card Grid** (`.news-cards`, Bootstrap `col-sm-6 col-lg-4`): each card has:
  - Thumbnail image (`.card-img-top`)
  - Title (h5, linked to detail page)
  - Excerpt (truncated body text)
  - Date (`<small class="text-muted">`)
- Right Sidebar: Promotional Offer card (`.bg-info text-white` — list of current promos) + Contact card

---

### 2.5 `NewsDetailPage`
**URLs:** `/novosti/[slug]/`, `/en/news/[slug]/`

**Structural elements:**
- Breadcrumb
- Hero Banner (1600×250, shared "Новости / Соопштенија" banner — not article-specific)
- **Article body** (col-md-8/col-lg-9): H3 title, date paragraph, rich text (paragraphs, sub-headings, lists, embedded images, PDF links)
- Right Sidebar: Apply Online CTA + Promotional Offer card + Contact card

---

### 2.6 `ExchangeRatePage`
**URL:** `/finansiski-pazari/kursna-lista/`

**Structural elements:**
- Breadcrumb
- Hero Banner (1600×250)
- Archive date picker (`.datepicker`, input + calendar icon) + Search button
- Print button (`window.print()`)
- Dynamic content container (`#divExchangeRate`) — loaded via `loadExchangeRate()` AJAX call
- Print-only: logo + "Курсна листа" heading (hidden on screen)
- Right Sidebar: related links + contact card

---

### 2.7 `LocationMapPage`
**URLs:** `/sb-lokacii/mreza-na-filijali/`, `/sb-lokacii/mreza-na-bankomati/`

**Structural elements:**
- Breadcrumb
- Google Maps embedded map (`gmaps.min.js`) with branch/ATM markers
- Detail panel (slides in on marker click — template literal `{{details.image}}` placeholder visible in crawl, indicating Angular/template binding)
- Filter UI (city selector)
- List view of locations

> **Note:** These pages appear to use an AngularJS or similar template-binding pattern for the detail pane — confirmed by the URL pattern `{{details.image}}` appearing as a literal route in the crawl.

---

### 2.8 `CalculatorsPage`
**URL:** `/ostanati-sodrzini/kalkulatori/`

**Structural elements:**
- Breadcrumb
- Hero Banner (1600×250)
- Product selector (`<select id="SelectCalculator">`) — 10 product types
- Dynamic calculator form (`#divExchangeRate` reused div) — rendered by `Calculators.js?v=22`:
  - Range sliders (rangeslider.min.js)
  - Numeric inputs (amount, term, interest rate)
  - Real-time payment calculation output
- Right Sidebar: contact card

**Calculator types (select options):**
1. Unsecured consumer loan up to 1,500,000 MKD
2. Unsecured consumer loan up to 80,000 EUR
3. Consumer loan via retail network
4. Home loan
5. Auto loan
6. Term deposit – Premium
7. Term deposit – EuroBonus
8. Micro business loan (legal entities)
9. Term deposit 12/13
10. Term deposit 18/20

---

### 2.9 `CorporateGovernancePage`
**URL:** `/za-bankata/korporativno-upravuvanje/`

**Structural elements:**
- Breadcrumb
- Hero Banner (1600×250)
- Tabbed content:
  - `Menacment` — Management Board: person profiles (role subtitle, name h5, photo, bio bullet list)
  - `Nadzoren_odbor` — Supervisory Board: same person profile pattern
  - `Organizaciona_struktura` — Org chart (image)
- Right Sidebar: document downloads (PDF: statutes, ethics codes, governance reports, etc.)

---

### 2.10 `GenericContentPage`
**URLs:** Privacy policy, T&Cs, tariffs info, user corner, correspondent banks, etc.

**Structural elements:**
- Breadcrumb
- Hero Banner (1600×250) — often a generic/reused banner image
- Main content area: rich text only (H3/H4, paragraphs, lists, tables, PDF links)
- Optional right sidebar

---

### Sub-brand Templates

#### 2.11 `TopsiPage`
**Base URL:** `/topsi/`
- Distinct color scheme (blue-heavy youth brand)
- Separate logo (`logo_transparent.png`)
- Separate CSS: `/css/topsi.css?v=0.1.5`
- Separate favicons (favicon-16x16, 32x32, apple-touch-icon from `/media/215x/`)
- Social media links (Facebook, Twitter, Instagram, YouTube) with brand-specific icons
- STB footer logo variant (`stb_logo_bottom.jpg`)

#### 2.12 `GoldenClubPage`
**Base URL:** `/goldenclub/`
- Distinct dark/gold color scheme (senior/loyalty brand)
- Separate logo (`logogoldenclub-e1475663697742.png`)
- Separate CSS: `/css/GoldenClub.css?v=0.1.5`
- Large hero slider (1200×550)

#### 2.13 `ApplicationFormPage`
**Base URL:** `/appForm/[product-slug]`
- Completely different CSS stack: `SiteBoot.css`, `bootstrap.min.css`, `rangeslider.css`, `jquery-ui.css`
- Separate footer with social media icons (Facebook, LinkedIn, Instagram, Twitter, YouTube)
- Multi-step application form with range sliders for amount/term selection
- Logo variant (`stb_logo_bottom.jpg`)

#### 2.14 `OpenBankingDocsPage` *(separate Next.js micro-site)*
**Base URL:** `/docs/`
- Built with **Next.js 13+** (App Router — confirmed by `_next/static/chunks/app/[locale]/` paths)
- Separate tech stack from the Umbraco/ASP.NET main site
- API documentation style layout
- Section-aware locale routing (`[locale]` segment)

---

## 3. Reusable Components

### Layout Components
| Component | Class / ID | Description |
|---|---|---|
| Top Bar | `.navbar-custom.topnav-navbar` | Logo, language switch (mk/en), segment (Населenie/Pravni Lica), phone, search toggle, e-reg + e-login buttons |
| Main Nav | `#navbar .topnav` | Bootstrap collapse navbar, 2–3 level dropdowns, context-aware per segment |
| Footer | `footer.footer` | 4-column link list + social icons + logos + NBG group mention |
| Cookie/GDPR | `#GdprModal`, `#settingsModal` | Cookie consent with granular opt-in options |
| Search Panel | `.right-bar-toggle` | Slide-in search bar (right side) |
| Breadcrumb | `.breadcrumb` | Bootstrap breadcrumb, 2–5 levels |

### Content Components
| Component | Class / Selector | Description |
|---|---|---|
| Hero Banner | `.row > .col > img.w-100` + `.carousel-caption-left2` | Static banner image (1600×250) with overlaid left-aligned caption |
| Hero Carousel | `#homeMainSlider.carousel` | Full-width rotating hero (homepage only), 5 slides, desktop + mobile images |
| News Ticker | `.js-conveyor-example` | Horizontal auto-scrolling news headline strip (jConveyorTicker) |
| Product Tabs | `.nav-tabs.nav-bordered` + `.tab-content` | Bootstrap tab panel — product info, pricing, docs, security |
| FAQ Accordion | `.custom-accordion` / `.collapse` | Bootstrap accordion for Q&A content |
| News Card | `.card.d-block` in `.news-cards` | Image + h5 title + excerpt + date |
| Product Banner | `.card` with 500×250 image | Clickable product card used in listing grids |
| Person Profile | H5 name + img + ul biography | Used on corporate governance tab |
| Currency Widget | `.home-exchange-rate` | Mini rates table with currency flag icons |
| Icon Grid | `.home-what-i-need` / `.win-icon-row` | "What do I need?" — 9 colored circle + MDI icon links |
| Right Sidebar: Contact | `.card-body.text-center` | Phone icon + phone/email CTA buttons |
| Right Sidebar: Related | `ul.list-unstyled` | "May interest you" link list |
| Right Sidebar: Promo | `.card-body.bg-info.text-white` | Promotional offers list |
| Apply Online CTA | `a.btn.btn-primary.w-100` | Full-width primary button → `/appForm/` or `/` |
| Document Download Link | `<a href="/media/[id]/filename.pdf">` | Inline PDF download, no wrapper component |
| App Store Boxes | `#box_apple_store`, `#box_google_play` | Dismissible alert with store badge images |

---

## 4. Proposed Umbraco Document Type Hierarchy

### Master Layout Properties (shared by all document types)
| Property | Alias | Type |
|---|---|---|
| SEO Title | `seoTitle` | TextString |
| SEO Description | `seoDescription` | TextArea |
| OG Image | `ogImage` | MediaPicker |
| Hero Banner Image | `heroBannerImage` | MediaPicker |
| Hero Banner Heading | `heroBannerHeading` | TextString |
| Hero Banner Subheading | `heroBannerSubheading` | TextString |
| Hero CTA Text | `heroCtaText` | TextString |
| Hero CTA Link | `heroCtaLink` | Link |
| Show Right Sidebar | `showSidebar` | Toggle |
| Sidebar Related Links | `sidebarRelatedLinks` | MultiUrlPicker |
| Sidebar Promo Items | `sidebarPromoItems` | NestedContent / BlockList |

---

### Document Types

#### `HomePage`
Inherits master layout. Additional properties:
| Property | Alias | Type |
|---|---|---|
| Hero Slides | `heroSlides` | BlockList (HeroSlide block: desktopImage, mobileImage, heading, subheading, ctaText, ctaUrl) |
| News Ticker Heading | `newsTickerSource` | auto-populated from News nodes |
| Feature Icon Links | `featureIconLinks` | BlockList (FeatureIcon: icon MDI name, label, url, color) |
| Apply Online Card Image | `applyOnlineImage` | MediaPicker |
| Apply Online CTA URL | `applyOnlineUrl` | Link |
| Promo Banner 1 | `promoBanner1` | Content Picker |
| Promo Banner 2 | `promoBanner2` | Content Picker |

---

#### `SectionRoot` *(virtual nav node — Individuals / Legal Entities)*
| Property | Alias | Type |
|---|---|---|
| Segment Label MK | `segmentLabelMk` | TextString |
| Segment Label EN | `segmentLabelEn` | TextString |
| Navigation Menu | `navMenu` | auto-built from child nodes |
| Hero Banner | inherited | — |

---

#### `ProductDetailPage`
| Property | Alias | Type |
|---|---|---|
| Product Name | `productName` | TextString |
| Product Banner Image | `productBannerImage` | MediaPicker |
| Section Context (breadcrumb) | auto | from content tree |
| Tab: Product Info | `tabProductInfo` | Rich Text Editor |
| Tab: Pricing | `tabPricing` | Rich Text Editor |
| Tab: Criteria & Documents | `tabCriteriaAndDocs` | Rich Text Editor (with media pickers for PDFs) |
| Tab: Security | `tabSecurity` | Rich Text Editor |
| Tab: Interest Rates | `tabInterestRates` | Rich Text Editor |
| Tab Labels (override) | `tabLabels` | BlockList (optional) |
| Apply Online URL | `applyOnlineUrl` | Link |
| FAQ Items | `faqItems` | BlockList (question: TextString, answer: RTE) |

---

#### `NewsListingPage`
| Property | Alias | Type |
|---|---|---|
| Banner Image | `bannerImage` | MediaPicker |
| Banner Heading | `bannerHeading` | TextString |
| Banner Subheading | `bannerSubheading` | TextString |
| News per page | `newsPerPage` | Numeric (default 9) |
| *(News items are child `NewsDetailPage` nodes)* | — | — |

---

#### `NewsDetailPage`
| Property | Alias | Type |
|---|---|---|
| Title | `title` | TextString |
| Publish Date | `publishDate` | DateTime |
| Summary / Excerpt | `summary` | TextArea |
| Thumbnail Image | `thumbnailImage` | MediaPicker |
| Article Body | `articleBody` | Rich Text Editor |
| Sidebar Promo Items | `sidebarPromoItems` | BlockList |

---

#### `ExchangeRatePage`
| Property | Alias | Type |
|---|---|---|
| Banner Image | `bannerImage` | MediaPicker |
| Archive API Endpoint | `rateApiEndpoint` | TextString (config) |
| Currency List | `currencyList` | auto-populated from API |

> Exchange rates load dynamically — the Umbraco page is a shell; rates come from an external/internal API call via JavaScript.

---

#### `LocationMapPage`
| Property | Alias | Type |
|---|---|---|
| Map Type | `mapType` | Dropdown (Branches / ATMs / Regional Centres) |
| API Endpoint for Locations | `locationsApiEndpoint` | TextString |
| Banner Image | `bannerImage` | MediaPicker |
| Map Default Centre (Lat/Lng) | `mapDefaultCenter` | TextString |

---

#### `CalculatorsPage`
| Property | Alias | Type |
|---|---|---|
| Banner Image | `bannerImage` | MediaPicker |
| Available Calculators | `calculators` | BlockList (label, productKey, parameterDefaults) |
| Calculator JS Config | `calculatorConfig` | JSON Editor (or Textarea) |

---

#### `CorporateGovernancePage`
| Property | Alias | Type |
|---|---|---|
| Banner Image | `bannerImage` | MediaPicker |
| Management Board Members | `managementBoard` | BlockList (PersonProfile block) |
| Supervisory Board Members | `supervisoryBoard` | BlockList (PersonProfile block) |
| Org Chart Image | `orgChartImage` | MediaPicker |
| Governance Documents | `governanceDocs` | BlockList (docLabel, docFile MediaPicker) |

**PersonProfile block:**
| Property | Alias | Type |
|---|---|---|
| Photo | `photo` | MediaPicker |
| Role / Title | `role` | TextString |
| Full Name | `fullName` | TextString |
| Biography Items | `bioItems` | BlockList (text: TextString) |

---

#### `GenericContentPage`
| Property | Alias | Type |
|---|---|---|
| Banner Image | `bannerImage` | MediaPicker |
| Page Heading | `pageHeading` | TextString |
| Body Content | `bodyContent` | Rich Text Editor |
| Document Downloads | `documentDownloads` | BlockList (label, file MediaPicker) |
| Show Sidebar | `showSidebar` | Toggle |

---

#### `SubBrandHomePage` *(TOPSI / GoldenClub)*
| Property | Alias | Type |
|---|---|---|
| Sub-brand Key | `subbrandKey` | Dropdown (topsi / goldenclub) |
| Custom CSS Class | `themeCssClass` | TextString |
| Logo | `logo` | MediaPicker |
| Hero Slides | `heroSlides` | BlockList |
| Content Sections | `contentSections` | BlockList |

---

### Full Hierarchy

```
Root
└── Home (HomePage)
    ├── Individuals / Население (SectionRoot)
    │   ├── Deposits (SectionRoot)
    │   │   ├── About Deposits (GenericContentPage)   /depoziti/za-depozitite/
    │   │   ├── Demand Deposits (SectionRoot)
    │   │   │   ├── Savings Accounts (ProductDetailPage)
    │   │   │   ├── Package Plus (ProductDetailPage)
    │   │   │   └── Flexi Deposit (ProductDetailPage)
    │   │   └── Term Deposits (SectionRoot)
    │   │       ├── 18/20 Deposit (ProductDetailPage)
    │   │       ├── 12/13 Deposit (ProductDetailPage)
    │   │       ├── 6/9 Month Deposit (ProductDetailPage)
    │   │       ├── Bee Children's Savings (ProductDetailPage)
    │   │       ├── Regular Deposit (ProductDetailPage)
    │   │       ├── Premium Deposit (ProductDetailPage)
    │   │       ├── My Plan (ProductDetailPage)
    │   │       └── EuroBonus (ProductDetailPage)
    │   ├── Loans / Кредити (SectionRoot)
    │   │   ├── Loans Overview (ProductListingPage)
    │   │   ├── Consumer Loans (SectionRoot)
    │   │   │   ├── Up to 1,500,000 MKD (ProductDetailPage)
    │   │   │   ├── Secured Consumer Loan (ProductDetailPage)
    │   │   │   ├── My Cash (ProductDetailPage)
    │   │   │   ├── Purpose Loan via Retailers (ProductDetailPage)
    │   │   │   └── Secured for Homeowners (ProductDetailPage)
    │   │   ├── Home Loans (SectionRoot)
    │   │   │   ├── Main Characteristics (ProductDetailPage)
    │   │   │   └── Under-construction Properties (ProductDetailPage)
    │   │   └── Auto Loan (SectionRoot)
    │   │       └── With Vehicle Pledge (ProductDetailPage)
    │   ├── Cards / Картички (SectionRoot)
    │   │   ├── Cards Overview (ProductListingPage)
    │   │   ├── Credit Cards (SectionRoot)
    │   │   │   ├── MasterCard Standard (ProductDetailPage)
    │   │   │   ├── VISA Star (ProductDetailPage)
    │   │   │   ├── VISA Vero (ProductDetailPage)
    │   │   │   ├── VISA Zero (ProductDetailPage)
    │   │   │   └── VISA Gold (ProductDetailPage)
    │   │   ├── Debit Cards (SectionRoot)
    │   │   │   ├── Mastercard Debit (ProductDetailPage)
    │   │   │   ├── Mastercard TOPSI (ProductDetailPage)
    │   │   │   ├── Mastercard Platinum (ProductDetailPage)
    │   │   │   ├── VISA Classic (ProductDetailPage)
    │   │   │   ├── VISA Gold Debit (ProductDetailPage)
    │   │   │   └── VISA Internet (ProductDetailPage)
    │   │   ├── Promotions (SectionRoot)
    │   │   │   ├── Installments Without Interest (ProductDetailPage)
    │   │   │   ├── Promo Cash Weekend (ProductDetailPage)
    │   │   │   ├── Mastercard Day (ProductDetailPage)
    │   │   │   └── Cash Installments Without Interest (ProductDetailPage)
    │   │   ├── ATM Cash Deposit (ProductDetailPage)
    │   │   ├── Google Pay (ProductDetailPage)
    │   │   ├── Apple Pay (ProductDetailPage)
    │   │   ├── Visa Direct (ProductDetailPage)
    │   │   └── Secure Payment (ProductDetailPage)
    │   ├── Accounts / Сметки (SectionRoot)
    │   │   ├── Payment Accounts (ProductDetailPage)
    │   │   ├── Basic Payment Accounts (ProductDetailPage)
    │   │   ├── Overdraft (ProductDetailPage)
    │   │   ├── Salary Plus (ProductDetailPage)
    │   │   ├── Payment Services Act Info (GenericContentPage)
    │   │   └── Account Search Tool (GenericContentPage)
    │   ├── Digital Banking (SectionRoot)
    │   │   ├── OneID Client Update (ProductDetailPage)
    │   │   ├── E-Registration / Reactivation (GenericContentPage)
    │   │   ├── E-Banking (ProductDetailPage)
    │   │   ├── M-Banking (ProductDetailPage)
    │   │   ├── TOPSI Pay (ProductDetailPage)
    │   │   ├── ATM Payments (ProductDetailPage)
    │   │   └── Video Tutorials (GenericContentPage)
    │   ├── Insurance (SectionRoot)
    │   │   ├── Insurance Overview (GenericContentPage)
    │   │   ├── Credit Life Insurance (ProductDetailPage)
    │   │   ├── Risk Life Insurance (ProductDetailPage)
    │   │   ├── Pension Insurance (ProductDetailPage)
    │   │   ├── Travel Insurance (ProductDetailPage)
    │   │   ├── Property Insurance (ProductDetailPage)
    │   │   ├── Auto Insurance (ProductDetailPage)
    │   │   ├── Health Insurance (ProductDetailPage)
    │   │   ├── Accident Insurance (ProductDetailPage)
    │   │   ├── Loan Repayment Insurance (ProductDetailPage)
    │   │   └── Unit-Link Life Insurance (ProductDetailPage)
    │   └── Payment Services (SectionRoot)
    │       ├── Domestic Payments (ProductDetailPage)
    │       ├── International Payments (ProductDetailPage)
    │       └── Western Union (ProductDetailPage)
    ├── Legal Entities / Правни Лица (SectionRoot)
    │   ├── Digital Banking (SectionRoot)
    │   │   └── E-Commerce (ProductDetailPage)
    │   ├── Credit Products (GenericContentPage)
    │   ├── Deposits (GenericContentPage)
    │   └── Documentary Banking (GenericContentPage)
    ├── Financial Markets / Финансиски Пазари (SectionRoot)
    │   ├── Exchange Rate (ExchangeRatePage)
    │   ├── Forex Market (GenericContentPage)
    │   ├── Broker Services (GenericContentPage)
    │   ├── Government Securities (GenericContentPage)
    │   └── Currency Exchange (GenericContentPage)
    ├── About Bank / За Банката (SectionRoot)
    │   ├── Bank Profile (GenericContentPage)
    │   ├── Corporate Governance (CorporateGovernancePage)
    │   ├── Shareholders (GenericContentPage)
    │   ├── Reports & Data (GenericContentPage)
    │   ├── Interest Rates (GenericContentPage)
    │   ├── Correspondent Banks (GenericContentPage)
    │   ├── Contact (GenericContentPage)
    │   └── Privacy Policy & Cookies (GenericContentPage)
    ├── Marketing & CSR (SectionRoot)
    │   ├── CSR Practices (GenericContentPage)
    │   ├── Realized Projects (GenericContentPage)
    │   ├── Logos & Trademarks (GenericContentPage)
    │   ├── Ad Campaigns (GenericContentPage)
    │   ├── Interviews & Publications (GenericContentPage)
    │   └── Campaign Pages (GenericContentPage, multiple)
    ├── Locations (SectionRoot)
    │   ├── Branch Network (LocationMapPage)
    │   ├── ATM Network (LocationMapPage)
    │   └── Regional Business Centres (LocationMapPage)
    ├── News (NewsListingPage)
    │   └── [news articles] (NewsDetailPage, N items)
    ├── Career (GenericContentPage)
    ├── Other Content (SectionRoot)
    │   ├── Calculators (CalculatorsPage)
    │   ├── Property Sale (GenericContentPage + FancyBox gallery)
    │   ├── Tariffs (GenericContentPage)
    │   ├── User Corner (GenericContentPage)
    │   ├── Terms of Use (GenericContentPage)
    │   └── Open Banking Portal (GenericContentPage — embeds Next.js)
    ├── TOPSI (SubBrandHomePage + child pages)
    └── GoldenClub (SubBrandHomePage + child pages)
```

> **EN mirror:** All MK pages have an EN equivalent under `/en/`. Umbraco culture variants should be used (`mk` + `en` per node) rather than a separate content tree.

---

## 5. Full URL Sitemap

### MK (Macedonian — root)

```
/                                                                   Home
/naselenie/                                                         Individuals home

  Deposits
  /naselenie/depoziti/za-depozitite/                               About Deposits
  /naselenie/depoziti/po-viduvanje/shtedni-smetki/                 Savings Accounts
  /naselenie/depoziti/po-viduvanje/paket-plus/                     Package Plus
  /naselenie/depoziti/po-viduvanje/fleksi-depozit/                 Flexi Deposit
  /naselenie/depoziti/oroceni-depoziti/18-20-depozit/              18/20 Deposit
  /naselenie/depoziti/oroceni-depoziti/12-13-depozit/              12/13 Deposit
  /naselenie/depoziti/oroceni-depoziti/6-i-9-meseci-orocen-depozit/ 6/9 Month Deposit
  /naselenie/depoziti/oroceni-depoziti/pcelka-detsko-shtedenje/    Bee Children's Savings
  /naselenie/depoziti/oroceni-depoziti/depozit-orocen-na-1-2-3-6-12-24-36-meseci/ Regular Deposit
  /naselenie/depoziti/oroceni-depoziti/premium/                    Premium
  /naselenie/depoziti/oroceni-depoziti/moj-plan/                   My Plan
  /naselenie/depoziti/oroceni-depoziti/evrobonus/                  EuroBonus

  Consumer Loans
  /naselenie/krediti/                                              Loans overview
  /naselenie/krediti/potroshuvacki-kredit/do-1-500-000-denari-25-000-evra/
  /naselenie/krediti/potroshuvacki-kredit/potroshuvacki-obezbeden-kredit/
  /naselenie/krediti/potroshuvacki-kredit/moj-kesh/
  /naselenie/krediti/potroshuvacki-kredit/namenski-potroshuvacki-kredit-preku-trgovci/
  /naselenie/krediti/potroshuvacki-kredit/obezbeden-potroshuvacki-kredit-za-klienti-so-stanben-kredit-vo-sb/
  /naselenie/krediti/stanbeni-krediti/glavni-karakteristiki/
  /naselenie/krediti/stanbeni-krediti/karakteristiki-za-stanovi-vo-izgradba/
  /naselenie/krediti/stanbeni-krediti/karakteristiki-na-stanben-kredit/
  /naselenie/krediti/avtomobilski-kredit/so-zalog-na-vozilo/

  Cards
  /naselenie/karticki/                                             Cards overview
  /naselenie/karticki/kreditni-karticki/mastercard-standard/
  /naselenie/karticki/kreditni-karticki/visa-star/
  /naselenie/karticki/kreditni-karticki/visa-vero/
  /naselenie/karticki/kreditni-karticki/visa-zero/
  /naselenie/karticki/kreditni-karticki/visa-gold/
  /naselenie/karticki/debitni-karticki/mastercard-debit/
  /naselenie/karticki/debitni-karticki/mastercard-topsi/
  /naselenie/karticki/debitni-karticki/mastercard-platinum/
  /naselenie/karticki/debitni-karticki/visa-classic/
  /naselenie/karticki/debitni-karticki/visa-gold-debit/
  /naselenie/karticki/debitni-karticki/visa-internet/
  /naselenie/karticki/promocii/kupuvanje-na-rati-bez-kamata/
  /naselenie/karticki/promocii/promo-kesh-vikend/
  /naselenie/karticki/promocii/mastercard-day/
  /naselenie/karticki/promocii/kesh-na-rati-bez-kamata/
  /naselenie/karticki/uplata-na-gotovina-na-bankomat/
  /naselenie/karticki/google-pay/
  /naselenie/karticki/apple-pay/
  /naselenie/karticki/visa-direct-isprati-pari-na-visa-karticka/
  /naselenie/karticki/bezbedno-plakjanje-so-karticki/

  Accounts
  /naselenie/smetki/platezni-smetki/
  /naselenie/smetki/platezni-smetki-so-osnovni-funkcii/
  /naselenie/smetki/precekoruvanje-na-smetka/
  /naselenie/smetki/plata-plus/
  /naselenie/smetki/informacija-za-zakonot-za-platezni-uslugi-i-platni-sistemi/
  /naselenie/smetki/alatka-za-prebaruvanje-smetki/

  Digital Banking
  /naselenie/digitalno-bankarstvo/azuriranje-na-klient-so-oneid/
  /naselenie/digitalno-bankarstvo/e-registracija-reaktivacija/
  /naselenie/digitalno-bankarstvo/e-banking/
  /naselenie/digitalno-bankarstvo/m-banking/
  /naselenie/digitalno-bankarstvo/brzi-plakjanja-topsi-pay/
  /naselenie/digitalno-bankarstvo/atm-plakjanja/
  /naselenie/digitalno-bankarstvo/korisni-video-upatstva/

  Insurance
  /naselenie/osiguruvanje/osiguruvanje-vo-stopanska-banka/
  /naselenie/osiguruvanje/kreditno-zivotno-osiguruvanje/
  /naselenie/osiguruvanje/riziko-zivotno-osiguruvanje/
  /naselenie/osiguruvanje/penzisko-osiguruvanje/
  /naselenie/osiguruvanje/patnicko-osiguruvanje/
  /naselenie/osiguruvanje/imotno-osiguruvanje/
  /naselenie/osiguruvanje/avtomobilsko-osiguruvanje/
  /naselenie/osiguruvanje/zdravstveno-osiguruvanje/
  /naselenie/osiguruvanje/osiguruvanje-od-nezgoda/
  /naselenie/osiguruvanje/osiguruvanje-na-otplata-na-kredit/
  /naselenie/osiguruvanje/unit-link-zhivotno-osiguruvanje/

  Payment Services
  /naselenie/platezni-uslugi/platezni-uslugi-vo-zemjata/
  /naselenie/platezni-uslugi/platezni-uslugi-so-stranstvo/
  /naselenie/platezni-uslugi/western-union/

  Other under Individuals
  /naselenie/topsi-za-mladi/
  /naselenie/traen-nalog/
  /naselenie/sefovi/
  /naselenie/tarifi-naselenie/
  /naselenie/aplikacija-za-azuriranje-za-naselenie/
  /naselenie/standardna-tabela-so-podatoci-po-oddelni-krediti-i-depoziti/
  /naselenie/katce-za-korisnici/
  /naselenie/zashtita-na-licni-podatoci/
  /naselenie/portal-za-otvoreno-bankarstvo/
  /naselenie/informativni-formulari-za-kreditni-proizvodi/

/pravni-lica/                                                       Legal Entities home
  /pravni-lica/digitalno-bankarstvo/e-commerce/
  /pravni-lica/kreditni-proizvodi/
  /pravni-lica/depoziti/
  /pravni-lica/dokumentarno-rabotenje/

/finansiski-pazari/kursna-lista/                                    Exchange Rate
/finansiski-pazari/devizen-pazar/                                   Forex Market
/finansiski-pazari/drzavni-khartii-od-vrednost/                     Government Securities
/finansiski-pazari/brokerski-uslugi/                                Broker Services
/finansiski-pazari/menuvacko-rabotenje/                             Currency Exchange

/za-bankata/profil-na-bankata/                                      Bank Profile
/za-bankata/korporativno-upravuvanje/                               Corporate Governance
/za-bankata/korporativno-upravuvanje/tela-na-korporativno-upravuvanje/odbor-za-revizija/
/za-bankata/za-akcionerite/                                         Shareholders
/za-bankata/podatoci-i-izveshtai/                                   Reports & Data
/za-bankata/kamatni-stapki/                                         Interest Rates
/za-bankata/glavni-korespodentni-banki/                             Correspondent Banks
/za-bankata/kontakt/                                                Contact
/za-bankata/politika-za-privatnost-i-postavuvanje-na-kolacinja/     Privacy Policy & Cookies

/marketing-i-oop/opshtestveno-odgovorni-praktiki/
/marketing-i-oop/realizirani-proekti/
/marketing-i-oop/logo-i-trgovski-marki/
/marketing-i-oop/reklamni-kampanji/
/marketing-i-oop/intervjua-i-publikacii/
/marketing-i-oop/sonuvame-menuvame-vol-7/
/marketing-i-oop/vozi-pravo-vozi-zdravo-vol-7/
/marketing-i-oop/vozi-pravo-vozi-zdravo-vol-8/

/sb-lokacii/mreza-na-filijali/                                      Branch Network Map
/sb-lokacii/mreza-na-bankomati/                                     ATM Network Map

/novosti/                                                           News listing
/novosti/[slug]/                                                    News article (N items)

/kariera-vo-stb/kariera-vo-stb/                                     Career

/ostanati-sodrzini/kalkulatori/                                     Calculators
/ostanati-sodrzini/prodazba-na-imot/prodazba-na-imot-na-bankata/    Property Sale
/ostanati-sodrzini/tarifi/                                          Tariffs
/ostanati-sodrzini/katce-za-korisnici/                              User Corner
/ostanati-sodrzini/uslovi-za-koristenje/                            Terms of Use
/ostanati-sodrzini/rss/                                             RSS Feed
/ostanati-sodrzini/portal-za-otvoreno-bankarstvo/                   Open Banking Portal

/topsi/                                                             TOPSI sub-brand home
/topsi//proizvodi/site-proizvodi/                                   TOPSI Products

/goldenclub/doma/                                                   GoldenClub home

/appForm/[product-slug]                                             Online application forms
  e.g. /appForm/potrosuvacki-kredit-bez-obezbeduvanje-1500000

/docs/                                                              Open Banking Docs (Next.js)
/docs/getting-started
/docs/access-to-accounts
/docs/payment-initiation
/docs/funds-confirmation
/docs/api-reference
/docs/glossary

/faq                                                                FAQ
/contact-us                                                         Contact (EN alias?)
/terms-of-use                                                       Terms of Use (EN alias?)
```

### EN (English) — `/en/` prefix

Mirrors MK structure with English slugs:
```
/en/                                                                EN Home
/en/individuals/                                                    Individuals EN
/en/individuals/deposits/[product-slug]/
/en/individuals/loans/consumer-loan/[product-slug]/
/en/individuals/loans/home-loans/[product-slug]/
/en/individuals/cards/credit-cards/[product-slug]/
/en/individuals/cards/debit-cards/[product-slug]/
/en/individuals/accounts/[product-slug]/
/en/individuals/i-bank/e-banking1/
/en/individuals/i-bank/m-banking1/
/en/individuals/i-bank/phone-banking/
/en/individuals/i-bank/atm-banking/
/en/individuals/loans/consumer-loan/consumer-loan-up-to-mkd-1-200-000-20-000-eur/
/en/individuals/loans/consumer-loan/consumer-secured-loan-up-to-eur-80-000/
/en/legal-entities/payment-operations/
/en/legal-entities/digital-banking/
/en/sb-locations/branch-network/
/en/sb-locations/atm-network/
/en/sb-locations/regional-business-centres/
/en/other-content/sb-tariffs/
/en/other-content/sb-tariffs/tariffs-individuals/
/en/news/                                                           EN News listing
/en/news/[slug]/                                                    EN News articles
/en/for-the-bank/ (implied)
```

---

## 6. External Dependencies

### JavaScript (CDN)
| Library | Source | Version |
|---|---|---|
| jQuery | ajax.aspnetcdn.com (Microsoft CDN) | 3.0.0 |
| jQuery Validate | ajax.aspnetcdn.com | 1.16.0 |
| jQuery Validate Unobtrusive | ajax.aspnetcdn.com (ASP.NET MVC) | 5.2.3 |
| Moment.js (with locales) | momentjs.com/downloads/ | latest at crawl |

### Fonts
| Font | Source |
|---|---|
| Open Sans (ital, wght 300–800) | fonts.googleapis.com / fonts.gstatic.com |
| Open Sans (local backup) | /css/open-sans-font.css |

### Analytics & Tracking
| Tool | Identifier |
|---|---|
| Google Analytics 4 | G-VNYDDYL1MY (via GTM) |
| Google Tag Manager | (embedded with gtag.js) |
| Facebook Pixel | 1917220645254726 |
| Google Site Verification | 0-t73Vga6CCUhM-pB-Ie14sZgVF0owG8azIsDs7w6Sk |

### Icon Sets
| Library | Local Path |
|---|---|
| MDI (Material Design Icons) | /assets/css/icons.min.css |

### Maps
| Library | Local Path |
|---|---|
| Google Maps (gmaps.js wrapper) | /assets/js/vendor/gmaps.min.js |
| jVectorMap 1.2.2 | /assets/js/vendor/jquery-jvectormap-1.2.2.min.js + /assets/css/vendor/jquery-jvectormap-1.2.2.css |

### Charting
| Library | Local Path |
|---|---|
| Chart.js (bundle) | /assets/js/vendor/Chart.bundle.min.js |

### UI Components (local vendor)
| Library | Local Path | Purpose |
|---|---|---|
| Bootstrap 4 | (built into app.css) | Layout & components |
| jConveyorTicker | /scripts/jquery.jConveyorTicker.min.js + /css/jConveyorTicker.css | News headline ticker |
| jQuery Fancybox 1.3.4 | /js/FancyBox/jquery.fancybox-1.3.4.* | Lightbox for property photos |
| jQuery UI | /scripts/apply/jquery-ui.js + /css/jquery-ui.css | Date picker, sliders |
| jQuery Migrate | /js/jquery-migrate-3.1.0.min.js | jQuery compat |
| jQuery Keyboard | /js/jquery.keyboard.js + extensions | Secure virtual keyboard (scramble, caret, autocomplete, typing) |
| jsuites.js | /js/jsuites.js | E-registration form components |
| rangeslider.min.js | /scripts/apply/rangeslider.min.js | Loan/deposit calculator sliders |

### Open Banking Portal (Next.js micro-app — separate)
- **Framework:** Next.js 13+ with App Router
- **Output:** `/\_next/static/chunks/` (many split chunks)
- **Locale routing:** `[locale]/(root-layout)/` pattern in chunk names
- Hosted on same domain at `/docs/` and `/naselenie/portal-za-otvoreno-bankarstvo/`

---

## 7. Interactive & Dynamic Features

| Feature | Implementation | Notes |
|---|---|---|
| **Hero Carousel** | Bootstrap carousel (data-ride, data-slide-to) | 5 slides; separate desktop + mobile images |
| **News Ticker** | jConveyorTicker | Auto-scrolling headlines; data from Umbraco |
| **Nav Dropdowns** | Bootstrap collapse + dropdown | 3-level hierarchy; context switches between Individuals / Legal Entities |
| **Currency Exchange Rate** | Custom JS `loadExchangeRate()`, AJAX to backend | Date picker archive; rates injected into `#divExchangeRate`; print support |
| **Loan/Deposit Calculators** | Calculators.js (custom), range sliders | 10 product types; real-time payment calculation; inputs: amount, term, rate |
| **Branch/ATM Locator** | Google Maps (gmaps.min.js) + template binding | Filter by city; slide-in detail pane; likely AngularJS or similar |
| **Account Search Tool** | Unknown (JS) | `/naselenie/smetki/alatka-za-prebaruvanje-smetki/` |
| **Online Application Forms** | Custom ASP.NET form + range sliders | Range slider for amount/term, separate CSS/JS stack |
| **Complaint Form** | ASP.NET ASPX (FormDojaviPrigovori.aspx) | Multiple form types via `?title=1-8` query param |
| **FAQ Accordion** | Bootstrap collapse | On homepage and product pages |
| **Product Tabs** | Bootstrap tabs | 2–5 tabs per product page |
| **Cookie Consent / GDPR** | Custom modal + local storage | Granular ad_storage / analytics_storage consent |
| **Search Panel** | Slide-in sidebar | Triggered by `.right-bar-toggle` click |
| **Print Support** | `.d-print-none` / `.d-print-block` classes | Exchange rates page, pages with documents |
| **Virtual Keyboard** | jQuery Keyboard + scramble extension | Secure PIN entry on e-registration page |
| **OneID Integration** | External service (epin.stb.com.mk) | Client identity update without visiting branch |
| **TOPSI Pay** | Peer-to-peer payment feature | MK-market-specific, QR/fast-payments |
| **Property Sale Gallery** | Fancybox 1.3.4 | Photo lightbox gallery |
| **Open Banking APIs** | Next.js docs + sandbox/prod endpoints | PSD2-compliant API documentation portal |
| **Mobile App Banners** | Dismissible Bootstrap alert | Links to iOS App Store + Google Play + Huawei AppGallery |
| **Video Tutorial Links** | External YouTube links (embedded or linked) | Digital banking tutorials |

---

## 8. Notes for Umbraco 15 / .NET 9 Implementation

1. **Culture Variants:** Use Umbraco culture variants (`mk` / `en`) on every document type — do NOT create a separate EN content tree. Map `/en/` URLs via culture-based routing in `appsettings.json`.

2. **Segment Context (Individuals vs Legal Entities):** The nav and homepage hero change based on the current section. This is handled by which root child node (`/` vs `/pravni-lica/`) the current page lives under. Implement a `_Layout.cshtml` partial that detects the segment from the current page's ancestors and renders the appropriate nav.

3. **Exchange Rate API:** The rates data is loaded dynamically via JavaScript. The Umbraco page is only a shell. Implement a dedicated API controller (`/api/exchange-rates?date=`) that feeds the existing JS, or replace with a Razor-rendered table + HTMX for archive lookup.

4. **Calculators:** The `Calculators.js` file contains all product-specific logic. For the reimplement, either port this as a Blazor component or keep the JS file and provide configuration data from Umbraco via a JSON endpoint.

5. **Media Path Convention:** All media is served from `/media/[id]/[filename]` — this is the standard Umbraco media path. Preserve this convention.

6. **Banners — Dual Images:** Every interior page has a 1600×250 banner; homepage carousel uses 1600×544 (desktop) + 500×250 (mobile). Define `heroMobileImage` and `heroDesktopImage` properties on the hero block.

7. **Sub-brands (TOPSI, GoldenClub):** Implement as separate Umbraco sections with theme-specific layouts (`_TopsiLayout.cshtml`, `_GoldenClubLayout.cshtml`). Each sub-brand needs its own CSS, logo, and footer.

8. **Open Banking Docs (`/docs/`):** This is a Next.js micro-app already deployed. In Umbraco, create a redirect/proxy rule or a passthrough for `/docs/*` routes to the Next.js app. Do not rebuild this in Umbraco.

9. **Application Forms (`/appForm/`):** These are self-contained ASP.NET forms with a completely different CSS. Consider rebuilding as standalone Razor pages (outside the Umbraco content tree) or integrating via an iframe/embed component.

10. **PDFs in `/media/`:** 470 PDF documents (tariffs, forms, governance docs) are stored in Umbraco Media Library. They will be migrated as-is — ensure file import includes all `/media/[id]/` paths.

11. **Footer:** 4-column link structure. Define footer links as a composition or a settings-level document in Umbraco (`FooterSettings` node under `Settings`).

12. **GDPR Cookie Consent:** Integrate with Umbraco Forms or a standalone cookie consent library. The existing consent stores preferences in JS/cookies; replicate with a compliant library (e.g., CookieConsent.js or Cookiebot).
