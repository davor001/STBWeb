using Humanizer;
using STBWeb.Services;
using static NPoco.Snapshot<T>;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<HomepageFallbackService>();

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
