// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
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

        private void finished(WebRequest request, Exception e)
        {
            Finished?.Invoke(this, e);
        }

        public T ResponseObject => JsonConvert.DeserializeObject<T>(ResponseString);

        /// <summary>
        /// Request has finished with success or failure. Check exception == null for success.
        /// </summary>
        public new event RequestCompleteHandler<T> Finished;
    }
}
