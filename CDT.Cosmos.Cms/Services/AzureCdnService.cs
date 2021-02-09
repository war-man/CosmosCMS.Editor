using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CDT.Azure.CDN;
using CDT.Cosmos.Cms.Common.Data;
using CDT.Cosmos.Cms.Common.Data.Logic;
using CDT.Cosmos.Cms.Common.Services;
using CDT.Cosmos.Cms.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CDT.Cosmos.Cms.Services
{
    public class AzureCdnService
    {
        private readonly ArticleLogic _articleLogic;
        private readonly AzureCdnConfig _azureCdnConfig;
        private readonly Management _cdnManagement;
        private readonly ApplicationDbContext _dbContext;

        public AzureCdnService(IOptions<AzureCdnConfig> options, ILogger logger, ApplicationDbContext dbContext,
            IOptions<SiteCustomizationsConfig> customOptions)
        {
            _dbContext = dbContext;
            _azureCdnConfig = options.Value;
            _cdnManagement = new Management(options.Value);
            _articleLogic = new ArticleLogic(dbContext, null, customOptions.Value, logger, null);
        }

        public CdnProvider CdnProvider => _cdnManagement.CdnProvider;

        /// <summary>
        ///     Purges one or more end paths on a CDN.
        /// </summary>
        /// <param name="paths"></param>
        /// <returns></returns>
        public async Task<CdnPurgeViewModel> Purge(params string[] paths)
        {
            var result = new CdnPurgeViewModel();

            if (paths != null && paths.Any())
                // Check for global purge
                if (paths.Any(a => a.Equals("/*")))
                    // Akamai standard does not support wildcard

                    switch (_cdnManagement.CdnProvider)
                    {
                        case CdnProvider.StandardAkamai:
                        {
                            // Get only published items.
                            var query = _dbContext.Articles.Where(a => a.Published != null);

                            var urls = (await _articleLogic.GetArticleList(query)).Select(s => s.UrlPath).ToList();

                            var newPaths = new List<string>();

                            foreach (var url in urls)
                                if (url == "root")
                                    newPaths.Add("/");
                                else
                                    newPaths.Add("/" + url);

                            paths = newPaths.ToArray();
                            result = new CdnPurgeViewModel
                            {
                                PurgeId = DateTime.UtcNow.Ticks.ToString(),
                                Detail = "Azure CDN - Akamai Standard",
                                EstimatedSeconds = 20,
                                SupportId = "",
                                HttpStatus = "Accepted"
                            };
                        }
                            break;
                        case CdnProvider.PremiumVerizon:
                        {
                            result = new CdnPurgeViewModel
                            {
                                PurgeId = DateTime.UtcNow.Ticks.ToString(),
                                Detail = "Azure CDN - Verizon Premium",
                                EstimatedSeconds = 120,
                                SupportId = "",
                                HttpStatus = "Accepted"
                            };
                        }
                            break;
                        case CdnProvider.StandardMicrosoft:
                        {
                            result = new CdnPurgeViewModel
                            {
                                PurgeId = DateTime.UtcNow.Ticks.ToString(),
                                Detail = "Azure CDN - Microsoft Standard",
                                EstimatedSeconds = 600,
                                SupportId = "",
                                HttpStatus = "Accepted"
                            };
                        }
                            break;
                        case CdnProvider.StandardVerizon:
                        {
                            result = new CdnPurgeViewModel
                            {
                                PurgeId = DateTime.UtcNow.Ticks.ToString(),
                                Detail = "Azure CDN - Verizon Standard",
                                EstimatedSeconds = 600,
                                SupportId = "",
                                HttpStatus = "Accepted"
                            };
                        }
                            break;
                    }

            await  _cdnManagement.PurgeEndpoints(_azureCdnConfig.ResourceGroup, _azureCdnConfig.CdnProfileName,
                _azureCdnConfig.EndPointName, paths);

            // Run this asynchronously so we can continue.
            //task.Start();

            return result;
        }
    }
}