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
        /// <summary>
        /// Article logic field.
        /// </summary>
        private readonly ArticleLogic _articleLogic;
        /// <summary>
        /// Logger field
        /// </summary>
        private readonly ILogger _logger;
        /// <summary>
        /// Redis cache configuration field
        /// </summary>
        private readonly IOptions<RedisContextConfig> _redisOptions;
        /// <summary>
        /// Google cloud configuration 
        /// </summary>
        private readonly IOptions<GoogleCloudAuthConfig> _gglConfig;
        /// <summary>
        /// Simple proxy settings
        /// </summary>
        private readonly IOptions<SimpleProxyConfigs> _proxyConfigs;

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
            IOptions<GoogleCloudAuthConfig> gglConfig,
            IOptions<SimpleProxyConfigs> proxyConfigs)
        {
            _logger = logger;
            _redisOptions = redisOptions;
            _gglConfig = gglConfig;
            _proxyConfigs = proxyConfigs;
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

        /// <summary>
        /// Returns a proxy result as a <see cref="JsonResult"/>.
        /// </summary>
        /// <param name="endpointName"></param>
        /// <returns></returns>
        public async Task<IActionResult> SimpleProxyJson(string endpointName)
        {
            if (_proxyConfigs.Value == null) return Json(string.Empty);
            var proxy = new SimpleProxyService(_proxyConfigs);
            return Json(await proxy.CallEndpoint(endpointName, new UserIdentityInfo(User)));
        }

        /// <summary>
        /// Returns a proxy as a simple string.
        /// </summary>
        /// <param name="endpointName"></param>
        /// <returns></returns>
        public async Task<string> SimpleProxy(string endpointName)
        {
            if (_proxyConfigs.Value == null) return string.Empty;
            var proxy = new SimpleProxyService(_proxyConfigs);
            return await proxy.CallEndpoint(endpointName, new UserIdentityInfo(User));
        }

        /// <summary>
        /// Gets the current user's identity information.
        /// </summary>
        /// <returns></returns>
        private string GetUserIdentityInfo()
        {
            return JsonConvert.SerializeObject(new UserIdentityInfo(User));
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