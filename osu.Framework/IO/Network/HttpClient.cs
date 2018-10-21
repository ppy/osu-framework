// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System.Net;
using System.Net.Http;
using osu.Framework.Configuration;

namespace osu.Framework.IO.Network
{
    public class HttpClient : System.Net.Http.HttpClient
    {
        private static readonly object lock_obj;
        internal static readonly Bindable<string> USER_AGENT;

        private static HttpClient instance;
        public static HttpClient Instance
        {
            get
            {
                lock (lock_obj)
                {
                    return instance ?? (instance = new HttpClient());
                }
            }
        }

        static HttpClient()
        {
            lock_obj = new object();
            USER_AGENT = new Bindable<string>();

            USER_AGENT.BindValueChanged(ua =>
            {
                instance?.DefaultRequestHeaders.UserAgent.Clear();
                instance?.DefaultRequestHeaders.UserAgent.ParseAdd(ua);
            });
        }

        private HttpClient()
            : base(new HttpClientHandler { AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate })
        {
            DefaultRequestHeaders.UserAgent.ParseAdd(USER_AGENT?.Value);

            // Timeout is controlled manually through cancellation tokens because
            // HttpClient does not properly timeout while reading chunked data
            Timeout = System.Threading.Timeout.InfiniteTimeSpan;
        }
    }
}
