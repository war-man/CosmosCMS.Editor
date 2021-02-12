
using Microsoft.Extensions.Options;

namespace CDT.Cosmos.Cms.Services
{
    public class AkamaiService
    {
        private readonly AkamaiCdnClient _client;
        private readonly IOptions<AkamaiContextConfig> _options;

        public AkamaiService(IOptions<AkamaiContextConfig> options)
        {
            _options = options;
            _client = new AkamaiCdnClient(_options.Value.ClientToken, _options.Value.AccessToken, _options.Value.Secret,
                _options.Value.AkamaiHost);
        }

        public string PurgeCdnByCpCode()
        {
            var purgeObjects = new AkamaiPurgeObjects {Objects = new[] {_options.Value.CpCode}};
            return _client.PurgeProduction(purgeObjects,
                PurgeEndPoints.CpCodeProductionEndpoint);
        }
    }
}