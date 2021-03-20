using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace CDT.Cosmos.Cms.Common.Services
{
    /// <summary>
    /// This is a generic class that makes server to server calls
    /// </summary>
    public class SimpleProxyService
    {
        /// <summary>
        /// Optsions field
        /// </summary>
        private readonly IOptions<SimpleProxyConfigs> config;

        public SimpleProxyService(IOptions<SimpleProxyConfigs> config)
        {
            this.config = config;
        }

        /// <summary>
        /// Calls and endpoint and returns the results as a string.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="user"></param>
        /// <param name="proxyData"></param>
        /// <returns></returns>
        public async Task<string> CallEndpoint(string name, UserIdentityInfo user, string proxyData = "")
        {
            var endpointConfig = config.Value.Configs.FirstOrDefault(c => c.Name.Equals(name, StringComparison.CurrentCultureIgnoreCase));

            if (endpointConfig == null)
                throw new Exception("Endpoint not found.");

            if (string.IsNullOrEmpty(endpointConfig.Data))
            {
                endpointConfig.Data = proxyData;
            }

            if (endpointConfig.Roles.Contains("Anonymous")
                ||
                user != null && endpointConfig.Roles.Contains("Authenticated") &&  user.IsAuthenticated
                ||
                user != null && endpointConfig.Roles.Any(a => user.IsInRole(a))
                )
            {
                return await CallEndpoint(
                new Uri(endpointConfig.UriEndpoint),
                endpointConfig.Method,
                endpointConfig.Data,
                endpointConfig.UserName,
                endpointConfig.Password,
                endpointConfig.ContentType);
            }

            return "Permission denied.";
        }

        /// <summary>
        /// Use this private method to call and end point
        /// </summary>
        /// <returns></returns>
        private async Task<string> CallEndpoint(Uri endPoint, string method, string proxyData, string userName, string password, string contentType = "application/x-www-form-urlencoded")
        {
            WebRequest request;
            if (method.Equals("get", StringComparison.CurrentCultureIgnoreCase))
            {
                request = WebRequest.Create(endPoint + proxyData);
            }
            else
            {
                request = WebRequest.Create(endPoint);
            }
            request.Method = method;
            request.ContentType = contentType;

            if (!string.IsNullOrEmpty(userName) && !string.IsNullOrEmpty(password))
            {
                request.Credentials = new NetworkCredential(userName, password);
            }

            if (method.Equals("post", StringComparison.CurrentCultureIgnoreCase))
            {
                ASCIIEncoding ascii = new ASCIIEncoding();
                byte[] data = ascii.GetBytes(proxyData);
                request.ContentLength = data.Length;

                using (var stream = await request.GetRequestStreamAsync())
                {
                    stream.Write(data, 0, data.Length);
                }
            }

            using var response = (HttpWebResponse) await request.GetResponseAsync();
            
            return new StreamReader(response.GetResponseStream()).ReadToEnd();
        }
    }
}
