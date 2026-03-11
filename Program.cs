using System.Globalization;
using Microsoft.AspNetCore.Localization;
using STBWeb.Services;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<HomepageFallbackService>();

// ── MK / EN request localisation ──────────────────────────────────────────
// Supported cultures mirror the Umbraco languages created in ContentSeeder.
// mk is the default (root domain "/"); en lives under "/en/".
builder.Services.AddLocalization();

var supportedCultures = new[] { new CultureInfo("mk"), new CultureInfo("en") };
builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    options
        .SetDefaultCulture("mk")
        .AddSupportedCultures(supportedCultures.Select(c => c.Name).ToArray())
        .AddSupportedUICultures(supportedCultures.Select(c => c.Name).ToArray());

    // Detect culture from the URL prefix that Umbraco's domain binding uses:
    //   /en/...  → "en"
    //   anything else → "mk"
    // This must be first so it overrides Accept-Language / cookie providers.
    options.RequestCultureProviders.Insert(0,
        new CustomRequestCultureProvider(ctx =>
        {
            var path = ctx.Request.Path.Value ?? string.Empty;
            var culture = path.StartsWith("/en/", StringComparison.OrdinalIgnoreCase)
                          || string.Equals(path, "/en", StringComparison.OrdinalIgnoreCase)
                ? "en"
                : "mk";
            return Task.FromResult<ProviderCultureResult?>(new ProviderCultureResult(culture));
        }));
});
// ──────────────────────────────────────────────────────────────────────────

builder.CreateUmbracoBuilder()
    .AddBackOffice()
    .AddWebsite()
    .AddComposers()
    .Build();

WebApplication app = builder.Build();

// ✅ MOVED HERE — must be before BootUmbracoAsync
var mediaPath = Path.Combine(app.Environment.WebRootPath, "media");
Directory.CreateDirectory(mediaPath);
Directory.CreateDirectory(Path.Combine(mediaPath, "Uploads"));

await app.BootUmbracoAsync();

app.UseStaticFiles();

var scrapedMediaPath = Path.Combine(app.Environment.ContentRootPath, "scraped-media");
if (Directory.Exists(scrapedMediaPath))
{
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(scrapedMediaPath),
        RequestPath = "/scraped-media"
    });
}

// Apply the MK/EN culture to Thread.CurrentCulture before Umbraco renders views.
// Must run after BootUmbracoAsync and static files but before Umbraco middleware.
app.UseRequestLocalization();

app.UseUmbraco()
    .WithMiddleware(u =>
    {
        u.UseBackOffice();
        u.UseWebsite();
    })
    .WithEndpoints(u =>
    {
        u.UseBackOfficeEndpoints();
        u.UseWebsiteEndpoints();
    });

await app.RunAsync();