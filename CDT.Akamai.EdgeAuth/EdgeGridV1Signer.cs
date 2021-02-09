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
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;

namespace CDT.Akamai.EdgeAuth
{
    /// <summary>
    ///     The EdgeGrid Signer is responsible for brokering a requests.This class is responsible
    ///     for the core interaction logic given an API command and the associated set of parameters.
    ///     When event is executed, the 'Authorization' header is decorated
    ///     If connection is going to be reused, pass the persistent HttpWebRequest object when calling execute()
    ///     TODO: support rebinding on IO communication errors (eg: connection reset)
    ///     TODO: support Async callbacks and Async IO
    ///     TODO: support multiplexing
    ///     TODO: optimize and adapt throughput based on connection latency
    ///     Author: colinb@akamai.com  (Colin Bendell)
    /// </summary>
    public class EdgeGridV1Signer : IRequestSigner
    {
        public const string AuthorizationHeader = "Authorization";

        public EdgeGridV1Signer(IList<string> headers = null, long? maxBodyHashSize = 2048)
        {
            HeadersToInclude = headers ?? new List<string>();
            MaxBodyHashSize = maxBodyHashSize;
            SignVersion = SignType.HMACSHA256;
            HashVersion = HashType.Sha256;
        }

        /// <summary>
        ///     The SignVersion enum value
        /// </summary>
        public SignType SignVersion { get; }

        /// <summary>
        ///     The checksum mechanism to hash the request body
        /// </summary>
        public HashType HashVersion { get; }

        /// <summary>
        ///     The ordered list of header names to include in the signature.
        /// </summary>
        public IList<string> HeadersToInclude { get; }

        /// <summary>
        ///     The maximum body size used for computing the POST body hash (in bytes).
        /// </summary>
        public long? MaxBodyHashSize { get; }

        /// <summary>
        ///     Signs the given request with the given client credential.
        /// </summary>
        /// <param name="request">The web request to sign</param>
        /// <param name="credential">the credential used in the signing</param>
        /// <param name="uploadStream"></param>
        /// <returns>the signed request</returns>
        public WebRequest Sign(WebRequest request, ClientCredential credential, Stream uploadStream = null)
        {
            var timestamp = DateTime.UtcNow;

            //already signed?
            if (request.Headers.Get(AuthorizationHeader) != null)
                request.Headers.Remove(AuthorizationHeader);

            var requestData = GetRequestData(request.Method, request.RequestUri, request.Headers, uploadStream);
            var authData = GetAuthDataValue(credential, timestamp);
            var authHeader = GetAuthorizationHeaderValue(credential, timestamp, authData, requestData);
            request.Headers.Add(AuthorizationHeader, authHeader);

            return request;
        }

        /// <summary>
        ///     Opens the connection to the {OPEN} API, assembles the signing headers and uploads any files.
        /// </summary>
        /// <param name="request">the </param>
        /// <param name="credential"></param>
        /// <param name="uploadStream"></param>
        /// <returns> the output stream of the response</returns>
        public Stream Execute(WebRequest request, ClientCredential credential, Stream uploadStream = null)
        {
            //Make sure that this connection will behave nicely with multiple calls in a connection pool.
            ServicePointManager.EnableDnsRoundRobin = true;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            request = Sign(request, credential, uploadStream);

            if (request.Method == "PUT" || request.Method == "POST" || request.Method == "PATCH")
            {
                //Disable the nastiness of Expect100Continue
                ServicePointManager.Expect100Continue = false;
                if (uploadStream == null)
                    request.ContentLength = 0;
                else if (uploadStream.CanSeek)
                    request.ContentLength = uploadStream.Length;
                else if (request is HttpWebRequest webRequest)
                    webRequest.SendChunked = true;

                if (uploadStream != null)
                {
                    // avoid internal memory allocation before buffering the output
                    if (request is HttpWebRequest webRequest) webRequest.AllowWriteStreamBuffering = false;

                    if (string.IsNullOrEmpty(request.ContentType))
                        request.ContentType = "application/json";

                    using var requestStream = request.GetRequestStream();
                    using (uploadStream)
                    {
                        uploadStream.CopyTo(requestStream, 1024 * 1024);
                    }
                }
            }

            if (request is HttpWebRequest httpRequest)
            {
                httpRequest.Accept = "*/*";
                if (string.IsNullOrEmpty(httpRequest.UserAgent))
                    httpRequest.UserAgent = "EdgeGrid.Net/v1";
            }

            WebResponse response;
            try
            {
                response = request.GetResponse();
            }
            catch (WebException e)
            {
                // non 200 OK responses throw exceptions.
                // is this because of Time drift? can we re-try?
                using (response = e.Response)
                {
                    Validate(response);
                }
            }

            return response.GetResponseStream();
        }

        public string GetAuthDataValue(ClientCredential credential, DateTime? timestamp)
        {
            if (timestamp == null)
                throw new ArgumentNullException(nameof(timestamp));

            var nonce = Guid.NewGuid();
            return
                $"{SignVersion.Name} client_token={credential.ClientToken};access_token={credential.AccessToken};timestamp={timestamp.Value.ToISO8601()};nonce={nonce.ToString().ToLower()};";
        }

        public string GetRequestData(string method, Uri uri, NameValueCollection requestHeaders = null,
            Stream requestStream = null)
        {
            if (string.IsNullOrEmpty(method))
                throw new ArgumentNullException(nameof(method));

            var headers = GetRequestHeaders(requestHeaders);
            var bodyHash = "";
            // Only POST body is hashed
            if (method == "POST")
                bodyHash = GetRequestStreamHash(requestStream);

            return $"{method.ToUpper()}\t{uri.Scheme}\t{uri.Host}\t{uri.PathAndQuery}\t{headers}\t{bodyHash}\t";
        }

        public string GetRequestHeaders(NameValueCollection requestHeaders)
        {
            if (requestHeaders == null) return string.Empty;

            var headers = new StringBuilder();
            foreach (var name in HeadersToInclude)
            {
                //TODO: should auto detect headers and remove standard non-http headers
                var value = requestHeaders.Get(name);
                if (!string.IsNullOrEmpty(value))
                    headers.AppendFormat("{0}:{1}\t", name,
                        Regex.Replace(value.Trim(), "\\s+", " ", RegexOptions.Compiled));
            }

            return headers.ToString();
        }

        public string GetRequestStreamHash(Stream requestStream)
        {
            if (requestStream == null) return string.Empty;

            if (!requestStream.CanRead)
                throw new IOException("Cannot read stream to compute hash");

            if (!requestStream.CanSeek)
                throw new IOException("Stream must be seekable!");

            var streamHash = requestStream.ComputeHash(HashVersion.Checksum, MaxBodyHashSize).ToBase64();
            requestStream.Seek(0, SeekOrigin.Begin);
            return streamHash;
        }

        internal string GetAuthorizationHeaderValue(ClientCredential credential, DateTime timestamp, string authData,
            string requestData)
        {
            var signingKey = timestamp.ToISO8601().ToByteArray()
                .ComputeKeyedHash(credential.Secret, SignVersion.Algorithm).ToBase64();
            var authSignature = $"{requestData}{authData}".ToByteArray()
                .ComputeKeyedHash(signingKey, SignVersion.Algorithm).ToBase64();
            return $"{authData}signature={authSignature}";
        }

        /// <summary>
        ///     Validates the response and attempts to detect root causes for failures for non 200 responses. The most common cause
        ///     is
        ///     due to time synchronization of the local server. If the local server is more than 30seconds out of sync then the
        ///     API server will reject the request.
        ///     TODO: catch rate limitting errors. Should delay and retry.
        /// </summary>
        /// <param name="response">the active response object</param>
        public void Validate(WebResponse response)
        {
            if (response is HttpWebResponse httpResponse)
            {
                if (httpResponse.StatusCode == HttpStatusCode.OK)
                    return;

                var date = httpResponse.Headers.Get("Date");
                if (date != null
                    && DateTime.TryParse(date, out var responseDate))
                    if (DateTime.Now.Subtract(responseDate).TotalSeconds > 30)
                        throw new HttpRequestException(
                            "Local server Date is more than 30s out of sync with Remote server");

                using var reader =
                    new StreamReader(httpResponse.GetResponseStream() ?? throw new InvalidOperationException(),
                        Encoding.UTF8);
                var responseBody = reader.ReadToEnd();
                // Do something with the value

                throw new HttpRequestException(
                    $"Unexpected Response from Server: {httpResponse.StatusCode} {httpResponse.StatusDescription}\n{httpResponse.Headers}\n\n{responseBody}");
            }
        }

        public class SignType
        {
            public static SignType HMACSHA256 = new SignType("EG1-HMAC-SHA256", KeyedHashAlgorithm.HMACSHA256);

            private SignType(string name, KeyedHashAlgorithm algorithm)
            {
                Name = name;
                Algorithm = algorithm;
            }

            public string Name { get; }
            public KeyedHashAlgorithm Algorithm { get; }
        }

        public class HashType
        {
            public static HashType Sha256 = new HashType(ChecksumAlgorithm.SHA256);

            private HashType(ChecksumAlgorithm checksum)
            {
                Checksum = checksum;
            }

            public ChecksumAlgorithm Checksum { get; }
        }
    }
}