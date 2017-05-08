// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using Amib.Threading;
using osu.Framework.Extensions;
using osu.Framework.Logging;

namespace osu.Framework.IO.Network
{
    public class WebRequest : IDisposable
    {
        /// <summary>
        /// Update has progressed.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="current">Total bytes processed.</param>
        /// <param name="total">Total bytes required.</param>
        public delegate void RequestUpdateHandler(WebRequest request, long current, long total);

        /// <summary>
        /// Request has completed.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="e">An error, if an error occurred, else null.</param>
        public delegate void RequestCompleteHandler(WebRequest request, Exception e);

        /// <summary>
        /// Request has completed.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="e">An error, if an error occurred, else null.</param>
        public delegate void RequestCompleteHandler<T>(JsonWebRequest<T> request, Exception e);

        /// <summary>
        /// Request has started.
        /// </summary>
        /// <param name="request">The request.</param>
        public delegate void RequestStartedHandler(WebRequest request);

        private string url;

        /// <summary>
        /// The URL of this request.
        /// </summary>
        public string Url
        {
            get { return url; }
            set
            {
#if !DEBUG
                if (!value.StartsWith(@"https://"))
                    value = @"https://" + value.Replace(@"http://", @"");
#endif
                url = value;
            }
        }

        /// <summary>
        /// The request has been aborted by the user or due to an exception.
        /// </summary>
        public bool Aborted { get; private set; }

        /// <summary>
        /// The request has been executed and successfully completed.
        /// </summary>
        public bool Completed { get; private set; }

        /// <summary>
        /// Monitor upload progress.
        /// </summary>
        public event RequestUpdateHandler UploadProgress;

        /// <summary>
        /// Monitor download progress.
        /// </summary>
        public event RequestUpdateHandler DownloadProgress;

        /// <summary>
        /// Request has finished with success or failure. Check exception == null for success.
        /// </summary>
        public event RequestCompleteHandler Finished;

        /// <summary>
        /// Request has started.
        /// </summary>
        public event RequestStartedHandler Started;

        /// <summary>
        /// To avoid memory leaks due to delegates being bound to events, on request completion events are removed by default.
        /// This controls whether they should be kept to allow for further requests.
        /// </summary>
        public bool KeepEventsBound;

        /// <summary>
        /// POST parameters.
        /// </summary>
        public Dictionary<string, string> Parameters = new Dictionary<string, string>();

        /// <summary>
        /// FILE parameters.
        /// </summary>
        public Dictionary<string, byte[]> Files = new Dictionary<string, byte[]>();

        /// <summary>
        /// Headers.
        /// </summary>
        public Dictionary<string, string> Headers = new Dictionary<string, string>();

        public const int DEFAULT_TIMEOUT = 10000;

        public HttpMethod Method;

        /// <summary>
        /// The amount of time from last sent or received data to trigger a timeout and abort the request.
        /// </summary>
        public int Timeout = DEFAULT_TIMEOUT;

        private static readonly Logger logger;

        private static readonly SmartThreadPool thread_pool;
        private IWorkItemResult workItem;

        /// <summary>
        /// The remote IP address used for this connection.
        /// </summary>
        private string address;

        static WebRequest()
        {
            thread_pool = new SmartThreadPool(new STPStartInfo
            {
                MaxWorkerThreads = 64,
                AreThreadsBackground = true,
                IdleTimeout = 300000
            });

            //set some sane defaults for ServicePoints
            ServicePointManager.Expect100Continue = false;
            ServicePointManager.DefaultConnectionLimit = 12;
            ServicePointManager.CheckCertificateRevocationList = false;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls;

            logger = Logger.GetLogger(LoggingTarget.Network, true);
        }

        public WebRequest(string url = null, params object[] args)
        {
            if (!string.IsNullOrEmpty(url))
                Url = args.Length == 0 ? url : string.Format(url, args);
        }

        ~WebRequest()
        {
            Dispose(false);
        }

        private HttpWebRequest request;
        private HttpWebResponse response;

        private Stream internalResponseStream;

        private static readonly AddressFamily? preferred_network = AddressFamily.InterNetwork;

        /// <summary>
        /// Does a manual DNS lookup and forcefully uses IPv4 by shoving IP addresses where the host normally is.
        /// Unreliable for HTTPS+Cloudflare? Only use when necessary.
        /// </summary>
        public static bool UseExplicitIPv4Requests;

        private static bool? useFallbackPath;
        private bool didGetIPv6IP;

        protected virtual HttpWebRequest CreateWebRequest(string requestString = null)
        {
            HttpWebRequest req;

            string requestUrl = string.IsNullOrEmpty(requestString) ? Url : $@"{Url}?{requestString}";

            if (useFallbackPath != true && !UseExplicitIPv4Requests)
            {
                req = (HttpWebRequest)System.Net.WebRequest.Create(requestUrl);
                req.ServicePoint.BindIPEndPointDelegate += bindEndPoint;
            }
            else
            {
                string baseHost = requestUrl.Split('/', ':')[3];

                var addresses = Dns.GetHostAddresses(baseHost);

                address = null;

                foreach (var ip in addresses)
                {
                    bool preferred = ip.AddressFamily == preferred_network;

                    if (!preferred && address != null)
                        continue;

                    switch (ip.AddressFamily)
                    {
                        case AddressFamily.InterNetwork:
                            address = $@"{ip}";
                            break;
                        case AddressFamily.InterNetworkV6:
                            address = $@"[{ip}]";
                            break;
                    }

                    if (preferred)
                        //always use first preferred network address for now.
                        break;
                }

                req = System.Net.WebRequest.Create(requestUrl.Replace(baseHost, $"{address}:443")) as HttpWebRequest;
            }

            if (req == null)
                throw new InvalidOperationException(@"request could not be created");

            req.UserAgent = @"osu!";
            req.KeepAlive = useFallbackPath != true;
            req.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
            req.Host = requestUrl.Split('/')[2];
            req.ReadWriteTimeout = System.Threading.Timeout.Infinite;
            req.Timeout = System.Threading.Timeout.Infinite;

            return req;
        }

        private IPEndPoint bindEndPoint(ServicePoint servicePoint, IPEndPoint remoteEndPoint, int retries)
        {
            didGetIPv6IP |= remoteEndPoint.AddressFamily == AddressFamily.InterNetworkV6;
            return null;
        }

        private int responseBytesRead;

        private const int buffer_size = 32768;
        private byte[] buffer;

        private MemoryStream requestBody;

        private MemoryStream rawContent;

        public string ContentType;

        protected virtual Stream CreateOutputStream()
        {
            return new MemoryStream();
        }

        public Stream ResponseStream;

        public string ResponseString
        {
            get
            {
                try
                {
                    ResponseStream.Seek(0, SeekOrigin.Begin);
                    StreamReader r = new StreamReader(ResponseStream, Encoding.UTF8);
                    return r.ReadToEnd();
                }
                catch
                {
                    return null;
                }
            }
        }

        public byte[] ResponseData
        {
            get
            {
                try
                {
                    byte[] data = new byte[ResponseStream.Length];
                    ResponseStream.Seek(0, SeekOrigin.Begin);
                    ResponseStream.Read(data, 0, data.Length);
                    return data;
                }
                catch
                {
                    return null;
                }
            }
        }

        public WebHeaderCollection ResponseHeaders => response?.Headers;

        /// <summary>
        /// Start the request asynchronously.
        /// </summary>
        public void Perform()
        {
            if (workItem != null)
                throw new InvalidOperationException("Can not perform a web request multiple times.");

            workItem = thread_pool.QueueWorkItem(perform);
            if (thread_pool.InUseThreads == thread_pool.MaxThreads)
                logger.Add(@"WARNING: ThreadPool is saturated!", LogLevel.Error);
        }

        private void perform()
        {
            Aborted = false;
            abortRequest();

            PrePerform();

            try
            {
                reportForwardProgress();
                thread_pool.QueueWorkItem(checkTimeoutLoop);

                requestBody = new MemoryStream();

                switch (Method)
                {
                    case HttpMethod.GET:
                        Debug.Assert(Files.Count == 0);

                        StringBuilder requestParameters = new StringBuilder();
                        foreach (var p in Parameters)
                            requestParameters.Append($@"{p.Key}={p.Value}&");

                        request = CreateWebRequest(requestParameters.ToString().TrimEnd('&'));
                        request.Method = @"GET";
                        break;
                    case HttpMethod.POST:
                        request = CreateWebRequest();
                        request.Method = @"POST";

                        if (Parameters.Count + Files.Count == 0)
                        {
                            rawContent?.WriteTo(requestBody);
                            request.ContentType = ContentType;
                            requestBody.Flush();
                            break;
                        }

                        const string boundary = @"-----------------------------28947758029299";

                        request.ContentType = $@"multipart/form-data; boundary={boundary}";

                        foreach (KeyValuePair<string, string> p in Parameters)
                        {
                            requestBody.WriteLineExplicit($@"--{boundary}");
                            requestBody.WriteLineExplicit($@"Content-Disposition: form-data; name=""{p.Key}""");
                            requestBody.WriteLineExplicit();
                            requestBody.WriteLineExplicit(p.Value);
                        }

                        foreach (KeyValuePair<string, byte[]> p in Files)
                        {
                            requestBody.WriteLineExplicit($@"--{boundary}");

                            requestBody.WriteLineExplicit($@"Content-Disposition: form-data; name=""{p.Key}""; filename=""{p.Key}""");
                            requestBody.WriteLineExplicit(@"Content-Type: application/octet-stream");
                            requestBody.WriteLineExplicit();
                            requestBody.Write(p.Value, 0, p.Value.Length);
                            requestBody.WriteLineExplicit();
                        }

                        requestBody.WriteLineExplicit($@"--{boundary}--");
                        requestBody.Flush();
                        break;
                }

                request.UserAgent = @"osu!";
                request.KeepAlive = useFallbackPath != true;
                request.Host = Url.Split('/')[2];
                request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
                request.ReadWriteTimeout = System.Threading.Timeout.Infinite;
                request.Timeout = Timeout; //todo: make sure this works correctly for long-lasting transfers.

                foreach (KeyValuePair<string, string> h in Headers)
                    request.Headers.Add(h.Key, h.Value);

                ResponseStream = CreateOutputStream();

                switch (Method)
                {
                    case HttpMethod.GET:
                        //GETs are easy
                        beginResponse();
                        break;
                    case HttpMethod.POST:
                        request.ContentLength = requestBody.Length;

                        UploadProgress?.Invoke(this, 0, request.ContentLength);
                        reportForwardProgress();

                        beginRequestOutput();
                        break;
                }
            }
            catch (Exception e)
            {
                Complete(e);
            }
        }

        /// <summary>
        /// Task to run direct before performing the request.
        /// </summary>
        protected virtual void PrePerform()
        {
        }

        private void beginRequestOutput()
        {
            try
            {
                using (Stream requestStream = request.GetRequestStream())
                {
                    reportForwardProgress();
                    requestBody.Position = 0;
                    byte[] buff = new byte[buffer_size];
                    int read;
                    int totalRead = 0;
                    while ((read = requestBody.Read(buff, 0, buffer_size)) > 0)
                    {
                        reportForwardProgress();
                        requestStream.Write(buff, 0, read);
                        requestStream.Flush();

                        totalRead += read;
                        UploadProgress?.Invoke(this, totalRead, request.ContentLength);
                    }
                }

                beginResponse();
            }
            catch (Exception e)
            {
                Complete(e);
            }
        }

        private void beginResponse()
        {
            try
            {
                response = request.GetResponse() as HttpWebResponse;
                Trace.Assert(response != null);

                Started?.Invoke(this);

                internalResponseStream = response.GetResponseStream();
                Trace.Assert(internalResponseStream != null);

#if !DEBUG
                checkCertificate();
#endif

                buffer = new byte[buffer_size];

                reportForwardProgress();

                while (true)
                {
                    int read = internalResponseStream.Read(buffer, 0, buffer_size);

                    reportForwardProgress();

                    if (read > 0)
                    {
                        ResponseStream.Write(buffer, 0, read);
                        responseBytesRead += read;
                        DownloadProgress?.Invoke(this, responseBytesRead, response.ContentLength);
                    }
                    else
                    {
                        ResponseStream.Seek(0, SeekOrigin.Begin);
                        Complete();
                        break;
                    }
                }
            }
            catch (Exception e)
            {
                Complete(e);
            }
        }

        [Obfuscation(Feature = @"virtualization", Exclude = false)]
        private void checkCertificate()
        {
            if (request.ServicePoint.Certificate == null && request.RequestUri.Host != request.Address.Host && request.Address.Host.EndsWith(@".ppy.sh", StringComparison.Ordinal))
                //osu!direct downloads happen over http at the moment. we should probably move them across to https.
                return;

            //if it's null at this point we don't mind throwing an exception.
            Trace.Assert(request.ServicePoint.Certificate != null);

            //this is enough for now to assume we have a valid certificate.
            if (new X509Certificate2(request.ServicePoint.Certificate).Subject.Contains(@"CN=*.ppy.sh"))
                return;

            //else we should just destroy the response and no.
            response?.Close();
            response = null;
            throw new WebException(@"SSL failures");
        }

        public virtual void BlockingPerform()
        {
            Exception exc = null;
            bool completed = false;

            Finished += delegate(WebRequest r, Exception e)
            {
                exc = e;
                completed = true;
            };

            perform();

            while (!completed && !Aborted)
                Thread.Sleep(10);

            if (exc != null)
                throw exc;
        }

        protected virtual void Complete(Exception e = null)
        {
            if (Aborted)
                return;

            WebException we = e as WebException;
            if (we != null)
            {
                bool allowRetry = true;

                HttpStatusCode? statusCode = (we.Response as HttpWebResponse)?.StatusCode ?? HttpStatusCode.RequestTimeout;

                switch (statusCode)
                {
                    case HttpStatusCode.RequestTimeout:
                        if (hasExceededTimeout)
                        {
                            logger.Add($@"Timeout exceeded ({timeSinceLastAction} > {DEFAULT_TIMEOUT})");
                            e = new WebException($"Timeout to {Url} ({address}) after {timeSinceLastAction / 1000} seconds idle (read {responseBytesRead} bytes).", WebExceptionStatus.Timeout);
                        }
                        break;
                    case HttpStatusCode.NotFound:
                    case HttpStatusCode.MethodNotAllowed:
                    case HttpStatusCode.Forbidden:
                        allowRetry = false;
                        break;
                    case HttpStatusCode.Unauthorized:
                        allowRetry = false;
                        break;
                }

                if (allowRetry && retriesRemaining-- > 0 && responseBytesRead == 0)
                {
                    logger.Add($@"Request to {Url} ({address}) failed with {statusCode} (retrying {default_retry_count - retriesRemaining}/{default_retry_count}).");

                    //do a retry
                    perform();
                    return;
                }
                if (useFallbackPath == null && allowRetry && didGetIPv6IP)
                {
                    useFallbackPath = true;
                    logger.Add(@"---------------------- USING FALLBACK PATH! ---------------------");
                }

                logger.Add($@"Request to {Url} ({address}) failed with {statusCode} (FAILED).");
            }
            else if (e == null)
            {
                if (useFallbackPath == null)
                    useFallbackPath = false;
                logger.Add($@"Request to {Url} ({address}) successfully completed!");
            }

            response?.Close();

            Finished?.Invoke(this, e);

            if (!KeepEventsBound)
                unbindEvents();

            if (e != null)
                Aborted = true;
            else
                Completed = true;
        }

        private void abortRequest()
        {
            try
            {
                request?.Abort();
                response?.Close();
            }
            catch
            {
                //This *can* throw random exceptions internally from inside ServicePointManager.
            }
        }

        /// <summary>
        /// Forcefully abort the request.
        /// </summary>
        public void Abort()
        {
            if (Aborted) return;
            Aborted = true;

            abortRequest();

            workItem?.Cancel();
            workItem = null;

            if (!KeepEventsBound)
                unbindEvents();
        }

        /// <summary>
        /// Add a header to this request.
        /// </summary>
        public void AddHeader(string key, string val)
        {
            Headers.Add(key, val);
        }

        /// <summary>
        /// Adds a raw POST body to this request.
        /// </summary>
        public void AddRaw(string text) => AddRaw(Encoding.UTF8.GetBytes(text));

        /// <summary>
        /// Adds a raw POST body to this request.
        /// </summary>
        public void AddRaw(byte[] bytes) => AddRaw(new MemoryStream(bytes));

        /// <summary>
        /// Adds a raw POST body to this request.
        /// </summary>
        public void AddRaw(Stream stream)
        {
            if (rawContent == null)
                rawContent = new MemoryStream();

            stream.CopyTo(rawContent);
        }

        /// <summary>
        /// Add a new FILE parameter to this request.
        /// </summary>
        public void AddFile(string name, byte[] data)
        {
            Files.Add(name, data);
        }

        /// <summary>
        /// Add a new POST parameter to this request.
        /// </summary>
        public void AddParameter(string name, string value)
        {
            Parameters.Add(name, value);
        }

        private void unbindEvents()
        {
            UploadProgress = null;
            DownloadProgress = null;
            Finished = null;
            Started = null;

            try
            {
                if (request?.ServicePoint.BindIPEndPointDelegate != null)
                    // ReSharper disable once DelegateSubtraction
                    request.ServicePoint.BindIPEndPointDelegate -= bindEndPoint;
            }
            catch
            {
            }
        }

        #region Timeout Handling

        private long lastAction;

        private long timeSinceLastAction => (DateTime.Now.Ticks - lastAction) / TimeSpan.TicksPerMillisecond;

        private bool hasExceededTimeout => timeSinceLastAction > Timeout;

        private object checkTimeoutLoop(object state)
        {
            while (!Aborted && !Completed)
            {
                if (hasExceededTimeout) abortRequest();
                Thread.Sleep(500);
            }

            return state;
        }

        private void reportForwardProgress()
        {
            lastAction = DateTime.Now.Ticks;
        }

        #endregion

        #region IDisposable Support

        private bool isDisposed;

        protected void Dispose(bool disposing)
        {
            if (isDisposed) return;
            isDisposed = true;

            if (!(ResponseStream is MemoryStream))
                ResponseStream?.Dispose();

            internalResponseStream?.Dispose();
            response?.Close();
            unbindEvents();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion

        #region Retry Logic

        private const int default_retry_count = 2;

        private int retryCount = default_retry_count;

        public int RetryCount
        {
            get { return retryCount; }
            set { retriesRemaining = retryCount = value; }
        }

        private int retriesRemaining = default_retry_count;

        #endregion
    }
}
