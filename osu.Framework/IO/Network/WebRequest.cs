// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Authentication;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using osu.Framework.Configuration;
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

        public const int DEFAULT_TIMEOUT = 10000;

        public HttpMethod Method;

        /// <summary>
        /// The amount of time from last sent or received data to trigger a timeout and abort the request.
        /// </summary>
        public int Timeout = DEFAULT_TIMEOUT;

        /// <summary>
        /// The type of content expected by this web request.
        /// </summary>
        protected virtual string Accept => string.Empty;

        private static readonly Logger logger;

        private static readonly HttpClient client;

        static WebRequest()
        {
            var handler = new HttpClientHandler
            {
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
                SslProtocols = SslProtocols.Tls,
                CheckCertificateRevocationList = false,
                MaxConnectionsPerServer = 12
            };

            client = new HttpClient(handler);
            client.DefaultRequestHeaders.UserAgent.ParseAdd("osu!");
            client.DefaultRequestHeaders.ExpectContinue = true;
            client.Timeout = System.Threading.Timeout.InfiniteTimeSpan;

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

        private int responseBytesRead;

        private const int buffer_size = 32768;
        private byte[] buffer;

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

        public HttpResponseHeaders ResponseHeaders => response.Headers;

        private bool canPerform = true;

        private CancellationTokenSource cancellationToken;
        private HttpResponseMessage response;
        private int contentLength;

        /// <summary>
        /// Performs the request asynchronously.
        /// </summary>
        public Task PerformAsync()
        {
            return Task.Factory.StartNew(() =>
            {
                if (!canPerform)
                    throw new InvalidOperationException("Can not perform a web request multiple times.");
                canPerform = false;

                Aborted = false;
                abortRequest();
                cancellationToken = new CancellationTokenSource();

                PrePerform();

                try
                {
                    reportForwardProgress();
                    ThreadPool.QueueUserWorkItem(checkTimeoutLoop);

                    HttpRequestMessage request = null;

                    switch (Method)
                    {
                        case HttpMethod.GET:
                            Debug.Assert(Files.Count == 0);

                            StringBuilder requestParameters = new StringBuilder();
                            foreach (var p in Parameters)
                                requestParameters.Append($@"{p.Key}={p.Value}&");
                            string requestString = requestParameters.ToString().TrimEnd('&');

                            request = new HttpRequestMessage(System.Net.Http.HttpMethod.Get, string.IsNullOrEmpty(requestString) ? Url : $"{Url}?{requestString}");
                            break;
                        case HttpMethod.POST:
                            const string boundary = @"-----------------------------28947758029299";

                            var formData = new MultipartFormDataContent(boundary);
                            contentLength = formData.ReadAsByteArrayAsync().Result.Length;

                            formData.Add(new FormUrlEncodedContent(Parameters));

                            foreach (var p in Files)
                                formData.Add(new ByteArrayContent(p.Value), p.Key, p.Key);

                            using (var requestStream = new LengthTrackingStream(formData.ReadAsStreamAsync().Result))
                            {
                                requestStream.BytesRead.ValueChanged += v =>
                                {
                                    reportForwardProgress();
                                    UploadProgress?.Invoke(this, v, contentLength);
                                };

                                request = new HttpRequestMessage(System.Net.Http.HttpMethod.Post, Url) { Content = new StreamContent(requestStream) };
                            }
                            break;
                    }

                    if (!string.IsNullOrEmpty(Accept))
                        request?.Headers.Accept.TryParseAdd(Accept);

                    response = client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken.Token).Result;

                    ResponseStream = CreateOutputStream();

                    switch (Method)
                    {
                        case HttpMethod.GET:
                            //GETs are easy
                            beginResponse();
                            break;
                        case HttpMethod.POST:
                            reportForwardProgress();
                            UploadProgress?.Invoke(this, 0, contentLength);

                            beginResponse();
                            break;
                    }
                }
                catch (Exception e)
                {
                    Complete(e);
                }
            }, TaskCreationOptions.LongRunning);
        }

        /// <summary>
        /// Task to run direct before performing the request.
        /// </summary>
        protected virtual void PrePerform()
        {
        }

        private void beginResponse()
        {
            try
            {
                using (var responseStream = response.Content.ReadAsStreamAsync().Result)
                {
                    reportForwardProgress();
                    Started?.Invoke(this);

                    buffer = new byte[buffer_size];

                    while (true)
                    {
                        int read = responseStream.Read(buffer, 0, buffer_size);

                        reportForwardProgress();

                        if (read > 0)
                        {
                            ResponseStream.Write(buffer, 0, read);
                            responseBytesRead += read;
                            DownloadProgress?.Invoke(this, responseBytesRead, response.Content.Headers.ContentLength ?? responseBytesRead);
                        }
                        else
                        {
                            ResponseStream.Seek(0, SeekOrigin.Begin);
                            Complete();
                            break;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Complete(e);
            }
        }


        protected virtual void Complete(Exception e = null)
        {
            if (Aborted)
                return;

            switch (e)
            {
                default:
                    bool allowRetry = true;

                    switch (response.StatusCode)
                    {
                        case HttpStatusCode.RequestTimeout:
                            if (hasExceededTimeout)
                            {
                                logger.Add($@"Timeout exceeded ({timeSinceLastAction} > {DEFAULT_TIMEOUT})");
                                e = new WebException($"Timeout to {Url} after {timeSinceLastAction / 1000} seconds idle (read {responseBytesRead} bytes).", WebExceptionStatus.Timeout);
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
                        logger.Add($@"Request to {Url} failed with {response.StatusCode} (retrying {default_retry_count - retriesRemaining}/{default_retry_count}).");

                        //do a retry
                        PerformAsync();
                        return;
                    }

                    logger.Add($@"Request to {Url} failed with {response.StatusCode} (FAILED).");
                    break;
                case null:
                    logger.Add($@"Request to {Url} successfully completed!");
                    break;
            }

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
            cancellationToken?.Cancel();
            cancellationToken?.Dispose();
            cancellationToken = null;
        }

        /// <summary>
        /// Forcefully abort the request.
        /// </summary>
        public void Abort()
        {
            if (Aborted) return;
            Aborted = true;

            abortRequest();
            canPerform = true;

            if (!KeepEventsBound)
                unbindEvents();
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
        }

        #region Timeout Handling

        private long lastAction;

        private long timeSinceLastAction => (DateTime.Now.Ticks - lastAction) / TimeSpan.TicksPerMillisecond;

        private bool hasExceededTimeout => timeSinceLastAction > Timeout;

        private void checkTimeoutLoop(object state)
        {
            while (!Aborted && !Completed)
            {
                if (hasExceededTimeout) abortRequest();
                Thread.Sleep(500);
            }
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

            abortRequest();

            if (!(ResponseStream is MemoryStream))
                ResponseStream?.Dispose();

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

        private class LengthTrackingStream : Stream
        {
            public readonly BindableLong BytesRead = new BindableLong();

            private readonly Stream baseStream;

            public LengthTrackingStream(Stream baseStream)
            {
                this.baseStream = baseStream;
            }

            public override void Flush()
            {
                baseStream.Flush();
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                int read = baseStream.Read(buffer, offset, count);
                BytesRead.Value += read;
                return read;
            }

            public override long Seek(long offset, SeekOrigin origin)
            {
                return baseStream.Seek(offset, origin);
            }

            public override void SetLength(long value)
            {
                baseStream.SetLength(value);
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
                baseStream.Write(buffer, offset, count);
            }

            public override bool CanRead => baseStream.CanRead;
            public override bool CanSeek => baseStream.CanSeek;
            public override bool CanWrite => baseStream.CanWrite;
            public override long Length => baseStream.Length;
            public override long Position
            {
                get => baseStream.Position;
                set => baseStream.Position = value;
            }

            protected override void Dispose(bool disposing)
            {
                base.Dispose(disposing);
                baseStream.Dispose();
            }
        }
    }
}
