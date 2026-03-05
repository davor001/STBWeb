using Examine;
using Microsoft.AspNetCore.Mvc;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.PublishedCache;
using Umbraco.Cms.Web.Common.Controllers;
using Umbraco.Cms.Web.Website.Controllers;
using System.Collections.Generic;
using System.Linq;

namespace STBWeb.Controllers
{
    // A standard Surface Controller allows us to handle the search form POST/GET and return a View
    public class SiteSearchController : SurfaceController
    {
        private readonly IExamineManager _examineManager;
        private readonly IPublishedContentQuery _publishedContentQuery;

        public SiteSearchController(
            Umbraco.Cms.Core.Web.IUmbracoContextAccessor umbracoContextAccessor,
            Umbraco.Cms.Infrastructure.Persistence.IUmbracoDatabaseFactory databaseFactory,
            Umbraco.Cms.Core.Services.ServiceContext services,
            Umbraco.Cms.Core.Cache.AppCaches appCaches,
            Umbraco.Cms.Core.Logging.IProfilingLogger profilingLogger,
            Umbraco.Cms.Core.Routing.IPublishedUrlProvider publishedUrlProvider,
            IExamineManager examineManager,
            IPublishedContentQuery publishedContentQuery) 
            : base(umbracoContextAccessor, databaseFactory, services, appCaches, profilingLogger, publishedUrlProvider)
        {
            _examineManager = examineManager;
            _publishedContentQuery = publishedContentQuery;
        }

        [HttpGet]
        public IActionResult Search(string q)
        {
            var results = new List<SearchResultViewModel>();

            if (string.IsNullOrWhiteSpace(q))
            {
                // Fallback to the current page if no query, but pass empty results
                // Note: Ensure the CurrentPage is rendering the SearchResultsPage
                ViewData["SearchQuery"] = q;
                ViewData["SearchResults"] = results;
                return CurrentUmbracoPage();
            }

            // Determine the culture currently being viewed (en or mk)
            var currentCulture = System.Threading.Thread.CurrentThread.CurrentCulture.Name;
            var isEnglish = currentCulture.StartsWith("en", System.StringComparison.OrdinalIgnoreCase);

            if (_examineManager.TryGetIndex(Umbraco.Cms.Core.Constants.UmbracoIndexes.ExternalIndexName, out var index))
            {
                var searcher = index.Searcher;
                var query = searcher.CreateQuery("content");

                // Examine query checking nodeName and bodyContent. 
                // In Umbraco 14+, culture variants are usually indexed as nodeName_en-us, etc.
                var languageSuffix = isEnglish ? "en" : "mk";
                var nameField = $"nodeName_{languageSuffix}";
                
                // You can also add other fields here based on your document types like bodyText, summary, etc.
                var booleanOperation = query.GroupedOr(
                    new[] { "nodeName", nameField, "bodyContent", "summary", "seoTitle", "seoDescription" }, 
                    q.MultipleCharacterWildcard() // simple wildcard matching
                );

                var searchResults = booleanOperation.Execute();

                foreach (var result in searchResults)
                {
                    // Convert examine result ID to Published Content to get proper URLs and mapped properties
                    if (int.TryParse(result.Id, out int nodeId))
                    {
                        var content = _publishedContentQuery.Content(nodeId);
                        if (content != null)
                        {
                            results.Add(new SearchResultViewModel
                            {
                                Title = content.Name(currentCulture) ?? content.Name,
                                Url = content.Url(currentCulture),
                                Summary = result.Values.ContainsKey("seoDescription") ? result.Values["seoDescription"] : 
                                          (result.Values.ContainsKey("summary") ? result.Values["summary"] : "Резултат од пребарувањето...")
                            });
                        }
                    }
                }
            }

            // Return the View with populated results.
            ViewData["SearchQuery"] = q;
            ViewData["SearchResults"] = results;
            return CurrentUmbracoPage();
        }
    }

    public class SearchResultViewModel
    {
        public string Title { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string Summary { get; set; } = string.Empty;
    }
}
