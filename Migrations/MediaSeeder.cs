using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.Services;
using IOFile = System.IO.File;

namespace STBWeb.Migrations;

/// <summary>
/// Phase 1F — Media Migration.
/// On startup:
///   1. Copies every file from reference/stb-crawl/www.stb.com.mk/media/[id]/[file]
///      to wwwroot/media/[id]/[file], preserving the original site's media URLs.
///   2. Creates a folder structure in the Umbraco media library and an IMedia
///      node for each file so they appear in the backoffice.
/// Idempotent: skips if the sentinel folder "STB Media Import" already exists.
/// Registered in DocumentTypeMigrationComposer (runs last).
/// </summary>
public class MediaSeederHandler : INotificationAsyncHandler<UmbracoApplicationStartedNotification>
{
    private static readonly HashSet<string> ImageExtensions =
        new(StringComparer.OrdinalIgnoreCase) { "jpg", "jpeg", "png", "gif", "svg", "webp", "bmp", "ico" };

    private readonly IMediaService _mediaService;
    private readonly IWebHostEnvironment _webHostEnvironment;
    private readonly ILogger<MediaSeederHandler> _logger;

    public MediaSeederHandler(
        IMediaService mediaService,
        IWebHostEnvironment webHostEnvironment,
        ILogger<MediaSeederHandler> logger)
    {
        _mediaService = mediaService;
        _webHostEnvironment = webHostEnvironment;
        _logger = logger;
    }

    public Task HandleAsync(UmbracoApplicationStartedNotification notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("STBWeb: Starting media seeder...");
        try
        {
            SeedMedia();
            _logger.LogInformation("STBWeb: Media seeder finished.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "STBWeb: Media seeder failed.");
        }
        return Task.CompletedTask;
    }

    private void SeedMedia()
    {
        // Idempotency: sentinel folder
        var existing = _mediaService.GetRootMedia()
            .FirstOrDefault(m => m.Name == "STB Media Import");
        if (existing != null)
        {
            _logger.LogInformation("STBWeb: Media already imported (sentinel found), skipping.");
            return;
        }

        var contentRoot = _webHostEnvironment.ContentRootPath;
        var webRoot = _webHostEnvironment.WebRootPath;

        var crawlMediaDir = Path.Combine(contentRoot, "reference", "stb-crawl", "www.stb.com.mk", "media");
        if (!Directory.Exists(crawlMediaDir))
        {
            _logger.LogWarning("STBWeb: Crawl media directory not found at {Path}. Skipping.", crawlMediaDir);
            return;
        }

        var wwwMediaDir = Path.Combine(webRoot, "media");
        Directory.CreateDirectory(wwwMediaDir);

        // ── Umbraco media library structure ──────────────────────────────────
        var rootFolder = _mediaService.CreateMedia("STB Media Import", -1, "Folder");
        _mediaService.Save(rootFolder);

        var imgFolder = _mediaService.CreateMedia("Images", rootFolder.Id, "Folder");
        _mediaService.Save(imgFolder);

        var docFolder = _mediaService.CreateMedia("Documents", rootFolder.Id, "Folder");
        _mediaService.Save(docFolder);

        // ── Import media/[id]/[filename] files ────────────────────────────────
        var numericDirs = Directory.GetDirectories(crawlMediaDir)
            .Where(d => int.TryParse(Path.GetFileName(d), out _))
            .OrderBy(d => int.Parse(Path.GetFileName(d)));

        int copied = 0, skipped = 0, errors = 0;

        foreach (var sourceDir in numericDirs)
        {
            var dirId = Path.GetFileName(sourceDir);
            var files = Directory.GetFiles(sourceDir);
            if (files.Length == 0) continue;

            var sourceFile = files[0];
            var fileName = Path.GetFileName(sourceFile);
            var ext = Path.GetExtension(fileName).TrimStart('.').ToLowerInvariant();

            // Copy physical file to wwwroot/media/[id]/[filename]
            var destDir = Path.Combine(wwwMediaDir, dirId);
            Directory.CreateDirectory(destDir);
            var destFile = Path.Combine(destDir, fileName);

            try
            {
                if (!IOFile.Exists(destFile))
                {
                    IOFile.Copy(sourceFile, destFile);
                    copied++;
                }
                else
                {
                    skipped++;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "STBWeb: Could not copy media file {File}", sourceFile);
                errors++;
                continue;
            }

            // Create Umbraco IMedia node
            try
            {
                var isImage = ImageExtensions.Contains(ext);
                var mediaTypeAlias = isImage ? "Image" : "File";
                var parentFolderId = isImage ? imgFolder.Id : docFolder.Id;
                var nodeName = Path.GetFileNameWithoutExtension(fileName);
                if (nodeName.Length > 200) nodeName = nodeName[..200];

                // URL for the copied file (preserve original path)
                var urlPath = $"/media/{dirId}/{Uri.EscapeDataString(fileName)}";
                var fileJson = $"{{\"src\":\"{urlPath}\"}}";

                var media = _mediaService.CreateMedia(nodeName, parentFolderId, mediaTypeAlias);
                media.SetValue("umbracoFile", fileJson);
                media.SetValue("umbracoExtension", ext);
                media.SetValue("umbracoBytes", new FileInfo(destFile).Length);
                _mediaService.Save(media);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "STBWeb: Could not create IMedia node for {File}", fileName);
            }
        }

        // ── Also copy content/Pdf/ to wwwroot/media/pdfs/ ────────────────────
        var crawlPdfDir = Path.Combine(contentRoot, "reference", "stb-crawl", "www.stb.com.mk", "content", "Pdf");
        if (Directory.Exists(crawlPdfDir))
        {
            var pdfDestDir = Path.Combine(wwwMediaDir, "pdfs");
            Directory.CreateDirectory(pdfDestDir);

            var pdfFolder = _mediaService.CreateMedia("Content PDFs", rootFolder.Id, "Folder");
            _mediaService.Save(pdfFolder);

            foreach (var pdfFile in Directory.GetFiles(crawlPdfDir, "*.pdf", SearchOption.AllDirectories))
            {
                var pdfName = Path.GetFileName(pdfFile);
                var destPdf = Path.Combine(pdfDestDir, pdfName);

                try
                {
                    if (!IOFile.Exists(destPdf))
                        IOFile.Copy(pdfFile, destPdf);

                    var urlPath = $"/media/pdfs/{Uri.EscapeDataString(pdfName)}";
                    var nodeName = Path.GetFileNameWithoutExtension(pdfName);
                    if (nodeName.Length > 200) nodeName = nodeName[..200];

                    var media = _mediaService.CreateMedia(nodeName, pdfFolder.Id, "File");
                    media.SetValue("umbracoFile", $"{{\"src\":\"{urlPath}\"}}");
                    media.SetValue("umbracoExtension", "pdf");
                    media.SetValue("umbracoBytes", new FileInfo(destPdf).Length);
                    _mediaService.Save(media);
                    copied++;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "STBWeb: Could not process PDF {File}", pdfFile);
                    errors++;
                }
            }
        }

        _logger.LogInformation(
            "STBWeb: Media import done. Copied={Copied} Skipped={Skipped} Errors={Errors}",
            copied, skipped, errors);
    }
}