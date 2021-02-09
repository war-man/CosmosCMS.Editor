// Copyright 2014 Akamai Technologies http://developer.akamai.com.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
// Author: colinb@akamai.com  (Colin Bendell)
//

using System;
using CDT.Akamai.EdgeAuth;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CDT.Akamai.Tests
{
    [TestClass]
    public class ClientCredentialTest
    {
        private static IConfigurationSection _config;
        private static string _clientToken;
        private static string _accessToken;
        private static string _secret;

        public static string GetConfigValue(string keyName)
        {
            if (_config == null) _config = ConfigUtilities.GetConfig().GetSection("Akamai");
            var value = _config[keyName];
            if (string.IsNullOrEmpty(value))
                // If nothing found, may be a pipeline unit test.
                // Try and use an environment variable
                value = Environment.GetEnvironmentVariable(keyName);

            return value;
        }

        [TestInitialize]
        public void TestInitialize()
        {
            _accessToken = GetConfigValue("AccessToken");
            _clientToken = GetConfigValue("ClientToken");
            _secret = GetConfigValue("Secret");
        }

        [TestMethod]
        public void ConstructorDefaultTest()
        {
            Assert.IsNotNull(new ClientCredential(_clientToken, _accessToken, _secret));
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void ConstructorDefaultTest_NullClientToken()
        {
            var credential = new ClientCredential(null, _accessToken, _secret);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void ConstructorDefaultTest_NullAccessToken()
        {
            var credential = new ClientCredential(_clientToken, null, _secret);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void ConstructorDefaultTest_NullSecret()
        {
            var credential = new ClientCredential(_clientToken, _accessToken, null);
        }

        [TestMethod]
        public void GetterTest()
        {
            var credential = new ClientCredential(_clientToken, _accessToken, _secret);

            Assert.AreEqual(credential.AccessToken, _accessToken);
            Assert.AreEqual(credential.ClientToken, _clientToken);
            Assert.AreEqual(credential.Secret, _secret);
        }
    }
}