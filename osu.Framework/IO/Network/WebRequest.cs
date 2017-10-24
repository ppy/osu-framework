// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
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

        /// <summary>
        /// The request headers.
        /// </summary>
        // ReSharper disable once CollectionNeverUpdated.Global
        public Dictionary<string, string> Headers = new Dictionary<string, string>();

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
            client = new HttpClient(new HttpClientHandler { AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate });
            client.DefaultRequestHeaders.UserAgent.ParseAdd("osu!");
            client.DefaultRequestHeaders.ExpectContinue = true;

            // Timeout is controlled manually through cancellation tokens because
            // HttpClient does not properly timeout while reading chunked data
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

        private CancellationTokenSource abortToken;
        private CancellationTokenSource timeoutToken;
        private CancellationTokenSource linkedToken;

        private LengthTrackingStream requestStream;
        private HttpResponseMessage response;

        private long contentLength => requestStream?.Length ?? 0;

        private const string form_boundary = "-----------------------------28947758029299";

        private const string form_content_type = "multipart/form-data; boundary=" + form_boundary;

        /// <summary>
        /// Performs the request asynchronously.
        /// </summary>
        public Task PerformAsync()
        {
            if (!canPerform)
                throw new InvalidOperationException("Can not perform a web request multiple times.");
            canPerform = false;

            Aborted = false;
            abortRequest();
            abortToken = new CancellationTokenSource();
            timeoutToken = new CancellationTokenSource();
            linkedToken = CancellationTokenSource.CreateLinkedTokenSource(abortToken.Token, timeoutToken.Token);

            return Task.Factory.StartNew(() =>
            {
                PrePerform();

                try
                {
                    reportForwardProgress();

                    HttpRequestMessage request;

                    switch (Method)
                    {
                        default:
                            throw new InvalidOperationException($"HTTP method {Method} is currently not supported");
                        case HttpMethod.GET:
                            if (Files.Count > 0)
                                throw new InvalidOperationException($"Cannot use {nameof(AddFile)} in a GET request. Please set the {nameof(Method)} to POST.");

                            StringBuilder requestParameters = new StringBuilder();
                            foreach (var p in Parameters)
                                requestParameters.Append($@"{p.Key}={p.Value}&");
                            string requestString = requestParameters.ToString().TrimEnd('&');

                            request = new HttpRequestMessage(System.Net.Http.HttpMethod.Get, string.IsNullOrEmpty(requestString) ? Url : $"{Url}?{requestString}");
                            break;
                        case HttpMethod.POST:
                            request = new HttpRequestMessage(System.Net.Http.HttpMethod.Post, Url);

                            Stream postContent;

                            if (rawContent != null)
                            {
                                if (Parameters.Count > 0)
                                    throw new InvalidOperationException($"Cannot use {nameof(AddRaw)} in conjunction with {nameof(AddParameter)}");
                                if (Files.Count > 0)
                                    throw new InvalidOperationException($"Cannot use {nameof(AddRaw)} in conjunction with {nameof(AddFile)}");

                                postContent = new MemoryStream();
                                rawContent.Position = 0;
                                rawContent.CopyTo(postContent);
                                postContent.Position = 0;
                            }
                            else
                            {
                                if (!string.IsNullOrEmpty(ContentType) && ContentType != form_content_type)
                                    throw new InvalidOperationException($"Cannot use custom {nameof(ContentType)} in a POST request.");

                                ContentType = form_content_type;

                                var formData = new MultipartFormDataContent(form_boundary);

                                foreach (var p in Parameters)
                                    formData.Add(new StringContent(p.Value), p.Key);

                                foreach (var p in Files)
                                {
                                    var byteContent = new ByteArrayContent(p.Value);
                                    byteContent.Headers.Add("Content-Type", "application/octet-stream");
                                    formData.Add(byteContent, p.Key, p.Key);
                                }

                                postContent = formData.ReadAsStreamAsync().Result;
                            }

                            requestStream = new LengthTrackingStream(postContent);

                            requestStream.BytesRead.ValueChanged += v =>
                            {
                                reportForwardProgress();
                                UploadProgress?.Invoke(this, v, contentLength);
                            };

                            request.Content = new StreamContent(requestStream);
                            if (!string.IsNullOrEmpty(ContentType))
                                request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse(ContentType);
                            break;
                    }

                    if (!string.IsNullOrEmpty(Accept))
                        request.Headers.Accept.TryParseAdd(Accept);

                    foreach (var kvp in Headers)
                        request.Headers.Add(kvp.Key, kvp.Value);

                    response = client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, linkedToken.Token).Result;

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
                catch (Exception) when (timeoutToken.IsCancellationRequested)
                {
                    logger.Add($@"Request timeout exceeded ({timeSinceLastAction})");
                    Complete(new WebException($"Request to {Url} timed out after {timeSinceLastAction / 1000} seconds idle (read {responseBytesRead} bytes).", WebExceptionStatus.Timeout));
                }
                catch (Exception e)
                {
                    Complete(e);
                    throw;
                }
            }, linkedToken.Token, TaskCreationOptions.LongRunning, TaskScheduler.Current);
        }

        /// <summary>
        /// Performs the request synchronously.
        /// </summary>
        public void Perform() => PerformAsync().Wait();

        /// <summary>
        /// Task to run direct before performing the request.
        /// </summary>
        protected virtual void PrePerform()
        {
        }

        private void beginResponse()
        {
            using (var responseStream = response.Content.ReadAsStreamAsync().Result)
            {
                reportForwardProgress();
                Started?.Invoke(this);

                buffer = new byte[buffer_size];

                while (true)
                {
                    linkedToken.Token.ThrowIfCancellationRequested();

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

        protected virtual void Complete(Exception e = null)
        {
            if (Aborted)
                return;

            var we = e as WebException;

            bool allowRetry = false;
            bool hasFailed = false;

            if (e != null)
            {
                hasFailed = true;
                allowRetry = we?.Status == WebExceptionStatus.Timeout;
            }
            else if (!response.IsSuccessStatusCode)
            {
                hasFailed = true;
                allowRetry = true;

                switch (response.StatusCode)
                {
                    case HttpStatusCode.GatewayTimeout:
                    case HttpStatusCode.RequestTimeout:
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
            }

            if (hasFailed)
            {
                if (allowRetry && retriesRemaining-- > 0 && responseBytesRead == 0)
                {
                    logger.Add($@"Request to {Url} failed with {e?.ToString() ?? response.StatusCode.ToString()} (retrying {default_retry_count - retriesRemaining}/{default_retry_count}).");

                    //do a retry
                    Perform();
                    return;
                }

                logger.Add($"Request to {Url} failed with {e?.ToString() ?? response.StatusCode.ToString()} (FAILED).");
            }
            else
                logger.Add($@"Request to {Url} successfully completed!");

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
            linkedToken?.Cancel();
            linkedToken?.Dispose();
            linkedToken = null;
            abortToken?.Dispose();
            abortToken = null;
            timeoutToken?.Dispose();
            timeoutToken = null;
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

        private void reportForwardProgress()
        {
            lastAction = DateTime.Now.Ticks;
            timeoutToken.CancelAfter(Timeout);
        }

        #endregion

        #region IDisposable Support

        private bool isDisposed;

        protected void Dispose(bool disposing)
        {
            if (isDisposed) return;
            isDisposed = true;

            abortRequest();

            requestStream?.Dispose();

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
                get { return baseStream.Position; }
                set { baseStream.Position = value; }
            }

            protected override void Dispose(bool disposing)
            {
                base.Dispose(disposing);
                baseStream.Dispose();
            }
        }
    }
}
