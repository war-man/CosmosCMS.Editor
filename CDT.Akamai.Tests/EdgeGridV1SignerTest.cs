// Copyright 2014 Akamai Technologies http://developer.akamai.com.
//
// Licensed under the Apache License, KitVersion 2.0 (the "License");
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
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using CDT.Akamai.EdgeAuth;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CDT.Akamai.Tests
{
    [TestClass]
    public class EdgeGridV1SignerTest
    {
        private static IConfigurationSection _config;
        private static string _clientToken;
        private static string _accessToken;
        private static string _secret;


        public static string GetConfigValue(string keyName)
        {
            if (_config == null) _config = ConfigUtilities.GetConfig().GetSection("Akamai");
            var value = _config[keyName];

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
            var signer = new EdgeGridV1Signer();
            Assert.AreEqual(signer.SignVersion, EdgeGridV1Signer.SignType.HMACSHA256);
            Assert.AreEqual(signer.HashVersion, EdgeGridV1Signer.HashType.Sha256);
            Assert.IsNotNull(signer.HeadersToInclude);
            Assert.AreEqual(signer.HeadersToInclude.Count, 0);
            Assert.AreEqual(signer.MaxBodyHashSize, 2048);
        }

        [TestMethod]
        public void ConstructorTest()
        {
            var headers = new List<string> {"test"};
            var signer = new EdgeGridV1Signer(headers, 100);
            Assert.AreEqual(signer.HeadersToInclude.Count, 1);
            Assert.IsTrue(signer.HeadersToInclude.Contains("test"));
            Assert.AreEqual(signer.MaxBodyHashSize, 100);
        }

        [TestMethod]
        public void GetAuthDataValueTest()
        {
            var timestamp = new DateTime(1918, 11, 11, 11, 00, 00, DateTimeKind.Utc);


            var signer = new EdgeGridV1Signer();
            var credential = new ClientCredential(_clientToken, _accessToken, _secret);

            var authData = signer.GetAuthDataValue(credential, timestamp).Split(';');

            var values = new Dictionary<string, string>();

            foreach (var s in authData)
                if (!string.IsNullOrEmpty(s))
                {
                    var item = s.Split('=');
                    values.Add(item[0], item[1]);
                }

            Assert.AreEqual(_clientToken, values["EG1-HMAC-SHA256 client_token"]);
            Assert.AreEqual(_accessToken, values["access_token"]);
            Assert.AreEqual("19181111T11:00:00+0000", values["timestamp"]);
            Assert.IsTrue(Guid.TryParse(values["nonce"], out var guid));
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void GetRequestDataTest_EmptyMethod()
        {
            var signer = new EdgeGridV1Signer(new List<string> {"name1"});
            signer.GetRequestData("", new Uri("http://www.example.com/path.ext?name=value"));
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void GetRequestDataTest_NullMethod()
        {
            var signer = new EdgeGridV1Signer(new List<string> {"name1"});
            signer.GetRequestData(null, new Uri("http://www.example.com/path.ext?name=value"));
        }

        [TestMethod]
        public void GetRequestDataTest()
        {
            var signer = new EdgeGridV1Signer(new List<string> {"name1"});
            Assert.AreEqual("GET\thttp\twww.example.com\t/\t\t\t",
                signer.GetRequestData("GET", new Uri("http://www.example.com")));
            Assert.AreEqual("GET\thttp\twww.example.com\t/path.ext?name=value\t\t\t",
                signer.GetRequestData("GET", new Uri("http://www.example.com/path.ext?name=value")));

            var headers = new NameValueCollection {{"name1", "value1"}};
            Assert.AreEqual("GET\thttp\twww.example.com\t/path.ext?name=value\tname1:value1\t\t\t",
                signer.GetRequestData("GET", new Uri("http://www.example.com/path.ext?name=value"), headers));


            var data = "Lorem ipsum dolor sit amet, an sea putant quaeque, homero aperiam te eos.".ToByteArray();
            var stream = new MemoryStream(data);

            Assert.AreEqual("GET\thttp\twww.example.com\t/path.ext?name=value\tname1:value1\t\t\t",
                signer.GetRequestData("GET", new Uri("http://www.example.com/path.ext?name=value"), headers, stream));

            Assert.AreEqual(
                "POST\thttp\twww.example.com\t/path.ext?name=value\tname1:value1\t\tTors1txMl65Vww75sekbSCnvWHGxYmK0Yog4qA3AwuI=\t",
                signer.GetRequestData("POST", new Uri("http://www.example.com/path.ext?name=value"), headers, stream));
        }

        [TestMethod]
        public void GetRequestHeaders()
        {
            var signer = new EdgeGridV1Signer(new List<string> {"x-a", "x-b", "x-c"});

            Assert.AreEqual(string.Empty, signer.GetRequestHeaders(null));

            //NameValueCollection headers;
            Assert.AreEqual(string.Empty, signer.GetRequestHeaders(null));

            var headers = new NameValueCollection {{"name2", "value2"}};
            Assert.AreEqual(string.Empty, signer.GetRequestHeaders(null));

            headers = new NameValueCollection
            {
                {"x-a", "value1"},
                {"name2", "value2"}
            };
            Assert.AreEqual("x-a:value1\t", signer.GetRequestHeaders(headers));

            headers = new NameValueCollection
            {
                {"x-a", "va"},
                {"x-c", "\"     xc        \""},
                {"x-b", "   w         b"}
            };
            Assert.AreEqual("x-a:va\tx-b:w b\tx-c:\" xc \"\t", signer.GetRequestHeaders(headers));
        }

        [TestMethod]
        public void GetRequestStreamHashTest()
        {
            var signer = new EdgeGridV1Signer();
            Assert.AreEqual(string.Empty, signer.GetRequestStreamHash(null));

            var data = "Lorem ipsum dolor sit amet, an sea putant quaeque, homero aperiam te eos.".ToByteArray();
            var stream = new MemoryStream(data);
            Assert.AreEqual("Tors1txMl65Vww75sekbSCnvWHGxYmK0Yog4qA3AwuI=", signer.GetRequestStreamHash(stream));
            Assert.AreEqual(stream.Position, 0);
        }

        [TestMethod]
        public void GetRequestStreamHashMaxSizeTest()
        {
            var signer = new EdgeGridV1Signer(null, 50);
            Assert.AreEqual(string.Empty, signer.GetRequestStreamHash(null));

            var data = "Lorem ipsum dolor sit amet, an sea putant quaeque, homero aperiam te eos.".ToByteArray();
            var stream = new MemoryStream(data);
            Assert.AreEqual("IHJu55sckdViGcpD7CpUttVSzYoy/DiTQsmy7jrzoMU=", signer.GetRequestStreamHash(stream));
            Assert.AreEqual(stream.Position, 0);
        }

        [TestMethod]
        public void GetRequestStreamHashTest_NullStream()
        {
            var signer = new EdgeGridV1Signer();
            Assert.AreEqual(signer.GetRequestStreamHash(null), "");
        }

        [TestMethod]
        [ExpectedException(typeof(IOException))]
        public void GetRequestStreamHashTest_ClosedStream()
        {
            var signer = new EdgeGridV1Signer();

            var data = "Lorem ipsum dolor sit amet, an sea putant quaeque, homero aperiam te eos.".ToByteArray();
            var stream = new MemoryStream(data);
            signer = new EdgeGridV1Signer();
            stream.Close();
            signer.GetRequestStreamHash(stream);
        }

        [TestMethod]
        [ExpectedException(typeof(IOException))]
        public void GetRequestStreamHashTest_NonSeakingStream()
        {
            var signer = new EdgeGridV1Signer();

            var data = "Lorem ipsum dolor sit amet, an sea putant quaeque, homero aperiam te eos.".ToByteArray();

            var stream = new NonSeekMemoryStream(data);
            signer.GetRequestStreamHash(stream);
        }

        //[TestMethod]
        //public void GetAuthorizationHeaderValueTest()
        //{
        //    DateTime timestamp = new DateTime(1918, 11, 11, 11, 00, 00, DateTimeKind.Utc);

        //    EdgeGridV1Signer signer = new EdgeGridV1Signer();
        //    ClientCredential credential = new ClientCredential(ClientToken, AccessToken, Secret);

        //    Assert.IsNotNull(signer.GetAuthorizationHeaderValue(credential, timestamp, "auth-data", null));
        //    Assert.IsNotNull(signer.GetAuthorizationHeaderValue(credential, timestamp, "auth-data", "body-data"));

        //    //putting it all together
        //    // var authData = "Authorization: EG1-HMAC-SHA256 client_token=akaa-275ca6de04b11b91-cf46074bf3b52950;access_token=akaa-d6cfbdb2d0594ae4-ad000cf3a5473a08;timestamp=19181111T11:00:00Z;nonce=dd9957e2-4fe5-48ca-8d32-16a772ac6d8f;".Replace("Authorization: ", "").Split(';');
        //    string authBody = "GET\thttp\twww.example.com\t/\t\t\t";

        //    Dictionary<string, string> values = new Dictionary<string, string>();

        //    foreach (var s in authData)
        //    {
        //        if (!string.IsNullOrEmpty(s))
        //        {
        //            var item = s.Split('=');
        //            values.Add(item[0], item[1]);
        //        }
        //    }

        //    Assert.AreEqual(ClientToken, values["EG1-HMAC-SHA256 client_token"]);
        //    Assert.AreEqual(AccessToken, values["access_token"]);
        //    Assert.AreEqual("19181111T11:00:00+0000", values["timestamp"]);
        //    Guid guid;
        //    Assert.IsTrue(Guid.TryParse(values["nonce"], out guid));
        //    //Assert.AreEqual(string.Format("{0}signature=0iSbrT0ze1uDfJdodKOevZSSjYkXllt6VlLSghOiWtY=", authData), signer.GetAuthorizationHeaderValue(credential, timestamp, authData, authBody));
        //    //Assert.Catch<ArgumentNullException>(delegate { signer.GetAuthorizationHeaderValue(credential, null, authData, authBody); });
        //}


        [TestMethod]
        public void SignTest()
        {
            var credential = new ClientCredential(_clientToken, _accessToken, _secret);

            //DateTime timestamp = new DateTime(1918, 11, 11, 11, 00, 00, DateTimeKind.Utc);
            var signer = new EdgeGridV1Signer();

            var uri = new Uri("http://www.example.com/");

            var request = new HttpWebRequestTest(uri);
            signer.Sign(request, credential);

            Assert.AreEqual(request.Headers.Count, 1);

            var authData = request.Headers.Get("Authorization").Split(';');

            Assert.IsNotNull(request.Headers.Get("Authorization"));

            var values = new Dictionary<string, string>();

            foreach (var s in authData)
                if (!string.IsNullOrEmpty(s))
                {
                    var item = s.Split('=');
                    values.Add(item[0], item[1]);
                }

            Assert.AreEqual(_clientToken, values["EG1-HMAC-SHA256 client_token"]);
            Assert.AreEqual(_accessToken, values["access_token"]);

            //Assert.IsTrue(Regex.IsMatch(request.Headers.Get("Authorization"),
            //    @"EG1-HMAC-SHA256 client_token=clientToken;access_token=accessToken;timestamp=\d{8}T\d\d:\d\d:\d\d\+0000;nonce=[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12};signature=[A-Za-z0-9/+]{43}="));
        }

        private class NonSeekMemoryStream : MemoryStream
        {
            public NonSeekMemoryStream(byte[] data) : base(data)
            {
            }

            public override bool CanSeek => false;
        }

        //[TestMethod]
        //public void TestAPIActionValidateOK()
        //{
        //    string TestURIProtocol = "asdf";
        //    WebRequest.RegisterPrefix(TestURIProtocol, new WebRequestTestCreate());
        //    var request = (HttpWebRequestTest)WebRequest.Create("asdf://www.example.com/");


        //    var signer = new EdgeGridV1Signer();
        //    var response = request.CreateResponse();
        //    signer.Validate(response);
        //}

        //[TestMethod]
        //[ExpectedException(typeof(HttpRequestException))]
        //public void TestAPIActionValidateUnavailable()
        //{
        //    string TestURIProtocol = "asdf";
        //    WebRequest.RegisterPrefix(TestURIProtocol, new WebRequestTestCreate());
        //    var request = (HttpWebRequestTest)WebRequest.Create("asdf://www.example.com/");


        //    var signer = new EdgeGridV1Signer();
        //    var response = request.CreateResponse(HttpStatusCode.ServiceUnavailable, "Server Unavailable");

        //    var currentDate = DateTime.UtcNow;
        //    var headers = new WebHeaderCollection { { "Date", currentDate.ToString("r") } };
        //    response = request.CreateResponse(HttpStatusCode.ServiceUnavailable, "Server Unavailable", headers);
        //    signer.Validate(response);
        //}

        //[TestMethod]
        //[ExpectedException(typeof(HttpRequestException))]
        //public void TestAPIActionValidateDateDrift()
        //{
        //    string TestURIProtocol = "asdf";
        //    WebRequest.RegisterPrefix(TestURIProtocol, new WebRequestTestCreate());
        //    var request = (HttpWebRequestTest)WebRequest.Create("asdf://www.example.com/");


        //    var signer = new EdgeGridV1Signer();
        //    var response = request.CreateResponse(HttpStatusCode.ServiceUnavailable, "Server Unavailable");

        //    var currentDate = DateTime.UtcNow.AddMinutes(-2);
        //    var headers = new WebHeaderCollection { { "Date", currentDate.ToString("r") } };
        //    response = request.CreateResponse(HttpStatusCode.ServiceUnavailable, "Server Unavailable", headers);
        //    try
        //    {
        //        signer.Validate(response);
        //    }
        //    catch (Exception ex)
        //    {
        //        Assert.AreEqual(ex.Message, "Local server Date is more than 30s out of sync with Remote server");
        //        throw ex;
        //    }
        //}

        //[TestMethod]
        //public void TestAPIActionExecute()
        //{
        //    var credential = new ClientCredential(ClientToken, AccessToken, Secret);

        //    var signer = new EdgeGridV1Signer();
        //    string TestURIProtocol = "asdf";
        //    WebRequest.RegisterPrefix(TestURIProtocol, new WebRequestTestCreate());
        //    var request = (HttpWebRequestTest)WebRequest.Create("asdf://www.example.com/");

        //    var response = request.CreateResponse(HttpStatusCode.OK);
        //    request.NextResponse = response;

        //    Assert.AreSame(signer.Execute(request, credential), response.GetResponseStream());
        //    Assert.AreEqual(request.Method, "GET");
        //    Assert.AreEqual(request.Headers.Count, 1);

        //    request.Method = "PUT";
        //    signer.Execute(request, credential);
        //    Assert.AreEqual(request.ContentLength, 0);

        //    request.Method = "POST";
        //    var data = "Lorem ipsum dolor sit amet, an sea putant quaeque, homero aperiam te eos.".ToByteArray();
        //    var uploadStream = new MemoryStream(data);
        //    signer.Execute(request, credential, uploadStream);
        //    Assert.AreEqual(request.ContentLength, 73);
        //    CollectionAssert.AreEqual(request.RequestStream.ToArray(), data);
        //}
    }
}