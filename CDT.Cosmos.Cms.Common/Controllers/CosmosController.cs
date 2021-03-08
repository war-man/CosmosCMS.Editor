using CDT.Cosmos.Cms.Common.Data.Logic;
using CDT.Cosmos.Cms.Common.Models;
using CDT.Cosmos.Cms.Common.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace CDT.Cosmos.Cms.Common.Controllers
{
    /// <summary>
    ///     Base class for the Cosmos CMS controller.
    /// </summary>
    public abstract class CosmosController : Controller
    {
        private readonly ArticleLogic _articleLogic;
        private readonly ILogger _logger;
        private readonly IOptions<RedisContextConfig> _redisOptions;
        private readonly IOptions<GoogleCloudAuthConfig> _gglConfig;

        /// <summary>
        ///     Base constructor
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="articleLogic"></param>
        /// <param name="redisOptions"></param>
        /// <param name="gglConfig"></param>
        protected CosmosController(ILogger logger,
            ArticleLogic articleLogic,
            IOptions<RedisContextConfig> redisOptions,
            IOptions<GoogleCloudAuthConfig> gglConfig)
        {
            _logger = logger;
            _redisOptions = redisOptions;
            _gglConfig = gglConfig;
            _articleLogic = articleLogic;
        }

        /// <summary>
        ///     Index method of the home controller, main entry point for web pages.
        /// </summary>
        /// <param name="id">URL of page</param>
        /// <param name="lang">iso language code</param>
        /// <returns></returns>
        public async Task<IActionResult> Index(string id, string lang = "en")
        {
            try
            {
                // Make sure this is UrlEncoded, because this is the way it is stored in DB.
                if (!string.IsNullOrEmpty(id))
                    id = _articleLogic.HandleUrlEncodeTitle(id);

                //
                // Continue on for the live (read only site)
                //
                var article = await _articleLogic.GetByUrl(id, lang);

                if (article == null)
                {
                    //
                    // Create your own not found page for a graceful page for users.
                    //
                    article = await _articleLogic.GetByUrl("/not_found", lang);

                    HttpContext.Response.StatusCode = 404;

                    if (article == null) return NotFound();

                }

                if (article.StatusCode == StatusCodeEnum.Redirect) return Redirect(article.Content);

                //
                // Check Role Based Access Control (RBAC)
                //
                ViewData["CCMS-RBAC"] = GetUserIdentityInfo();

                if (!string.IsNullOrEmpty(article.RoleList))
                {
                    var roles = article.RoleList.Split(',');
                    if (User.Identity == null || User.Identity.IsAuthenticated == false || roles.Any(r => User.IsInRole(r))  == false)
                    {
                        HttpContext.Response.StatusCode = 401;
                        return Redirect("~/Identity/Account/Login");
                    }
                }
                
                // Convert PST to GMT for both Updated and Published  =
                // Azure documentation regarding cache-control header use and CDN:
                // https://docs.microsoft.com/en-us/azure/cdn/cdn-how-caching-works#cache-directive-headers
                var expires =
                    (_redisOptions?.Value.CacheDuration ?? 60) +
                    30; // Add 30 seconds to allow REDIS to reload new prior to CDN calling.

                // CDNs look to the Cache-Control header.
                Response.Headers[HeaderNames.CacheControl] = $"max-age={expires}";

                // Azure CDN Standard/Premium from Verizon supports ETag by default, while
                // Azure CDN Standard from Microsoft and Azure CDN Standard from Akamai do not.
                Response.Headers[HeaderNames.ETag] = article.Updated.Ticks.ToString();

                // Akamai, Microsoft and Verizon CDN all support watching last modified for changes.
                Response.Headers[HeaderNames.LastModified] = article.Updated.ToUniversalTime().ToString("R");

                // Determine if Google Translate v3 is configured so the javascript support will be added
                ViewData["UseGoogleTranslate"] = string.IsNullOrEmpty(_gglConfig?.Value?.ClientId) == false;

                return View(article);
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                throw;
            }
        }

        private string GetUserIdentityInfo()
        {
            UserIdentityInfo identity;
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                identity = new UserIdentityInfo
                {
                    IsAuthenticated = User.Identity.IsAuthenticated,
                    RoleMembership = ((ClaimsIdentity)User.Identity).Claims
                        .Where(c => c.Type == ClaimTypes.Role)
                        .Select(c => c.Value),
                    UserName = User.Identity.Name
                };
            }
            else
            {
                identity = new UserIdentityInfo
                {
                    IsAuthenticated = false,
                    RoleMembership = new string[] { },
                    UserName = string.Empty
                };
            }

            return JsonConvert.SerializeObject(identity);
        }

        /// <summary>
        ///     Gets a list of languages supported for translation
        /// </summary>
        /// <param name="lang">language code</param>
        /// <returns></returns>
        public async Task<JsonResult> GetSupportedLanguages(string lang = "en-US")
        {
            var result = await _articleLogic.GetSupportedLanguages(lang);
            return Json(result.Languages.Select(s => new LangItemViewModel
            {
                DisplayName = s.DisplayName,
                LanguageCode = s.LanguageCode
            }).ToList());
        }
    }
}