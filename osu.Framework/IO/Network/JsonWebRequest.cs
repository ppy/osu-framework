// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Net;
using Newtonsoft.Json;

namespace osu.Framework.IO.Network
{
    /// <summary>
    /// A web request with a specific JSON response format.
    /// </summary>
    /// <typeparam name="T">the response format.</typeparam>
    public class JsonWebRequest<T> : WebRequest
    {
        public JsonWebRequest(string url = null, params object[] args)
            : base(url, args)
        {
            base.Finished += finished;
        }

        protected override HttpWebRequest CreateWebRequest(string requestString = null)
        {
            var req = base.CreateWebRequest(requestString);
            req.Accept = @"application/json";
            return req;
        }

        private void finished(WebRequest request, Exception e)
        {
            try
            {
                deserialisedResponse = JsonConvert.DeserializeObject<T>(ResponseString);
            }
            catch (Exception se)
            {
                e = e == null ? se : new AggregateException(e, se);
            }

            Finished?.Invoke(this, e);
        }

        private T deserialisedResponse;

        public T ResponseObject => deserialisedResponse;

        /// <summary>
        /// Request has finished with success or failure. Check exception == null for success.
        /// </summary>
        public new event RequestCompleteHandler<T> Finished;
    }
}
