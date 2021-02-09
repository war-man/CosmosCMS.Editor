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
using System.IO;
using System.Net;

namespace CDT.Akamai.Tests
{
    public class WebRequestTestCreate : IWebRequestCreate
    {
        //public WebRequestTestCreate() { }

        #region IWebRequestCreate Members

        public WebRequest Create(Uri uri)
        {
            return new HttpWebRequestTest(uri);
        }

        #endregion
    }

    public class HttpWebRequestTest : WebRequest
    {
        public WebResponse NextResponse = null;

        public HttpWebRequestTest(Uri uri)
        {
            Method = "GET";
            Headers = new WebHeaderCollection();
            ContentLength = -1;
            RequestUri = uri;
        }

        public HttpStatusCode ResponseStatus { get; set; }
        public string ResponseStatusDescription { get; set; }
        public WebHeaderCollection ResponseHeaders { get; set; }
        public MemoryStream RequestStream { get; set; } = new MemoryStream();

        public sealed override string Method { get; set; }
        public sealed override WebHeaderCollection Headers { get; set; }
        public sealed override long ContentLength { get; set; }
        public override string ContentType { get; set; }

        public override Uri RequestUri { get; }

        public override Stream GetRequestStream()
        {
            return RequestStream;
        }

        public override WebResponse GetResponse()
        {
            return NextResponse;
        }

        //public HttpWebResponseTest CreateResponse(HttpStatusCode responseStatus = HttpStatusCode.OK, String statusDescription = "OK", WebHeaderCollection responseHeaders = null)
        //{
        //    SerializationInfo si = new SerializationInfo(typeof(HttpWebResponse), new System.Runtime.Serialization.FormatterConverter());
        //    //StreamingContext sc = new StreamingContext();
        //    si.AddValue("m_HttpResponseHeaders", responseHeaders ?? new WebHeaderCollection { });
        //    si.AddValue("m_Uri", this._itemUri);
        //    si.AddValue("m_Certificate", null);
        //    si.AddValue("m_Version", HttpVersion.Version11);
        //    si.AddValue("m_StatusCode", responseStatus);
        //    si.AddValue("m_ContentLength", 0);
        //    si.AddValue("m_Verb", this.Method);
        //    si.AddValue("m_StatusDescription", statusDescription);
        //    si.AddValue("m_MediaType", null);
        //    HttpWebResponseTest response = new HttpWebResponseTest();
        //    return response;
        //}
        //TODO: Enable Async support for Tests
        //public override IAsyncResult BeginGetResponse(AsyncCallback callback, object state)
        //{
        //    Task<WebResponse> f = Task<WebResponse>.Factory.StartNew(
        //        _ =>
        //        {
        //            throw GetException();
        //        },
        //        state
        //    );
        //    if (callback != null) f.ContinueWith((res) => callback(f));
        //    return f;
        //}
        //public override WebResponse EndGetResponse(IAsyncResult asyncResult)
        //{
        //    return ((Task<WebResponse>)asyncResult).Result;
        //}
    }

    //public class HttpWebResponseTest : HttpWebResponse
    //{
    //    public MemoryStream ResponseStream = new MemoryStream();
    //    public override Stream GetResponseStream()
    //    {
    //        return this.ResponseStream;
    //    }
    //}
}