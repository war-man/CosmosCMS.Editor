using CDT.Cosmos.Cms.Common.Controllers;
using CDT.Cosmos.Cms.Common.Data;
using CDT.Cosmos.Cms.Common.Data.Logic;
using CDT.Cosmos.Cms.Common.Models;
using CDT.Cosmos.Cms.Common.Services;
using CDT.Cosmos.Cms.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace CDT.Cosmos.Cms.Controllers
{
    public class HomeController : Controller
    {
        private readonly ArticleLogic _articleLogic;
        private readonly ApplicationDbContext _dbContext;
        private readonly ILogger<HomeController> _logger;
        private readonly IOptions<RedisContextConfig> _redisOptions;
        private readonly IOptions<SiteCustomizationsConfig> _siteOptions;
        private readonly IOptions<GoogleCloudAuthConfig> _gglConfig;

        public HomeController(ILogger<HomeController> logger, ApplicationDbContext dbContext,
            IOptions<SiteCustomizationsConfig> options,
            IOptions<RedisContextConfig> redisOptions,
            ArticleLogic articleLogic,
            IOptions<GoogleCloudAuthConfig> gglConfig)
        {
            _redisOptions = redisOptions;
            _logger = logger;
            _dbContext = dbContext;
            _siteOptions = options;
            _articleLogic = articleLogic;
            _gglConfig = gglConfig;
        }

        public async Task<IActionResult> Index(string id, string lang)
        {
            try
            {
                // ViewData["EditModeOn"] = false;

                // Determine if Google Translate v3 is configured so the javascript support will be added
                ViewData["UseGoogleTranslate"] = string.IsNullOrEmpty(_gglConfig?.Value?.ClientId) == false;


                // Make sure this is Url Encoded, because this is the way it is stored in DB.
                if (!string.IsNullOrEmpty(id))
                    id = _articleLogic.HandleUrlEncodeTitle(id);
                
                ArticleViewModel article;

                if (string.IsNullOrEmpty(lang))
                {
                    lang = "en";
                }

                //
                // Check if we are in read/write mode
                //
                if (_siteOptions.Value.ReadWriteMode)
                {
                    if (User.Identity?.IsAuthenticated == false)
                    {
                        //
                        // See if we need to register a new user.
                        //
                        if (await _dbContext.Users.AnyAsync()) return Redirect("~/Identity/Account/Login");
                        return Redirect("~/Identity/Account/Register");
                    }

                    if (!await _dbContext.Users.AnyAsync(u => u.UserName.Equals(User.Identity.Name)))
                        return RedirectToAction("SignOut", "Users");

                    if (_siteOptions.Value.AllowSetup &&
                        !await _dbContext.Roles.AnyAsync(r => r.Name.Contains("Administrators")))
                        return RedirectToAction("Index", "Setup");

                    if (!User.IsInRole("Reviewers") && !User.IsInRole("Authors") && !User.IsInRole("Editors") &&
                        !User.IsInRole("Administrators") &&
                        !User.IsInRole("Team Members")) return RedirectToAction("AccessPending");

                    if (!await _dbContext.Articles.AnyAsync()) return RedirectToAction("Index", "Editor");

                    //
                    // If yes, do NOT include headers that allow caching.
                    //
                    Response.Headers[HeaderNames.CacheControl] = "no-store";
                    Response.Headers[HeaderNames.Pragma] = "no-cache";

                    article = await _articleLogic.GetByUrl(id, lang);// ?? await _articleLogic.GetByUrl(id, langCookie);

                    // Article not found?
                    // try getting a version not published.

                    if (article == null)
                        // Not found.
                        return NotFound();

                    article.EditModeOn = false;
                    article.ReadWriteMode = true;

                    return View("Index_standard1", article);
                }

                //
                // Continue on for the live (read only site)
                //
                article = await _articleLogic.GetByUrl(id, lang);

                if (article == null) return NotFound();

                if (article.StatusCode == StatusCodeEnum.Redirect) return Redirect(article.Content);

                // Convert PST to GMT for both Updated and Published  =
                // Azure documentation regarding cache-control header use and CDN:
                // https://docs.microsoft.com/en-us/azure/cdn/cdn-how-caching-works#cache-directive-headers
                var expires =
                    (_redisOptions?.Value.CacheDuration ?? 60) +
                    30; // Add 30 seconds to allow REDIS to reload new prior to CDN calling.
                Response.Headers[HeaderNames.CacheControl] = $"max-age={expires}";

                // Azure CDN Standard/Premium from Verizon supports ETag by default, while
                // Azure CDN Standard from Microsoft and Azure CDN Standard from Akamai do not.
                Response.Headers[HeaderNames.ETag] = article.Updated.Ticks.ToString();

                article.EditModeOn = false;
                article.ReadWriteMode = false;

                return View("CosmosIndex", article);
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                throw;
            }
        }


        /// <summary>
        ///     Gets an article by its ID (or row key).
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [Authorize(Roles = "Reviewers,Authors,Editors,Administrators")]
        public async Task<IActionResult> Preview(int id)
        {
            try
            {
                Response.Headers[HeaderNames.CacheControl] = "no-store";
                Response.Headers[HeaderNames.Pragma] = "no-cache";

                ViewData["EditModeOn"] = false;
                var article = await _articleLogic.Get(id, EnumControllerName.Home);

                if (article != null)
                {
                    article.ReadWriteMode = false;
                    article.EditModeOn = false;

                    return View("CosmosIndex", article);
                }

                return NotFound();
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                throw;
            }
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            Response.Headers[HeaderNames.CacheControl] = "no-store";
            Response.Headers[HeaderNames.Pragma] = "no-cache";
            ViewData["EditModeOn"] = false;
            return View(new ErrorViewModel {RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier});
        }

        /// <summary>
        /// Gets a list of languages supported for translation
        /// </summary>
        /// <param name="lang">language code</param>
        /// <returns></returns>
        public async Task<JsonResult> GetSupportedLanguages(string lang = "en-US")
        {
            var result = await _articleLogic.GetSupportedLanguages("en-US");
            return Json(result.Languages.Select(s => new LangItemViewModel
            {
                DisplayName = s.DisplayName,
                LanguageCode = s.LanguageCode
            }).ToList());
        }

        #region STATIC WEB PAGES

        [Authorize]
        public IActionResult AccessPending()
        {
            var model = new ArticleViewModel
            {
                Id = 0,
                ArticleNumber = 0,
                UrlPath = null,
                VersionNumber = 0,
                Published = null,
                Title = "Access Pending",
                Content = null,
                Updated = default,
                HeaderJavaScript = null,
                FooterJavaScript = null,
                Layout = null,
                ReadWriteMode = false,
                PreviewMode = false,
                EditModeOn = false
            };
            return View(model);
        }

        #endregion
    }
}