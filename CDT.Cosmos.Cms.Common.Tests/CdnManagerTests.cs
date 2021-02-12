using System.Threading.Tasks;
using CDT.Cosmos.Cms.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CDT.Cosmos.Cms.Common.Tests
{
    [TestClass]
    public class CdnManagerTests
    {
        private static AzureCdnConfig _azureCdnConfig;

        [ClassInitialize]
        public static void Initialize(TestContext context)
        {
            var config = StaticUtilities.GetConfig();
            var section = config.GetSection("AzureCdnConfig");

            _azureCdnConfig = new AzureCdnConfig
            {
                CdnProfileName = section["CdnProfileName"],
                CdnProvider = section["CdnProvider"],
                ClientId = section["ClientId"],
                ClientSecret = section["ClientSecret"],
                EndPointName = section["EndPointName"],
                ResourceGroup = section["ResourceGroup"],
                TenantDomainName = section["TenantDomainName"],
                TenantId = section["TenantId"],
                SubscriptionId = section["SubscriptionId"]
            };
        }

        [TestMethod]
        public async Task CdnAuthenticate()
        {
            var manager = new Management(_azureCdnConfig.TenantId, _azureCdnConfig.TenantDomainName,
                _azureCdnConfig.ClientId, _azureCdnConfig.ClientSecret, _azureCdnConfig.SubscriptionId,
                _azureCdnConfig.CdnProvider);
            var authenticationResult = await manager.Authenticate();
            Assert.IsNotNull(authenticationResult);
            Assert.AreEqual("Bearer", authenticationResult.AccessTokenType);
            Assert.AreEqual(CdnProvider.StandardMicrosoft, manager.CdnProvider);
        }

        // This can take a long time, so comment this out when not needing to test this.
        //[TestMethod]
        //public async Task PurgeCdn()
        //{
        //    var manager = new Management(_azureCdnConfig.TenantId, _azureCdnConfig.TenantDomainName,
        //        _azureCdnConfig.ClientId, _azureCdnConfig.ClientSecret, _azureCdnConfig.SubscriptionId,
        //        _azureCdnConfig.CdnProvider);

        //    var paths = new[] {"/", "/Cosmos_CMS_Training"};

        //    await manager.PurgeEndpoints(_azureCdnConfig.ResourceGroup, _azureCdnConfig.CdnProfileName,
        //        _azureCdnConfig.EndPointName, paths);
        //}
    }
}