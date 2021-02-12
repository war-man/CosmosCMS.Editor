namespace CDT.Cosmos.Cms.Services
{
    /// <summary>
    /// Configuration for Azure CDN
    /// </summary>
    public class AzureCdnConfig
    {
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
        public string TenantId { get; set; }
        public string TenantDomainName { get; set; }
        public string CdnProfileName { get; set; }
        public string EndPointName { get; set; }
        public string CdnProvider { get; set; }
        public string ResourceGroup { get; set; }
        public string SubscriptionId { get; set; }
    }
}
