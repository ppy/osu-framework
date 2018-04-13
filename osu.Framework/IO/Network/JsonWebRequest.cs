// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

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

        protected override void ProcessResponse() => deserialisedResponse = JsonConvert.DeserializeObject<T>(ResponseString);

        private T deserialisedResponse;

        public T ResponseObject => deserialisedResponse;
    }
}
