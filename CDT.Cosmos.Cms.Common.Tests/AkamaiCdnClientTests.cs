using System;
using CDT.Cosmos.Cms.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CDT.Cosmos.Cms.Common.Tests
{
    [TestClass]
    public class AkamaiCdnClientTests
    {
        //private const string AccessToken =;
        //private const string ClientToken = ;
        //private const string Secret = ;
        //private const string AkamaiHost = ;

        //[TestMethod]
        //public void Authenticate_Success()
        //{
        //    var client = new Client(ClientToken, AccessToken, Secret, AkamaiHost);

        //    //var request = client.CreateWebRequest("/ccu/v3/invalidate/cpcode/production");
        //    //request.Method = "POST";
        //    var request = client.CreateWebRequest("/diagnostic-tools/v2/ghost-locations/available", "GET");
        //    Assert.IsNotNull(request);

        //    try
        //    {
        //        //using (var streamWriter = new StreamWriter(request.GetRequestStream()))
        //        //{
        //        //    streamWriter.Write("{\"objects\": [ 1067789 ]}");
        //        //}
        //        var response = request.GetResponse();
        //        Assert.IsNotNull(response);
        //        var encoding = ASCIIEncoding.ASCII;
        //        using (var reader = new System.IO.StreamReader(response.GetResponseStream(), encoding))
        //        {
        //            string responseText = reader.ReadToEnd();
        //            Assert.IsNotNull(responseText);
        //        }
        //    }
        //    catch (Exception e)
        //    {
        //        Console.WriteLine(e);
        //        throw;
        //    }

        //}

        private static IConfigurationSection _config;


        public static string GetConfigValue(string keyName)
        {
            if (_config == null) _config = StaticUtilities.GetConfig().GetSection("Akamai");
            var value = _config[keyName];

            return value;
        }


        [TestMethod]
        public void Invalidate_CPCode_Success()
        {
            var accessToken = GetConfigValue("AccessToken");

            var clientToken = GetConfigValue("ClientToken");
            var secret = GetConfigValue("Secret");
            var akamaiHost = GetConfigValue("AkamaiHost");

            var client = new AkamaiCdnClient(clientToken, accessToken, secret, akamaiHost);

            //var cpcodev2 = new PurgeCpCodes() { Objects = new[] { "959654" } };
            //string json = "{ \"objects\": [ 959654 ] }";
            try
            {
                var result = client.PurgeProduction(new AkamaiPurgeObjects { Objects = new[] { "1118561" } },
                    PurgeEndPoints.CpCodeProductionEndpoint);
                Assert.IsTrue(result.Contains("Request accepted"));
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        [TestMethod]
        public void Invalidate_Url_Success()
        {
            var accessToken = GetConfigValue("AccessToken");
            var clientToken = GetConfigValue("ClientToken");
            var secret = GetConfigValue("Secret");
            var akamaiHost = GetConfigValue("AkamaiHost");

            var client = new AkamaiCdnClient(clientToken, accessToken, secret, akamaiHost);

            //var cpcodev2 = new PurgeCpCodes() { Objects = new[] { "959654" } };
            //string json = "{ \"objects\": [ 959654 ] }";
            try
            {
                var result =
                    client.PurgeProduction(
                        new AkamaiPurgeObjects { Objects = new[] { "https://akamaipremium.dev.technology.ca.gov/" } },
                        PurgeEndPoints.UrlProductionEndpoint);
                Assert.IsTrue(result.Contains("Request accepted"));
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        //[TestMethod]
        //public void Invalidate_MultipleUrls_Success()
        //{

        //    var client = new Client(ClientToken, AccessToken, Secret, AkamaiHost);

        //    try
        //    {
        //        var result = client.PurgeProduction(new PurgeObjects() { Objects = new[] { "https://response.ca.gov/psps.html", "https://response.ca.gov/resources.html" } }, PurgeEndPoints.UrlProductionEndpoint);
        //        Assert.IsTrue(result.Contains("Request accepted"));
        //    }
        //    catch (Exception e)
        //    {
        //        Console.WriteLine(e);
        //        throw;
        //    }
        //}
    }
}
