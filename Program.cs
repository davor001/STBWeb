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
// dotnet publish drops dotfiles (including wwwroot/media/.gitkeep) via the StaticWebAssets
// pipeline, so wwwroot/media/ is absent from every deploy package. The primary fix is the
// explicit Content Include in STBWeb.csproj; this call is a runtime safety net in case the
// directory is still missing (e.g. first deploy before csproj fix, slot swap, local run).
// WebRootPath ?? fallback guards against the rare case where WebRootPath is null because
// the wwwroot subdirectory was not yet created when the host was configured.
var webRoot = app.Environment.WebRootPath
    ?? Path.Combine(app.Environment.ContentRootPath, "wwwroot");
Directory.CreateDirectory(Path.Combine(webRoot, "media"));

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
