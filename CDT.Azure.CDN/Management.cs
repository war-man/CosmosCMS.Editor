using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Management.Cdn;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.Rest; //using Microsoft.Azure.Management.Resources;
//using Microsoft.Azure.Management.Resources.Models;

namespace CDT.Azure.CDN
{
    public enum CdnProvider
    {
        StandardMicrosoft,
        StandardAkamai,
        StandardVerizon,
        PremiumVerizon
    }

    public class Management
    {
        private readonly string _authority;
        private readonly string _clientId;
        private readonly string _clientSecret;
        private readonly string _subscriptionId;

        /// <summary>
        ///     Constructor
        /// </summary>
        /// <param name="tenantId"></param>
        /// <param name="tenantDomainName"></param>
        /// <param name="clientId"></param>
        /// <param name="clientSecret"></param>
        /// <param name="subscriptionId"></param>
        /// <param name="cdnProvider">StandardMicrosoft | StandardAkamai | StandardVerizon | PremiumVerizon</param>
        /// <remarks>
        ///     <example>
        ///         Management
        ///     </example>
        ///     <para>
        ///         For more information please see
        ///         <a href="https://docs.microsoft.com/en-us/azure/cdn/cdn-app-dev-net">getting started</a> with the Azure CDN
        ///         Library for .NET.
        ///     </para>
        ///     <para>
        ///         The registered application (find by Client ID) needs to be given "CDN Profile Contributor" permissions.
        ///         <a
        ///             href="https://docs.microsoft.com/en-us/azure/cdn/cdn-app-dev-net#creating-the-azure-ad-application-and-applying-permissions">
        ///             Documentation
        ///         </a>
        ///         suggests assigning this IAM on the resource group.
        ///     </para>
        /// </remarks>
        public Management(string tenantId, string tenantDomainName, string clientId, string clientSecret,
            string subscriptionId,
            string cdnProvider)
        {
            _authority = $"https://login.microsoftonline.com/{tenantId}/{tenantDomainName}";
            _clientId = clientId;
            _clientSecret = clientSecret;
            _subscriptionId = subscriptionId;

            try
            {
                CdnProvider = (CdnProvider) Enum.Parse(typeof(CdnProvider), cdnProvider);
            }
            catch
            {
                throw new Exception($"CDN provider name {cdnProvider} not supported.");
            }
        }

        /// <summary>
        ///     Constructor
        /// </summary>
        /// <param name="config"></param>
        public Management(AzureCdnConfig config)
        {
            _authority = $"https://login.microsoftonline.com/{config.TenantId}/{config.TenantDomainName}";
            _clientId = config.ClientId;
            _clientSecret = config.ClientSecret;
            _subscriptionId = config.SubscriptionId;

            try
            {
                CdnProvider = (CdnProvider) Enum.Parse(typeof(CdnProvider), config.CdnProvider);
            }
            catch
            {
                throw new Exception($"CDN provider name {config.CdnProvider} not supported.");
            }
        }

        public CdnProvider CdnProvider { get; }

        /// <summary>
        ///     Authenticates with Azure
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         The registered application (find by Client ID) needs to be given "CDN Profile Contributor" permissions.
        ///         <a
        ///             href="https://docs.microsoft.com/en-us/azure/cdn/cdn-app-dev-net#creating-the-azure-ad-application-and-applying-permissions">
        ///             Documentation
        ///         </a>
        ///         suggests assigning this IAM on the resource group.  It can probably be set on the CDN profile instead.
        ///     </para>
        /// </remarks>
        /// <returns></returns>
        public async Task<AuthenticationResult> Authenticate()
        {
            var authContext = new AuthenticationContext(_authority);
            var credential = new ClientCredential(_clientId, _clientSecret);
            var result = await
                authContext.AcquireTokenAsync("https://management.core.windows.net/", credential);
            return result;
        }

        /// <summary>
        ///     Gets the CDN management client.
        /// </summary>
        /// <returns></returns>
        public async Task<CdnManagementClient> GetCdnManagementClient()
        {
            var authResult = await Authenticate();

            var cdn = new CdnManagementClient(new TokenCredentials(authResult.AccessToken))
                {SubscriptionId = _subscriptionId};
            return cdn;
        }

        /// <summary>
        ///     Purges one or more CDN paths.
        /// </summary>
        /// <param name="resourceGroupName"></param>
        /// <param name="profileName"></param>
        /// <param name="endpointName"></param>
        /// <param name="contentPaths"></param>
        /// <returns></returns>
        /// <remarks>
        ///     <para>Here are examples of how to set the parameters</para>
        ///     <list type="bullet">
        ///         <item>
        ///             resourceGroupName = "CosmosCMS"
        ///         </item>
        ///         <item>
        ///             profileName = "CosmosCmsCdn"
        ///         </item>
        ///         <item>
        ///             endpointName = Host name not including .azureedge.net
        ///         </item>
        ///         <item>
        ///         </item>
        ///         string [] { "/*" }
        ///     </list>
        ///     <para>
        ///         For more information please see
        ///         <a href="https://docs.microsoft.com/en-us/azure/cdn/cdn-app-dev-net#purge-an-endpoint">getting started</a> with
        ///         the Azure CDN Library for .NET.
        ///     </para>
        /// </remarks>
        public async Task PurgeEndpoints(string resourceGroupName, string profileName, string endpointName,
            params string[] contentPaths)
        {
            var cdn = await GetCdnManagementClient();
            await cdn.Endpoints.PurgeContentAsync(resourceGroupName, profileName, endpointName, contentPaths.ToList());
        }
    }
}