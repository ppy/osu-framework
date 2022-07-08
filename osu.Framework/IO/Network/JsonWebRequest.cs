// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using Newtonsoft.Json;

namespace osu.Framework.IO.Network
{
    /// <summary>
    /// A web request with a specific JSON response format.
    /// </summary>
    /// <typeparam name="T">the response format.</typeparam>
    public class JsonWebRequest<T> : WebRequest
    {
        protected override string Accept => "application/json";

        public JsonWebRequest(string url = null, params object[] args)
            : base(url, args)
        {
        }

        protected override void ProcessResponse()
        {
            if (ResponseStream != null)
                ResponseObject = JsonConvert.DeserializeObject<T>(GetResponseString());
        }

        public T ResponseObject { get; private set; }
    }
}
