using CDT.Cosmos.Cms.Common.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace CDT.Cosmos.Cms.Services
{
    public class CosmosResponseCacheAttribute : ResponseCacheAttribute
    {
        private readonly IOptions<SiteCustomizationsConfig> _siteOptions;

        public CosmosResponseCacheAttribute(IOptions<SiteCustomizationsConfig> siteOptions,
            IOptions<RedisContextConfig> redisConfig)
        {
            _siteOptions = siteOptions;
            if (siteOptions.Value.ReadWriteMode)
            {
                Location = ResponseCacheLocation.None;
            }
            else
            {
                Location = ResponseCacheLocation.Any;
                Duration = redisConfig.Value.CacheDuration;
                VaryByQueryKeys = new[] {"*"};
            }
        }
    }
}