using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Logging;
using Umbraco.Cms.Core.Routing;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Web;
using Umbraco.Cms.Infrastructure.Persistence;
using Umbraco.Cms.Web.Website.Controllers;

namespace STBWeb.Controllers
{
    public class AppFormSurfaceController : SurfaceController
    {
        private readonly ILogger<AppFormSurfaceController> _logger;

        public AppFormSurfaceController(
            IUmbracoContextAccessor umbracoContextAccessor,
            IUmbracoDatabaseFactory databaseFactory,
            ServiceContext services,
            AppCaches appCaches,
            IProfilingLogger profilingLogger,
            IPublishedUrlProvider publishedUrlProvider,
            ILogger<AppFormSurfaceController> logger)
            : base(umbracoContextAccessor, databaseFactory, services, appCaches, profilingLogger, publishedUrlProvider)
        {
            _logger = logger;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Submit()
        {
            if (!ModelState.IsValid)
            {
                return CurrentUmbracoPage();
            }

            // Extract basic fields (this is generic to handle various forms)
            var firstName = Request.Form["firstName"];
            var lastName = Request.Form["lastName"];
            var phone = Request.Form["phone"];
            var email = Request.Form["email"];

            // Simulating saving the application
            _logger.LogInformation("STBWeb: Application received from {FirstName} {LastName} ({Phone}, {Email})", 
                firstName, lastName, phone, email);

            TempData["SuccessMessage"] = "Успешно испратена апликација! Нашите агенти наскоро ќе ве контактираат.";

            return RedirectToCurrentUmbracoPage();
        }
    }
}
