using System.Collections.Generic;

namespace CDT.Cosmos.Cms.Common.Services
{
    public class SimpleProxyConfigs
    {
        public ProxyConfig[] Configs { get; set; }
    }

    public class ProxyConfig
    {
        public string Name { get; set; }
        public string Method { get; set; }
        public string UriEndpoint { get; set; }
        public string Data { get; set; }
        public string UserName { get; set; } = "";
        public string Password { get; set; } = "";
        public string ContentType { get; set; } = "application/x-www-form-urlencoded";
        public string[] Roles { get; set; }
    }
}
