// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
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
using osu.Framework.Extensions.ExceptionExtensions;
using osu.Framework.Logging;

namespace osu.Framework.IO.Network
{
    public class WebRequest : IDisposable
    {
        internal const int MAX_RETRIES = 1;

        /// <summary>
        /// Invoked when a response has been received, but not data has been received.
        /// </summary>
        public event Action Started;

        /// <summary>
        /// Invoked when the <see cref="WebRequest"/> has finished successfully.
        /// </summary>
        public event Action Finished;

        /// <summary>
        /// Invoked when the <see cref="WebRequest"/> has failed.
        /// </summary>
        public event Action<Exception> Failed;

        /// <summary>
        /// Invoked when the download progress has changed.
        /// </summary>
        public event Action<long, long> DownloadProgress;

        /// <summary>
        /// Invoked when the upload progress has changed.
        /// </summary>
        public event Action<long, long> UploadProgress;

        /// <summary>
        /// Whether the <see cref="WebRequest"/> was aborted due to an exception or a user abort request.
        /// </summary>
        public bool Aborted { get; private set; }

        private bool completed;
        /// <summary>
        /// Whether the <see cref="WebRequest"/> has been run.
        /// </summary>
        public bool Completed
        {
            get { return completed; }
            private set
            {
                completed = value;
                if (!completed) return;

                // WebRequests can only be used once - no need to keep events bound
                // This helps with disposal in PerformAsync usages
                Started = null;
                Finished = null;
                DownloadProgress = null;
                UploadProgress = null;
            }
        }

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
        /// POST parameters.
        /// </summary>
        private readonly Dictionary<string, string> parameters = new Dictionary<string, string>();

        /// <summary>
        /// FILE parameters.
        /// </summary>
        private readonly IDictionary<string, byte[]> files = new Dictionary<string, byte[]>();

        /// <summary>
        /// The request headers.
        /// </summary>
        private readonly IDictionary<string, string> headers = new Dictionary<string, string>();

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

        internal int RetryCount { get; private set; }

        /// <summary>
        /// Whether this request should internally retry (up to <see cref="MAX_RETRIES"/> times) on a timeout before throwing an exception.
        /// </summary>
        public bool AllowRetryOnTimeout { get; set; } = true;

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

            logger = Logger.GetLogger(LoggingTarget.Network);
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

        private CancellationTokenSource abortToken;
        private CancellationTokenSource timeoutToken;

        private LengthTrackingStream requestStream;
        private HttpResponseMessage response;

        private long contentLength => requestStream?.Length ?? 0;

        private const string form_boundary = "-----------------------------28947758029299";

        private const string form_content_type = "multipart/form-data; boundary=" + form_boundary;

        /// <summary>
        /// Performs the request asynchronously.
        /// </summary>
        public async Task PerformAsync()
        {
            if (Completed)
                throw new InvalidOperationException($"The {nameof(WebRequest)} has already been run.");
            try
            {
                await Task.Factory.StartNew(internalPerform, TaskCreationOptions.LongRunning);
            }
            catch (AggregateException ae)
            {
                ae.RethrowIfSingular();
            }
        }

        private void internalPerform()
        {
            using (abortToken = new CancellationTokenSource())
            using (timeoutToken = new CancellationTokenSource())
            using (var linkedToken = CancellationTokenSource.CreateLinkedTokenSource(abortToken.Token, timeoutToken.Token))
            {
                try
                {
                    PrePerform();

                    HttpRequestMessage request;

                    switch (Method)
                    {
                        default:
                            throw new InvalidOperationException($"HTTP method {Method} is currently not supported");
                        case HttpMethod.GET:
                            if (files.Count > 0)
                                throw new InvalidOperationException($"Cannot use {nameof(AddFile)} in a GET request. Please set the {nameof(Method)} to POST.");

                            StringBuilder requestParameters = new StringBuilder();
                            foreach (var p in parameters)
                                requestParameters.Append($@"{p.Key}={p.Value}&");
                            string requestString = requestParameters.ToString().TrimEnd('&');

                            request = new HttpRequestMessage(System.Net.Http.HttpMethod.Get, string.IsNullOrEmpty(requestString) ? Url : $"{Url}?{requestString}");
                            break;
                        case HttpMethod.POST:
                            request = new HttpRequestMessage(System.Net.Http.HttpMethod.Post, Url);

                            Stream postContent;

                            if (rawContent != null)
                            {
                                if (parameters.Count > 0)
                                    throw new InvalidOperationException($"Cannot use {nameof(AddRaw)} in conjunction with {nameof(AddParameter)}");
                                if (files.Count > 0)
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

                                foreach (var p in parameters)
                                    formData.Add(new StringContent(p.Value), p.Key);

                                foreach (var p in files)
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
                                UploadProgress?.Invoke(v, contentLength);
                            };

                            request.Content = new StreamContent(requestStream);
                            if (!string.IsNullOrEmpty(ContentType))
                                request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse(ContentType);
                            break;
                    }

                    if (!string.IsNullOrEmpty(Accept))
                        request.Headers.Accept.TryParseAdd(Accept);

                    foreach (var kvp in headers)
                        request.Headers.Add(kvp.Key, kvp.Value);

                    reportForwardProgress();

                    using (request)
                    {
                        response = client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, linkedToken.Token).Result;

                        ResponseStream = CreateOutputStream();

                        switch (Method)
                        {
                            case HttpMethod.GET:
                                //GETs are easy
                                beginResponse(linkedToken.Token);
                                break;
                            case HttpMethod.POST:
                                reportForwardProgress();
                                UploadProgress?.Invoke(0, contentLength);

                                beginResponse(linkedToken.Token);
                                break;
                        }
                    }
                }
                catch (Exception) when (timeoutToken.IsCancellationRequested)
                {
                    Complete(new WebException($"Request to {Url} timed out after {timeSinceLastAction / 1000} seconds idle (read {responseBytesRead} bytes, retried {RetryCount} times).", WebExceptionStatus.Timeout));
                }
                catch (Exception) when (abortToken.IsCancellationRequested)
                {
                    Complete(new WebException($"Request to {Url} aborted by user.", WebExceptionStatus.RequestCanceled));
                }
                catch (Exception e)
                {
                    if (Completed)
                        // we may be coming from one of the exception blocks handled above (as Complete will rethrow all exceptions).
                        throw;

                    Complete(e);
                }
            }
        }

        /// <summary>
        /// Performs the request synchronously.
        /// </summary>
        public void Perform()
        {
            try
            {
                PerformAsync().Wait();
            }
            catch (AggregateException ae)
            {
                ae.RethrowIfSingular();
            }
        }

        /// <summary>
        /// Task to run direct before performing the request.
        /// </summary>
        protected virtual void PrePerform()
        {
        }

        private void beginResponse(CancellationToken cancellationToken)
        {
            using (var responseStream = response.Content.ReadAsStreamAsync().Result)
            {
                reportForwardProgress();
                Started?.Invoke();

                buffer = new byte[buffer_size];

                while (true)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    int read = responseStream.Read(buffer, 0, buffer_size);

                    reportForwardProgress();

                    if (read > 0)
                    {
                        ResponseStream.Write(buffer, 0, read);
                        responseBytesRead += read;
                        DownloadProgress?.Invoke(responseBytesRead, response.Content.Headers.ContentLength ?? responseBytesRead);
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

            bool allowRetry = AllowRetryOnTimeout;

            if (e != null)
                allowRetry &= we?.Status == WebExceptionStatus.Timeout;
            else if (!response.IsSuccessStatusCode)
            {
                e = new WebException(response.StatusCode.ToString());

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

            if (e != null)
            {
                if (allowRetry && RetryCount < MAX_RETRIES && responseBytesRead == 0)
                {
                    RetryCount++;

                    logger.Add($@"Request to {Url} failed with {e} (retrying {RetryCount}/{MAX_RETRIES}).");

                    //do a retry
                    internalPerform();
                    return;
                }

                logger.Add($"Request to {Url} failed with {e}.");
            }
            else
                logger.Add($@"Request to {Url} successfully completed!");

            try
            {
                ProcessResponse();
            }
            catch (Exception se)
            {
                logger.Add($"Processing response from {Url} failed with {se}.");
                e = e == null ? se : new AggregateException(e, se);
            }

            Completed = true;

            if (e == null)
            {
                Finished?.Invoke();
            }
            else
            {
                Failed?.Invoke(e);
                Aborted = true;
                throw e;
            }
        }

        /// <summary>
        /// Performs any post-processing of the response.
        /// Exceptions thrown in this method will be passed to <see cref="Finished"/>.
        /// </summary>
        protected virtual void ProcessResponse()
        {
        }

        /// <summary>
        /// Forcefully abort the request.
        /// </summary>
        public void Abort()
        {
            if (Aborted || Completed) return;

            Aborted = true;
            Completed = true;

            try
            {
                abortToken?.Cancel();
            }
            catch (ObjectDisposedException)
            {
            }
        }

        /// <summary>
        /// Adds a raw POST body to this request.
        /// This may not be used in conjunction with <see cref="AddFile"/> and <see cref="AddParameter"/>.
        /// </summary>
        /// <param name="text">The text.</param>
        public void AddRaw(string text)
        {
            AddRaw(Encoding.UTF8.GetBytes(text));
        }

        /// <summary>
        /// Adds a raw POST body to this request.
        /// This may not be used in conjunction with <see cref="AddFile"/> and <see cref="AddParameter"/>.
        /// </summary>
        /// <param name="bytes">The raw data.</param>
        public void AddRaw(byte[] bytes)
        {
            AddRaw(new MemoryStream(bytes));
        }

        /// <summary>
        /// Adds a raw POST body to this request.
        /// This may not be used in conjunction with <see cref="AddFile"/> and <see cref="AddParameter"/>.
        /// </summary>
        /// <param name="stream">The stream containing the raw data. This stream will _not_ be finalized by this request.</param>
        public void AddRaw(Stream stream)
        {
            if (stream == null) throw new ArgumentNullException(nameof(stream));

            if (rawContent == null)
                rawContent = new MemoryStream();

            stream.CopyTo(rawContent);
        }

        /// <summary>
        /// Add a new FILE parameter to this request. Replaces any existing file with the same name.
        /// This may not be used in conjunction with <see cref="AddRaw(Stream)"/>. GET requests may not contain files.
        /// </summary>
        /// <param name="name">The name of the file. This becomes the name of the file in a multi-part form POST content.</param>
        /// <param name="data">The file data.</param>
        public void AddFile(string name, byte[] data)
        {
            if (name == null) throw new ArgumentNullException(nameof(name));
            if (data == null) throw new ArgumentNullException(nameof(data));

            files[name] = data;
        }

        /// <summary>
        /// Add a new POST parameter to this request. Replaces any existing parameter with the same name.
        /// This may not be used in conjunction with <see cref="AddRaw(Stream)"/>.
        /// </summary>
        /// <param name="name">The name of the parameter.</param>
        /// <param name="value">The parameter value.</param>
        public void AddParameter(string name, string value)
        {
            if (name == null) throw new ArgumentNullException(nameof(name));
            if (value == null) throw new ArgumentNullException(nameof(value));

            parameters[name] = value;
        }

        /// <summary>
        /// Adds a new header to this request. Replaces any existing header with the same name.
        /// </summary>
        /// <param name="name">The name of the header.</param>
        /// <param name="value">The header value.</param>
        public void AddHeader(string name, string value)
        {
            if (name == null) throw new ArgumentNullException(nameof(name));
            if (value == null) throw new ArgumentNullException(nameof(value));

            headers[name] = value;
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

            Abort();

            requestStream?.Dispose();
            response?.Dispose();

            if (!(ResponseStream is MemoryStream))
                ResponseStream?.Dispose();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

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
