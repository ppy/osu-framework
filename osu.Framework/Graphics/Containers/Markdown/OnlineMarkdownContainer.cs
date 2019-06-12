// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.IO.Network;

namespace osu.Framework.Graphics.Containers.Markdown
{
    public class OnlineMarkdownContainer : MarkdownContainer
    {
        private string url;

        /// <summary>
        /// The URL of the document.
        /// </summary>
        public string Url
        {
            get => url;
            set
            {
                if (url == value)
                    return;

                url = value;

                if (LoadState >= LoadState.Ready)
                    reloadDocument(url);
            }
        }

        /// <summary>
        /// The root URL of any resources linked to by the document.
        /// </summary>
        /// <exception cref="ArgumentException">If the provided URL is not an absolute URI.</exception>
        public new string RootUrl
        {
            get => base.RootUrl;
            set => base.RootUrl = value;
        }

        protected override void LoadAsyncComplete()
        {
            reloadDocument(url);

            base.LoadAsyncComplete();
        }

        private void reloadDocument(string url)
        {
            Text = string.Empty;

            if (string.IsNullOrEmpty(url))
                return;

            try
            {
                string document = RetrieveDocument(url);

                if (string.IsNullOrEmpty(document) || this.url != url)
                    return;

                DocumentUrl = url;
                Text = document;
            }
            catch
            {
            }
        }

        /// <summary>
        /// Retrieves the document.
        /// </summary>
        /// <param name="url">The url of the document.</param>
        /// <returns>The document content.</returns>
        protected virtual string RetrieveDocument(string url)
        {
            var req = new WebRequest(url);
            req.Perform();
            return req.ResponseString;
        }
    }
}
