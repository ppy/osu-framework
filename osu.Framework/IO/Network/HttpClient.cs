// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System.Net;
using System.Net.Http;

namespace osu.Framework.IO.Network
{
    public class HttpClient : System.Net.Http.HttpClient
    {
        private static readonly object lock_obj = new object();

        private static string userAgent;
        public static string UserAgent
        {
            get => instance != null ? instance.DefaultRequestHeaders.UserAgent.ToString() : userAgent;
            set
            {
                userAgent = value;
                if (instance == null) return;

                instance.DefaultRequestHeaders.UserAgent.Clear();
                instance.DefaultRequestHeaders.UserAgent.ParseAdd(value);
            }
        }

        private static HttpClient instance;
        public static HttpClient Instance
        {
            get
            {
                lock (lock_obj)
                    return instance ?? (instance = new HttpClient());
            }
        }

        private HttpClient()
            : base(new HttpClientHandler { AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate })
        {
            if (!string.IsNullOrEmpty(UserAgent))
                DefaultRequestHeaders.UserAgent.ParseAdd(UserAgent);

            // Timeout is controlled manually through cancellation tokens because
            // HttpClient does not properly timeout while reading chunked data
            Timeout = System.Threading.Timeout.InfiniteTimeSpan;
        }
    }
}
