using CDT.Cosmos.Cms.Common.Services;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CDT.Cosmos.Cms.Common.Tests
{
    [TestClass]
    public class A04SimpleProxyTests
    {
        private static SimpleProxyConfigs configs;

        [ClassInitialize]
        public static void Initialize(TestContext context)
        {
            configs = new SimpleProxyConfigs()
            {
                Configs = new ProxyConfig[]
                 {
                     new ProxyConfig()
                        {
                            ContentType = "text/html; charset=UTF-8",
                            Data = "",
                            Method = "GET",
                            Name = "GoogleAnonymous",
                            Password = "",
                            UriEndpoint = "https://www.google.com",
                            UserName = "",
                            Roles = new string[] { "Anonymous" }
                        },
                     new ProxyConfig()
                        {
                            ContentType = "text/html; charset=UTF-8",
                            Data = "95742",
                            Method = "GET",
                            Name = "zippopotam",
                            Password = "",
                            UriEndpoint = "https://api.zippopotam.us/us/",
                            UserName = "",
                            Roles = new string[] { "Anonymous" }
                        },
                     new ProxyConfig()
                        {
                            ContentType = "text/html; charset=UTF-8",
                            Data = "",
                            Method = "GET",
                            Name = "ZippopotamAddData",
                            Password = "",
                            UriEndpoint = "https://api.zippopotam.us/us/",
                            UserName = "",
                            Roles = new string[] { "Anonymous" }
                        },
                     new ProxyConfig()
                        {
                            ContentType = "text/html; charset=UTF-8",
                            Data = "",
                            Method = "GET",
                            Name = "ZippopotamAdminRoleAddData",
                            Password = "",
                            UriEndpoint = "https://api.zippopotam.us/us/",
                            UserName = "",
                            Roles = new string[] { "Administrators" }
                        }
                 }
            };
        }

        [TestMethod]
        public async Task A01_Get_Google_Anonymous()
        {
            // Arrange
            var proxy = new SimpleProxyService(Options.Create(configs));

            // Act
            var result = await proxy.CallEndpoint("googleanonymous", null);

            // Assert
            Assert.AreNotEqual("Permission denied.", result);
            Assert.IsNotNull(result);
            Assert.IsTrue(result.StartsWith("<!doctype html>"));
        }

        [TestMethod]
        public async Task A02_Get_Zippopotam_AddData()
        {
            // Arrange
            var proxy = new SimpleProxyService(Options.Create(configs));

            // Act
            var result = await proxy.CallEndpoint("ZippopotamAddData", null, "/93950");

            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue(result.Length > 9);
        }

        [TestMethod]
        public async Task A02_Zippopotam_Anonymous()
        {
            // Arrange
            var proxy = new SimpleProxyService(Options.Create(configs));

            // Act
            var result = await proxy.CallEndpoint("Zippopotam", null);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreNotEqual("Permission denied.", result);
            Assert.IsTrue(result.Length > 9);
        }

        [TestMethod]
        public async Task A03_Post_Authenticated_ZippopotamAdminRole_FailAuthentication()
        {
            // Arrange
            var proxy = new SimpleProxyService(Options.Create(configs));

            // Act
            var result = await proxy.CallEndpoint("ZippopotamAdminRoleAddData", null, "/95842");

            // Assert
            Assert.AreEqual("Permission denied.", result);
        }

        [TestMethod]
        public async Task A04_Post_Authenticated_ZippopotamAdminRole_SucceedAuthentication()
        {
            // Arrange
            var proxy = new SimpleProxyService(Options.Create(configs));
            var user = await StaticUtilities.GetPrincipal(TestUsers.Foo);

            // Act
            var result = await proxy.CallEndpoint("ZippopotamAdminRoleAddData", new UserIdentityInfo(user), "/95842");

            // Assert
            Assert.AreNotEqual("Permission denied.", result);
            Assert.IsNotNull(result);
            Assert.IsTrue(result.Length > 9);
        }

    }
}
