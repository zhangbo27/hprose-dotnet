/**********************************************************\
|                                                          |
|                          hprose                          |
|                                                          |
| Official WebSite: http://www.hprose.com/                 |
|                   http://www.hprose.org/                 |
|                                                          |
\**********************************************************/
/**********************************************************\
 *                                                        *
 * HproseHttpClient.cs                                    *
 *                                                        *
 * hprose http client class for C#.                       *
 *                                                        *
 * LastModified: Jan 23, 2016                             *
 * Author: Ma Bingyao <andot@hprose.com>                  *
 *                                                        *
\**********************************************************/
using System;
using System.Collections;
#if !(dotNET10 || dotNET11 || dotNETCF10 || dotNETMF)
using System.Collections.Generic;
#endif
using System.IO;
#if !(SILVERLIGHT || WINDOWS_PHONE || Core || PORTABLE)
using System.IO.Compression;
#elif WINDOWS_PHONE
using System.Net.Browser;
#endif
using System.Net;
using Hprose.IO;
using Hprose.Common;
#if !(dotNET10 || dotNET11 || dotNETCF10 || dotNETCF20 || SILVERLIGHT || WINDOWS_PHONE || Core || PORTABLE)
using System.Security.Cryptography.X509Certificates;
#endif
using System.Threading;

namespace Hprose.Client {
    public class HproseHttpClient : HproseClient {
#if !SILVERLIGHT
        private static bool disableGlobalCookie = false;
        public bool DisableGlobalCookie {
            get {
                return disableGlobalCookie;
            }
            set {
                disableGlobalCookie = value;
            }
        }
#endif
#if (PocketPC || Smartphone || WindowsCE || dotNETMF)
        private static CookieManager globalCookieManager = new CookieManager();
        private CookieManager cookieManager = disableGlobalCookie ? new CookieManager() : globalCookieManager;
#elif !SILVERLIGHT
        private static CookieContainer globalCookieContainer = new CookieContainer();
        private CookieContainer cookieContainer = disableGlobalCookie ? new CookieContainer() : globalCookieContainer;
#endif

#if !dotNETMF
        private class AsyncContext {
            internal HttpWebRequest request;
            internal HttpWebResponse response = null;
            internal MemoryStream data;
            internal AsyncCallback callback;
            internal Exception e = null;
            internal Timer timer;
            internal AsyncContext(HttpWebRequest request) {
                this.request = request;
            }
        }
#endif

#if !(dotNET10 || dotNET11 || dotNETCF10 || dotNETMF)
        private Dictionary<string, string> headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
#elif MONO
        private Hashtable headers = new Hashtable(StringComparer.OrdinalIgnoreCase);
#elif dotNETMF
        private WebHeaderCollection headers = new WebHeaderCollection();
#else
        private Hashtable headers = new Hashtable(new CaseInsensitiveHashCodeProvider(), new CaseInsensitiveComparer());
#endif
		private int timeout = 30000;
#if dotNETMF
        private NetworkCredential credentials = null;
#elif !(SILVERLIGHT || WINDOWS_PHONE || PORTABLE)
        private ICredentials credentials = null;
#endif
#if !(SILVERLIGHT || WINDOWS_PHONE || PORTABLE || Core)
        private bool keepAlive = true;
        private int keepAliveTimeout = 300;
        private IWebProxy proxy = null;
        private string encoding = null;
#if !dotNETCF10
        private string connectionGroupName = null;
#if dotNETMF
        private X509Certificate[] clientCertificates = null;
#elif !(dotNET10 || dotNET11 || dotNETCF20 || UNITY_WEBPLAYER)
        private X509CertificateCollection clientCertificates = null;
#endif
#endif
#endif

        public HproseHttpClient()
            : base() {
        }

        public HproseHttpClient(string uri)
            : base(uri) {
        }

#if !dotNETMF
        public HproseHttpClient(HproseMode mode)
            : base(mode) {
        }

        public HproseHttpClient(string uri, HproseMode mode)
            : base(uri, mode) {
        }
#endif

#if !dotNETMF
        public static new HproseClient Create(string uri, HproseMode mode) {
            Uri u = new Uri(uri);
            if (u.Scheme != "http" &&
                u.Scheme != "https") {
                throw new HproseException("This client doesn't support " + u.Scheme + " scheme.");
            }
            return new HproseHttpClient(uri, mode);
        }
#else
        public static new HproseClient Create(string uri) {
            Uri u = new Uri(uri);
            if (u.Scheme != "http" &&
                u.Scheme != "https") {
                throw new HproseException("This client doesn't support " + u.Scheme + " scheme.");
            }
            return new HproseHttpClient(uri);
        }
#endif

        public void SetHeader(string name, string value) {
            string nl = name.ToLower();
            if (nl != "content-type" &&
                nl != "content-length" &&
                nl != "host") {
                if (value == null) {
                    headers.Remove(name);
                }
                else {
#if dotNETMF
                    headers.Set(name, value);
#else
                    headers[name] = value;
#endif
                }
            }
        }

        public string GetHeader(string name) {
#if (dotNET10 || dotNET11 || dotNETCF10 || dotNETMF)
            return (string)headers[name];
#else
            return headers[name];
#endif
        }

        public int Timeout {
            get {
                return timeout;
            }
            set {
                timeout = value;
            }
        }

#if dotNETMF
        [CLSCompliantAttribute(false)]
        public NetworkCredential Credentials {
            get {
                return credentials;
            }
            set {
                credentials = value;
            }
        }
#elif !(SILVERLIGHT || WINDOWS_PHONE || PORTABLE)
        public ICredentials Credentials {
            get {
                return credentials;
            }
            set {
                credentials = value;
            }
        }
#endif

#if !(SILVERLIGHT || WINDOWS_PHONE || PORTABLE || Core)
        public bool KeepAlive {
            get {
                return keepAlive;
            }
            set {
                keepAlive = value;
            }
        }

        public int KeepAliveTimeout {
            get {
                return keepAliveTimeout;
            }
            set {
                keepAliveTimeout = value;
            }
        }

#if dotNETMF
        [CLSCompliantAttribute(false)]
#endif
        public IWebProxy Proxy {
            get {
                return proxy;
            }
            set {
                proxy = value;
            }
        }

        public string AcceptEncoding {
            get {
                return encoding;
            }
            set {
                encoding = value;
            }
        }

#if !dotNETCF10
        public string ConnectionGroupName {
            get {
                return connectionGroupName;
            }
            set {
                connectionGroupName = value;
            }
        }

#if dotNETMF
        [CLSCompliantAttribute(false)]
        public X509Certificate[] ClientCertificates {
            get {
                return clientCertificates;
            }
            set {
                clientCertificates = value;
            }
        }
#elif !(dotNET10 || dotNET11 || dotNETCF20 || UNITY_WEBPLAYER)
        public X509CertificateCollection ClientCertificates {
            get {
                return clientCertificates;
            }
            set {
                clientCertificates = value;
            }
        }
#endif
#endif
#endif

        private HttpWebRequest GetRequest() {
            Uri uri = new Uri(this.uri);
#if !WINDOWS_PHONE
            HttpWebRequest request = WebRequest.Create(uri) as HttpWebRequest;
#else
            HttpWebRequest request = WebRequestCreator.ClientHttp.Create(uri) as HttpWebRequest;
#endif
            request.Method = "POST";
            request.ContentType = "application/hprose";
#if !(SILVERLIGHT || WINDOWS_PHONE || PORTABLE)
            request.Credentials = credentials;
#if !Core
#if !(PocketPC || Smartphone || WindowsCE || dotNETMF)
            request.ServicePoint.ConnectionLimit = Int32.MaxValue;
#endif
            request.Timeout = timeout;
            request.SendChunked = false;
            if (encoding != null) {
                request.Headers.Set("Accept-Encoding", encoding);
            }
#if !(dotNET10 || dotNETCF10)
            request.ReadWriteTimeout = timeout;
#endif
            request.ProtocolVersion = HttpVersion.Version11;
            if (proxy != null) {
                request.Proxy = proxy;
            }
            request.KeepAlive = keepAlive;
            if (keepAlive) {
                request.Headers.Set("Keep-Alive", KeepAliveTimeout.ToString());
            }
#if !dotNETCF10
            request.ConnectionGroupName = connectionGroupName;
#if !(dotNET10 || dotNET11 || dotNETCF20 || UNITY_WEBPLAYER)
            if (clientCertificates != null) {
#if dotNETMF
                request.HttpsAuthentCerts = clientCertificates;
#else
                request.ClientCertificates = clientCertificates;
#endif
            }
#endif
#endif
#endif
#endif
#if (dotNET10 || dotNET11 || dotNETCF10)
            foreach (DictionaryEntry header in headers) {
                request.Headers[(string)header.Key] = (string)header.Value;
            }
#elif dotNETMF
            string[] allkeys = headers.AllKeys;
            foreach (string key in allkeys) {
                request.Headers.Add(key, headers[key]);
            }
#else
            foreach (KeyValuePair<string, string> header in headers) {
                request.Headers[header.Key] = header.Value;
            }
#endif
#if (PocketPC || Smartphone || WindowsCE || dotNETMF)
            request.AllowWriteStreamBuffering = true;
#if dotNETMF
            request.Headers.Add("Cookie", cookieManager.GetCookie(uri.Host,
                                                                uri.AbsolutePath,
                                                                uri.Scheme == "https"));
#else
            request.Headers["Cookie"] = cookieManager.GetCookie(uri.Host,
                                                                uri.AbsolutePath,
                                                                uri.Scheme == "https");
#endif

#elif !SILVERLIGHT
            request.CookieContainer = cookieContainer;
#endif
            return request;
        }

        private void Send(MemoryStream data, Stream ostream) {
            data.WriteTo(ostream);
            ostream.Flush();
#if (dotNET10 || dotNET11 || dotNETCF10 || dotNETCF20)
            ostream.Close();
#else
            ostream.Dispose();
#endif
        }

        private MemoryStream Receive(HttpWebRequest request, HttpWebResponse response) {
#if (PocketPC || Smartphone || WindowsCE || dotNETMF)
            cookieManager.SetCookie(response.Headers.GetValues("Set-Cookie"), request.RequestUri.Host);
            cookieManager.SetCookie(response.Headers.GetValues("Set-Cookie2"), request.RequestUri.Host);
#endif
            Stream istream = response.GetResponseStream();
#if !(SILVERLIGHT || WINDOWS_PHONE || Core || PORTABLE)
            string contentEncoding = response.ContentEncoding.ToLower();
            if (contentEncoding.IndexOf("deflate") > -1) {
                istream = new DeflateStream(istream, CompressionMode.Decompress);
            }
            else if (contentEncoding.IndexOf("gzip") > -1) {
                istream = new GZipStream(istream, CompressionMode.Decompress);
            }
#endif
            int len = (int)response.ContentLength;
#if dotNETMF
            MemoryStream data = new MemoryStream();
#else
            MemoryStream data = (len > 0) ? new MemoryStream(len) : new MemoryStream();
#endif
            len = (len > 81920 || len < 0) ? 81920 : len;
            byte[] buffer = new byte[len];
            for (;;) {
                int size = istream.Read(buffer, 0, len);
                if (size == 0) break;
                data.Write(buffer, 0, size);
            }
#if (dotNET10 || dotNET11 || dotNETCF10 || dotNETCF20)
            istream.Close();
#else
            istream.Dispose();
#endif
#if dotNET45 || PORTABLE
            response.Dispose();
#else
            response.Close();
#endif
            return data;
        }

        // SyncInvoke
#if !(SILVERLIGHT || WINDOWS_PHONE || Core || PORTABLE)
#if dotNETMF
        [CLSCompliantAttribute(false)]
#endif
        protected override MemoryStream SendAndReceive(MemoryStream data) {
            HttpWebRequest request = GetRequest();
            Send(data, request.GetRequestStream());
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            return Receive(request, response);
        }
#endif

#if !dotNETMF
        protected void TimeoutHandler(object state) {
            AsyncContext context = (AsyncContext)state;
            try {
                if (context.response == null) {
                    if (context.request != null) {
                        context.request.Abort();
                    }
                }
                else {
#if dotNET45 || PORTABLE
                    context.response.Dispose();
#else
                    context.response.Close();
#endif
                }
                if (context.timer != null) {
                    context.timer.Dispose();
                    context.timer = null;
                }
            }
            catch (Exception) { }
        }

        // AsyncInvoke
        protected override IAsyncResult BeginSendAndReceive(MemoryStream data, AsyncCallback callback) {
            HttpWebRequest request = GetRequest();
            AsyncContext context = new AsyncContext(request);
            context.timer = new Timer(new TimerCallback(TimeoutHandler),
                                      context,
                                      timeout,
                                      -1);
            context.data = data;
            context.callback = callback;
            return request.BeginGetRequestStream(new AsyncCallback(EndSend), context);
        }

        private void EndSend(IAsyncResult asyncResult) {
            AsyncContext context = (AsyncContext)asyncResult.AsyncState;
            try {
                Send(context.data, context.request.EndGetRequestStream(asyncResult));
                context.request.BeginGetResponse(context.callback, context);
            }
            catch (Exception e) {
                context.e = e;
                context.callback(asyncResult);
            }
        }

        protected override MemoryStream EndSendAndReceive(IAsyncResult asyncResult) {
            AsyncContext context = (AsyncContext)asyncResult.AsyncState;
            try {
                if (context.e != null) {
                    throw context.e;
                }
                context.response = (HttpWebResponse)context.request.EndGetResponse(asyncResult);
                return Receive(context.request, context.response);
            }
            finally {
                if (context.timer != null) {
                    context.timer.Dispose();
                    context.timer = null;
                }
            }
        }

#endif
    }
}
