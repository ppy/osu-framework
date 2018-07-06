using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using Markdig;
using Markdig.Extensions.AutoIdentifiers;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Testing;

namespace osu.Framework.Tests.Visual
{
    [System.ComponentModel.Description("markdown reader")]
    public class TestCaseMarkdown : TestCase
    {
        public TestCaseMarkdown()
        {
            var url = "https://github.com/lunet-io/scriban/blob/master/doc/language.md";
            string markdowntext = "";

            //if url is https
            if (url.StartsWith("https:"))
            {
                Uri uri;
                if (!Uri.TryCreate(url, UriKind.Absolute, out uri))
                {
                    Console.WriteLine($"Unable to parse Uri `{url}`");
                    return;
                }
                // Special handling of github URL to access the raw content instead
                if (uri.Host == "github.com")
                {
                    // https://github.com/lunet-io/scriban/blob/master/doc/language.md
                    // https://raw.githubusercontent.com/lunet-io/scriban/master/doc/language.md
                    var newPath = uri.AbsolutePath;
                    var paths = new List<string>(newPath.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries));
                    if (paths.Count < 5 || paths[2] != "blob")
                    {
                        Console.WriteLine($"Invalid github.com URL `{uri}`");
                        return;
                    }
                    paths.RemoveAt(2); // remove blob
                    uri = new Uri($"https://raw.githubusercontent.com/{(string.Join("/", paths))}");
                }

                var httpClient = new HttpClient();
                markdowntext = httpClient.GetStringAsync(uri).ConfigureAwait(false).GetAwaiter().GetResult();
            }
            else
            {
                //read from local file
                markdowntext = File.ReadAllText(url);
            }

            //convert markdown into markdown document
            var pipeline = new MarkdownPipelineBuilder().UseAutoIdentifiers(AutoIdentifierOptions.GitHub).Build();
            var doc = Markdig.Markdown.Parse(markdowntext, pipeline);

            //create markdown scrollView container
            var container = new MarkdownScrollContainer()
            {
                RelativeSizeAxes = Axes.Both,
            };
            container.MarkdownDocument = doc;
            Add(container);
        }
    }
}
