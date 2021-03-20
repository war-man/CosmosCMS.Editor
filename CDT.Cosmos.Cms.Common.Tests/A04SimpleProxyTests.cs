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
        public async Task A02_Post_Anonymous_Tableau()
        {
            // Arrange
            var proxy = new SimpleProxyService(Options.Create(configs));

            // Act
            var result = await proxy.CallEndpoint("TableauAnonymous", null);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreNotEqual("Permission denied.", result);
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
        }
        [TestMethod]
        public async Task A03_Post_Authenticated_Tableau_FailAuthentication()
        {
            // Arrange
            var proxy = new SimpleProxyService(Options.Create(configs));

            // Act
            var result = await proxy.CallEndpoint("TableauAuthenticated", null);

            // Assert
            Assert.AreEqual("Permission denied.", result);
        }

        [TestMethod]
        public async Task A04_Post_Authenticated_Tableau_SucceedAuthentication()
        {
            // Arrange
            var proxy = new SimpleProxyService(Options.Create(configs));
            var user = await StaticUtilities.GetPrincipal(TestUsers.Foo);

            // Act
            var result = await proxy.CallEndpoint("TableauAuthenticated", new UserIdentityInfo(user));

            // Assert
            Assert.AreNotEqual("Permission denied.", result);
            Assert.IsNotNull(result);
            Assert.IsTrue(result.Length > 20);
        }

        [TestMethod]
        public async Task A05_Post_Authenticated_Tableau_SucceedRole()
        {
            // Arrange
            var proxy = new SimpleProxyService(Options.Create(configs));
            var user = await StaticUtilities.GetPrincipal(TestUsers.Foo);

            // Act
            var result = await proxy.CallEndpoint("TableauAdministrators", new UserIdentityInfo(user));

            // Assert
            Assert.AreNotEqual("Permission denied.", result);
            Assert.IsNotNull(result);
            Assert.IsTrue(result.Length > 20);
        }
    }
}
