using System.Collections.Generic;

namespace CDT.Cosmos.Cms.Common.Services
{
    /// <summary>
    /// Simple proxy service config
    /// </summary>
    public class SimpleProxyConfigs
    {
        /// <summary>
        /// Array of configurations
        /// </summary>
        public ProxyConfig[] Configs { get; set; }
    }

    /// <summary>
    /// Proxy configuration
    /// </summary>
    public class ProxyConfig
    {
        /// <summary>
        /// Name of the connection
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// Method (i.e. GET or POST)
        /// </summary>
        public string Method { get; set; }
        /// <summary>
        /// URL End Point
        /// </summary>
        public string UriEndpoint { get; set; }
        /// <summary>
        /// GET string or POST data
        /// </summary>
        public string Data { get; set; }
        /// <summary>
        /// User name to use when accessing end point.
        /// </summary>
        public string UserName { get; set; } = "";
        /// <summary>
        /// Password to use when accessing end point.
        /// </summary>
        public string Password { get; set; } = "";
        /// <summary>
        /// Content type
        /// </summary>
        public string ContentType { get; set; } = "application/x-www-form-urlencoded";
        /// <summary>
        /// RBAC roles allowed to use end point
        /// </summary>
        /// <remarks>
        /// <para>Anonymous role enables anyone to use end point. Authenticated role allows any authenticated user access. Otherwise the specifc roles who have access are listed here.</para>
        /// </remarks>
        public string[] Roles { get; set; }
    }
}
