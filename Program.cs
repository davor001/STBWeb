using STBWeb.Services;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<HomepageFallbackService>();

builder.CreateUmbracoBuilder()
    .AddBackOffice()
    .AddWebsite()
    .AddComposers()
    .Build();

WebApplication app = builder.Build();

// Ensure Umbraco's media directory exists before boot.
// wwwroot/media/ is gitignored (content only), so it may be absent on a fresh Azure deploy.
// PhysicalFileProvider throws DirectoryNotFoundException at startup if the path is missing.
Directory.CreateDirectory(Path.Combine(app.Environment.WebRootPath, "media"));

await app.BootUmbracoAsync();

// Serve static files from wwwroot/ BEFORE Umbraco routing
app.UseStaticFiles();

var scrapedMediaPath = Path.Combine(app.Environment.ContentRootPath, "scraped-media");
if (Directory.Exists(scrapedMediaPath))
{
    // Temporary homepage verification fallback: expose locally scraped homepage media.
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(scrapedMediaPath),
        RequestPath = "/scraped-media"
    });
}

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
