using Microsoft.Extensions.Logging;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.Services;

namespace STBWeb.Migrations;

/// <summary>
/// Seeds the Umbraco content tree with the full STB site structure (~110 pages, 2 cultures).
/// Registered in DocumentTypeMigrationComposer so it runs after document types are created.
/// Idempotent: skips if root content already exists.
///
/// MK culture names use Latin-transliterated forms (matching original URL slugs) so that
/// Umbraco's ShortStringHelper produces correct URL segments (e.g. "Naselenie" → "naselenie").
/// EN culture names use plain English display text.
/// </summary>
public class ContentSeederHandler : INotificationAsyncHandler<UmbracoApplicationStartedNotification>
{
    private readonly IContentService _contentService;
    private readonly IContentTypeService _contentTypeService;
    private readonly ILanguageService _languageService;
    private readonly IDomainService _domainService;
    private readonly ILogger<ContentSeederHandler> _logger;

    public ContentSeederHandler(
        IContentService contentService,
        IContentTypeService contentTypeService,
        ILanguageService languageService,
        IDomainService domainService,
        ILogger<ContentSeederHandler> logger)
    {
        _contentService = contentService;
        _contentTypeService = contentTypeService;
        _languageService = languageService;
        _domainService = domainService;
        _logger = logger;
    }

    public async Task HandleAsync(UmbracoApplicationStartedNotification notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("STBWeb: Starting content seeder...");
        try
        {
            await EnsureLanguagesAsync();
            await SeedContentAsync();
            EnsureOstanato(); // idempotent patch — adds missing section on every startup
            _logger.LogInformation("STBWeb: Content seeder finished.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "STBWeb: Content seeder failed.");
        }
    }

    // -------------------------------------------------------------------------
    // Languages
    // -------------------------------------------------------------------------
    private async Task EnsureLanguagesAsync()
    {
        var mk = await _languageService.GetAsync("mk");
        if (mk == null)
        {
            var mkLang = new Language("mk", "Macedonian") { IsDefault = true, IsMandatory = true };
            await _languageService.CreateAsync(mkLang, Constants.Security.SuperUserKey);
            _logger.LogInformation("STBWeb: Created language 'mk'.");
        }

        var en = await _languageService.GetAsync("en");
        if (en == null)
        {
            var enLang = new Language("en", "English") { IsDefault = false, IsMandatory = false };
            await _languageService.CreateAsync(enLang, Constants.Security.SuperUserKey);
            _logger.LogInformation("STBWeb: Created language 'en'.");
        }
    }

    // -------------------------------------------------------------------------
    // Content tree
    // NOTE: MK names are Latin-transliterated so Umbraco generates correct slugs.
    // e.g. "Depoziti" → slug "depoziti" → URL /depoziti/ (matches original site)
    //
    // PRIMARY NAV (items 1-8 below) are direct children of Home — this gives
    // them top-level URLs (/depoziti/, /krediti/, etc.) matching stb.com.mk.
    // Secondary sections (Pravni Lica, Finansiski Pazari, etc.) are also direct
    // children of Home but are excluded from the primary nav via the nav view filter.
    // -------------------------------------------------------------------------
    private async Task SeedContentAsync()
    {
        // Guard: doc types must exist
        if (_contentTypeService.Get("homePage") == null)
        {
            _logger.LogWarning("STBWeb: 'homePage' content type not found — content seeding skipped.");
            return;
        }

        // Idempotency: skip if any root content already exists
        if (_contentService.GetRootContent().Any())
        {
            _logger.LogInformation("STBWeb: Root content already exists — seeder skipped.");
            return;
        }

        // ── HOME ─────────────────────────────────────────────────────────────
        var home = _contentService.Create("Home", -1, "homePage", -1);
        home.SetCultureName("Doma", "mk");
        home.SetCultureName("Home", "en");

        var saveResult = _contentService.Save(home);
        if (!saveResult.Success || home.Id <= 0)
        {
            _logger.LogError("CRITICAL ERROR: Failed to save Home node. Validation failed. Result: {@Result}", saveResult.Result);
            return;
        }

        var publishResult = _contentService.Publish(home, new[] { "*" });
        if (!publishResult.Success)
            _logger.LogWarning("WARNING: Failed to publish Home node. Result: {@Result}", publishResult.Result);

        await EnsureDomainsAsync(home.Key);
        var homeId = home.Id;

        // ── 1. ДЕПОЗИТИ ───────────────────────────────────────────────────────
        var depId = S("Deposits", "Depoziti", "Deposits", homeId, "sectionRoot");
        if (depId > 0)
        {
            S("About Deposits", "Za Depozitite", "About Deposits", depId, "genericContentPage");

            var demandId = S("Demand Deposits", "Po Viduvanje", "Demand Deposits", depId, "sectionRoot");
            if (demandId > 0)
            {
                S("Savings Accounts", "Shtedni Smetki", "Savings Accounts", demandId, "productDetailPage");
                S("Package Plus", "Paket Plus", "Package Plus", demandId, "productDetailPage");
                S("Flexi Deposit", "Fleksi Depozit", "Flexi Deposit", demandId, "productDetailPage");
            }

            var termId = S("Term Deposits", "Oroceni Depoziti", "Term Deposits", depId, "sectionRoot");
            if (termId > 0)
            {
                S("18-20 Deposit", "18-20 Depozit", "18/20 Deposit", termId, "productDetailPage");
                S("12-13 Deposit", "12-13 Depozit", "12/13 Deposit", termId, "productDetailPage");
                S("6-9 Month Deposit", "6 i 9 Meseci Orocen Depozit", "6/9 Month Deposit", termId, "productDetailPage");
                S("Bee Childrens Savings", "Pcelka Detsko Shtedenje", "Bee Children's Savings", termId, "productDetailPage");
                S("Regular Deposit", "Depozit Orocen Na 1 2 3 6 12 24 36 Meseci", "Regular Deposit", termId, "productDetailPage");
                S("Premium Deposit", "Premium", "Premium Deposit", termId, "productDetailPage");
                S("My Plan", "Moj Plan", "My Plan", termId, "productDetailPage");
                S("EuroBonus", "Evrobonus", "EuroBonus", termId, "productDetailPage");
            }
        }

        // ── 2. КРЕДИТИ ────────────────────────────────────────────────────────
        var loansId = S("Loans", "Krediti", "Loans", homeId, "sectionRoot");
        if (loansId > 0)
        {
            var conLoansId = S("Consumer Loans", "Potroshuvacki Kredit", "Consumer Loans", loansId, "sectionRoot");
            if (conLoansId > 0)
            {
                S("Up to 1500000 MKD", "Do 1 500 000 Denari 25 000 Evra", "Up to 1,500,000 MKD", conLoansId, "productDetailPage");
                S("Secured Consumer Loan", "Potroshuvacki Obezbeden Kredit", "Secured Consumer Loan", conLoansId, "productDetailPage");
                S("My Cash", "Moj Kesh", "My Cash", conLoansId, "productDetailPage");
                S("Purpose Loan via Retailers", "Namenski Potroshuvacki Kredit Preku Trgovci", "Purpose Loan via Retailers", conLoansId, "productDetailPage");
                S("Secured for Homeowners", "Obezbeden Potroshuvacki Kredit Za Klienti So Stanben Kredit Vo SB", "Secured for Homeowners in SB", conLoansId, "productDetailPage");
            }

            var homeLoansId = S("Home Loans", "Stanbeni Krediti", "Home Loans", loansId, "sectionRoot");
            if (homeLoansId > 0)
            {
                S("Main Characteristics", "Glavni Karakteristiki", "Main Characteristics", homeLoansId, "productDetailPage");
                S("Under-construction Properties", "Karakteristiki Za Stanovi Vo Izgradba", "Under-construction Properties", homeLoansId, "productDetailPage");
            }

            var autoLoanId = S("Auto Loan", "Avtomobilski Kredit", "Auto Loan", loansId, "sectionRoot");
            if (autoLoanId > 0)
                S("With Vehicle Pledge", "So Zalog Na Vozilo", "With Vehicle Pledge", autoLoanId, "productDetailPage");
        }

        // ── 3. КАРТИЧКИ ───────────────────────────────────────────────────────
        var cardsId = S("Cards", "Karticki", "Cards", homeId, "sectionRoot");
        if (cardsId > 0)
        {
            var creditCardsId = S("Credit Cards", "Kreditni Karticki", "Credit Cards", cardsId, "sectionRoot");
            if (creditCardsId > 0)
            {
                S("MasterCard Standard", "Mastercard Standard", "MasterCard Standard", creditCardsId, "productDetailPage");
                S("VISA Vero", "Visa Vero", "VISA Vero", creditCardsId, "productDetailPage");
                S("VISA Zero", "Visa Zero", "VISA Zero", creditCardsId, "productDetailPage");
                S("VISA Gold Credit", "Visa Gold", "VISA Gold", creditCardsId, "productDetailPage");
            }

            var debitCardsId = S("Debit Cards", "Debitni Karticki", "Debit Cards", cardsId, "sectionRoot");
            if (debitCardsId > 0)
            {
                S("Mastercard Debit", "Mastercard Debit", "Mastercard Debit", debitCardsId, "productDetailPage");
                S("Mastercard TOPSI", "Mastercard Topsi", "Mastercard TOPSI", debitCardsId, "productDetailPage");
                S("Mastercard Platinum", "Mastercard Platinum", "Mastercard Platinum", debitCardsId, "productDetailPage");
                S("VISA Internet", "Visa Internet", "VISA Internet", debitCardsId, "productDetailPage");
            }

            var promoId = S("Promotions", "Promocii", "Promotions", cardsId, "sectionRoot");
            if (promoId > 0)
            {
                S("Installments Without Interest", "Kupuvanje Na Rati Bez Kamata", "Installments Without Interest", promoId, "productDetailPage");
                S("Promo Cash Weekend", "Promo Kesh Vikend", "Promo Cash Weekend", promoId, "productDetailPage");
                S("Mastercard Day", "Mastercard Day", "Mastercard Day", promoId, "productDetailPage");
                S("Cash Installments Without Interest", "Kesh Na Rati Bez Kamata", "Cash Installments Without Interest", promoId, "productDetailPage");
            }

            S("ATM Cash Deposit", "Uplata Na Gotovina Na Bankomat", "ATM Cash Deposit", cardsId, "productDetailPage");
            S("Google Pay", "Google Pay", "Google Pay", cardsId, "productDetailPage");
            S("Apple Pay", "Apple Pay", "Apple Pay", cardsId, "productDetailPage");
            S("Visa Direct", "Visa Direct Isprati Pari Na Visa Karticka", "Visa Direct", cardsId, "productDetailPage");
            S("Secure Payment", "Bezbedno Plakjanje So Karticki", "Secure Payment", cardsId, "productDetailPage");
        }

        // ── 4. СМЕТКИ ────────────────────────────────────────────────────────
        var accId = S("Accounts", "Smetki", "Accounts", homeId, "sectionRoot");
        if (accId > 0)
        {
            S("Payment Accounts", "Platezni Smetki", "Payment Accounts", accId, "productDetailPage");
            S("Basic Payment Accounts", "Platezni Smetki So Osnovni Funkcii", "Basic Payment Accounts", accId, "productDetailPage");
            S("Overdraft", "Precekoruvanje Na Smetka", "Overdraft", accId, "productDetailPage");
            S("Salary Plus", "Plata Plus", "Salary Plus", accId, "productDetailPage");
            S("Payment Services Act Info", "Informacija Za Zakonot Za Platezni Uslugi", "Payment Services Act Info", accId, "genericContentPage");
            S("Account Search Tool", "Alatka Za Prebaruvanje Smetki", "Account Search Tool", accId, "genericContentPage");
        }

        // ── 5. ДИГИТАЛНО БАНКАРСТВО ──────────────────────────────────────────
        var digBankId = S("Digital Banking", "Digitalno Bankarstvo", "Digital Banking", homeId, "sectionRoot");
        if (digBankId > 0)
        {
            S("OneID Client Update", "Azuriranje Na Klient So OneID", "OneID Client Update", digBankId, "productDetailPage");
            S("E-Registration", "E Registracija Reaktivacija", "E-Registration and Reactivation", digBankId, "genericContentPage");
            S("E-Banking", "E Banking", "E-Banking", digBankId, "productDetailPage");
            S("M-Banking", "M Banking", "M-Banking", digBankId, "productDetailPage");
            S("TOPSI Pay", "Brzi Plakjanja Topsi Pay", "TOPSI Pay", digBankId, "productDetailPage");
            S("ATM Payments", "ATM Plakjanja", "ATM Payments", digBankId, "productDetailPage");
            S("Video Tutorials", "Korisni Video Upatstva", "Video Tutorials", digBankId, "genericContentPage");
        }

        // ── 6. ОСИГУРУВАЊЕ ────────────────────────────────────────────────────
        var insId = S("Insurance", "Osiguruvanje", "Insurance", homeId, "sectionRoot");
        if (insId > 0)
        {
            S("Insurance Overview", "Osiguruvanje Vo Stopanska Banka", "Insurance Overview", insId, "genericContentPage");
            S("Credit Life Insurance", "Kreditno Zivotno Osiguruvanje", "Credit Life Insurance", insId, "productDetailPage");
            S("Risk Life Insurance", "Riziko Zivotno Osiguruvanje", "Risk Life Insurance", insId, "productDetailPage");
            S("Pension Insurance", "Penzisko Osiguruvanje", "Pension Insurance", insId, "productDetailPage");
            S("Travel Insurance", "Patnicko Osiguruvanje", "Travel Insurance", insId, "productDetailPage");
            S("Property Insurance", "Imotno Osiguruvanje", "Property Insurance", insId, "productDetailPage");
            S("Auto Insurance", "Avtomobilsko Osiguruvanje", "Auto Insurance", insId, "productDetailPage");
            S("Health Insurance", "Zdravstveno Osiguruvanje", "Health Insurance", insId, "productDetailPage");
            S("Accident Insurance", "Osiguruvanje Od Nezgoda", "Accident Insurance", insId, "productDetailPage");
            S("Loan Repayment Insurance", "Osiguruvanje Na Otplata Na Kredit", "Loan Repayment Insurance", insId, "productDetailPage");
            S("Unit-Link Life Insurance", "Unit Link Zivotno Osiguruvanje", "Unit-Link Life Insurance", insId, "productDetailPage");
        }

        // ── 7. ПЛАТЕЖНИ УСЛУГИ ───────────────────────────────────────────────
        var payServId = S("Payment Services", "Platezni Uslugi", "Payment Services", homeId, "sectionRoot");
        if (payServId > 0)
        {
            S("Domestic Payments", "Platezni Uslugi Vo Zemjata", "Domestic Payments", payServId, "productDetailPage");
            S("International Payments", "Platezni Uslugi So Stranstvo", "International Payments", payServId, "productDetailPage");
            S("Western Union", "Western Union", "Western Union", payServId, "productDetailPage");
        }

        // ── 8. ОСТАНАТО ──────────────────────────────────────────────────────
        var ostId = S("Ostanato", "Ostanato", "Other", homeId, "sectionRoot");
        if (ostId > 0)
        {
            S("TOPSI Za Mladi", "Topsi Za Mladi", "TOPSI for Youth", ostId, "genericContentPage");
            S("Pension Insurance Link", "Penzisko Osiguruvanje", "Pension Insurance", ostId, "genericContentPage");
            S("Standing Order", "Traen Nalog", "Standing Order", ostId, "genericContentPage");
            S("Safe Deposit Boxes", "Sefovi", "Safe Deposit Boxes", ostId, "genericContentPage");
            S("Population Tariffs", "Tarifi Naselenie", "Population Tariffs", ostId, "genericContentPage");
            S("Client Update App", "Aplikacija Za Azuriranje Za Naselenie", "Client Update Application", ostId, "genericContentPage");
            S("Standard Data Table", "Standardna Tabela So Podatoci", "Standard Data Table", ostId, "genericContentPage");
            S("User Corner", "Katce Za Korisnici", "User Corner", ostId, "genericContentPage");
            S("Data Protection", "Zashtita Na Licni Podatoci", "Data Protection", ostId, "genericContentPage");
            S("Open Banking Portal", "Portal Za Otvoreno Bankarstvo", "Open Banking Portal", ostId, "genericContentPage");
        }

        // ── ПРАВНИ ЛИЦА ──────────────────────────────────────────────────────
        // (Not in primary nav — accessed via segment switcher)
        var legalId = S("Legal Entities", "Pravni Lica", "Legal Entities", homeId, "sectionRoot");
        if (legalId > 0)
        {
            var digBankLegalId = S("Digital Banking LE", "Digitalno Bankarstvo", "Digital Banking", legalId, "sectionRoot");
            if (digBankLegalId > 0)
                S("E-Commerce", "E Commerce", "E-Commerce", digBankLegalId, "productDetailPage");

            S("Credit Products", "Kreditni Proizvodi", "Credit Products", legalId, "genericContentPage");
            S("Deposits Legal", "Depoziti", "Deposits", legalId, "genericContentPage");
            S("Documentary Banking", "Dokumentarno Rabotenje", "Documentary Banking", legalId, "genericContentPage");
        }

        // ── ФИНАНСИСКИ ПАЗАРИ ─────────────────────────────────────────────────
        var finMktId = S("Financial Markets", "Finansiski Pazari", "Financial Markets", homeId, "sectionRoot");
        if (finMktId > 0)
        {
            S("Exchange Rate", "Kursna Lista", "Exchange Rate", finMktId, "exchangeRatePage");
            S("Forex Market", "Devizen Pazar", "Forex Market", finMktId, "genericContentPage");
            S("Broker Services", "Brokerski Uslugi", "Broker Services", finMktId, "genericContentPage");
            S("Government Securities", "Drzavni Khartii Od Vrednost", "Government Securities", finMktId, "genericContentPage");
            S("Currency Exchange", "Menuvacko Rabotenje", "Currency Exchange", finMktId, "genericContentPage");
        }

        // ── ЗА БАНКАТА ───────────────────────────────────────────────────────
        var aboutId = S("About Bank", "Za Bankata", "About Bank", homeId, "sectionRoot");
        if (aboutId > 0)
        {
            S("Bank Profile", "Profil Na Bankata", "Bank Profile", aboutId, "genericContentPage");
            var corpGovId = S("Corporate Governance", "Korporativno Upravuvanje", "Corporate Governance", aboutId, "corporateGovernancePage");
            if (corpGovId > 0)
                S("Audit Committee", "Odbor Za Revizija", "Audit Committee", corpGovId, "genericContentPage");

            S("Shareholders", "Za Akcionerite", "Shareholders", aboutId, "genericContentPage");
            S("Reports and Data", "Podatoci I Izvestai", "Reports and Data", aboutId, "genericContentPage");
            S("Interest Rates", "Kamatni Stapki", "Interest Rates", aboutId, "genericContentPage");
            S("Correspondent Banks", "Glavni Korespodentni Banki", "Correspondent Banks", aboutId, "genericContentPage");
            S("Contact", "Kontakt", "Contact", aboutId, "genericContentPage");
            S("Privacy Policy", "Politika Za Privatnost I Kolacinja", "Privacy Policy and Cookies", aboutId, "genericContentPage");
        }

        // ── МАРКЕТИНГ И ООП ───────────────────────────────────────────────────
        var mktId = S("Marketing and CSR", "Marketing I OOP", "Marketing and CSR", homeId, "sectionRoot");
        if (mktId > 0)
        {
            S("CSR Practices", "Opshtestveno Odgovorni Praktiki", "CSR Practices", mktId, "genericContentPage");
            S("Realized Projects", "Realizirani Proekti", "Realized Projects", mktId, "genericContentPage");
            S("Logos and Trademarks", "Logo I Trgovski Marki", "Logos and Trademarks", mktId, "genericContentPage");
            S("Ad Campaigns", "Reklamni Kampanji", "Ad Campaigns", mktId, "genericContentPage");
            S("Interviews and Publications", "Intervjua I Publikacii", "Interviews and Publications", mktId, "genericContentPage");
            S("Sonuvame Menuvame Vol 7", "Sonuvame Menuvame Vol 7", "Sonuvame Menuvame Vol 7", mktId, "genericContentPage");
            S("Vozi Pravo Vol 7", "Vozi Pravo Vozi Zdravo Vol 7", "Vozi Pravo Vozi Zdravo Vol 7", mktId, "genericContentPage");
            S("Vozi Pravo Vol 8", "Vozi Pravo Vozi Zdravo Vol 8", "Vozi Pravo Vozi Zdravo Vol 8", mktId, "genericContentPage");
        }

        // ── СБ ЛОКАЦИИ ────────────────────────────────────────────────────────
        var locsId = S("SB Locations", "SB Lokacii", "Locations", homeId, "sectionRoot");
        if (locsId > 0)
        {
            S("Branch Network", "Mreza Na Filijali", "Branch Network", locsId, "locationMapPage");
            S("ATM Network", "Mreza Na Bankomati", "ATM Network", locsId, "locationMapPage");
            S("Regional Business Centres", "Regionalni Delovni Centri", "Regional Business Centres", locsId, "locationMapPage");
        }

        // ── NEWS / CAREER / SUB-BRANDS ────────────────────────────────────────
        S("News", "Novosti", "News", homeId, "newsListingPage");
        S("Career", "Kariera Vo STB", "Career", homeId, "genericContentPage");
        S("TOPSI", "Topsi", "TOPSI", homeId, "subBrandHomePage");
        S("GoldenClub", "Goldenclub", "GoldenClub", homeId, "subBrandHomePage");

        _logger.LogInformation("STBWeb: Content tree seeded successfully.");
    }

    /// <summary>Binds language domains to the home node so /en/ routing works.</summary>
    private async Task EnsureDomainsAsync(Guid nodeKey)
    {
        var updateModel = new Umbraco.Cms.Core.Models.ContentEditing.DomainsUpdateModel
        {
            Domains = new List<Umbraco.Cms.Core.Models.ContentEditing.DomainModel>
            {
                new Umbraco.Cms.Core.Models.ContentEditing.DomainModel { DomainName = "/", IsoCode = "mk" },
                new Umbraco.Cms.Core.Models.ContentEditing.DomainModel { DomainName = "/en", IsoCode = "en" }
            }
        };
        await _domainService.UpdateDomainsAsync(nodeKey, updateModel);
    }

    /// <summary>
    /// Idempotent patch: creates the "Остanato" primary-nav section if it is absent
    /// from the live content tree.  Works with both the old DB (where Depoziti etc.
    /// are children of a "Naselenie" wrapper) and the new flat tree (where they are
    /// direct children of Home).  Safe to call on every startup.
    /// </summary>
    private void EnsureOstanato()
    {
        var roots = _contentService.GetRootContent().ToList();
        if (!roots.Any()) return; // full seed hasn't run yet — it will create Ostanato

        var homeId = roots.First().Id;

        // Find where the other primary-nav items live.
        // Old tree: they are under a "Naselenie" sectionRoot child of Home.
        // New tree: they are direct children of Home.
        long _;
        var homeChildren = _contentService.GetPagedChildren(homeId, 0L, 500, out _).ToList();

        var naselenie = homeChildren.FirstOrDefault(x =>
            string.Equals(x.Name, "Naselenie", StringComparison.OrdinalIgnoreCase));

        int parentId;
        IEnumerable<Umbraco.Cms.Core.Models.IContent> siblings;
        if (naselenie != null)
        {
            parentId = naselenie.Id;
            siblings = _contentService.GetPagedChildren(parentId, 0L, 500, out _);
        }
        else
        {
            parentId = homeId;
            siblings = homeChildren;
        }

        // Already exists — nothing to do.
        if (siblings.Any(x => string.Equals(x.Name, "Ostanato", StringComparison.OrdinalIgnoreCase)))
            return;

        _logger.LogInformation("STBWeb: 'Ostanato' section not found — creating under parent {ParentId}.", parentId);

        var ostId = S("Ostanato", "Ostanato", "Other", parentId, "sectionRoot");
        if (ostId <= 0) return;

        S("TOPSI Za Mladi",         "Topsi Za Mladi",                        "TOPSI for Youth",           ostId, "genericContentPage");
        S("Pension Insurance Link",  "Penzisko Osiguruvanje",                 "Pension Insurance",         ostId, "genericContentPage");
        S("Standing Order",          "Traen Nalog",                           "Standing Order",            ostId, "genericContentPage");
        S("Safe Deposit Boxes",      "Sefovi",                                "Safe Deposit Boxes",        ostId, "genericContentPage");
        S("Population Tariffs",      "Tarifi Naselenie",                      "Population Tariffs",        ostId, "genericContentPage");
        S("Client Update App",       "Aplikacija Za Azuriranje Za Naselenie", "Client Update Application", ostId, "genericContentPage");
        S("Standard Data Table",     "Standardna Tabela So Podatoci",         "Standard Data Table",       ostId, "genericContentPage");
        S("User Corner",             "Katce Za Korisnici",                    "User Corner",               ostId, "genericContentPage");
        S("Data Protection",         "Zashtita Na Licni Podatoci",            "Data Protection",           ostId, "genericContentPage");
        S("Open Banking Portal",     "Portal Za Otvoreno Bankarstvo",         "Open Banking Portal",       ostId, "genericContentPage");

        _logger.LogInformation("STBWeb: 'Ostanato' section created successfully.");
    }

    /// <summary>Creates and publishes a content node; returns its Id.</summary>
    private int S(string invariantName, string nameMk, string nameEn, int parentId, string alias)
    {
        if (parentId <= 0)
        {
            _logger.LogError("CRITICAL ERROR: Attempted to create child '{Name}' with invalid parentId {ParentId}", invariantName, parentId);
            return -1;
        }

        var content = _contentService.Create(invariantName, parentId, alias);
        content.SetCultureName(nameMk, "mk");
        content.SetCultureName(nameEn, "en");

        // Umbraco 15 requires Save followed by Publish.
        var saveResult = _contentService.Save(content);
        if (!saveResult.Success)
        {
            _logger.LogError("STBWeb: CRITICAL: Failed to save '{Name}'. Errors: {@Errors}", invariantName, saveResult.Result);
            return 0; // Prevent downstream crashes
        }

        var publishResult = _contentService.Publish(content, new[] { "*" });
        if (!publishResult.Success)
        {
            _logger.LogWarning("STBWeb: Could not publish '{Name}'. PublishResult: {@PublishResult}", invariantName, publishResult);
        }

        return content.Id;
    }
}