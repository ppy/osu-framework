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
            try
            {
                //test readme
                var url = "https://raw.githubusercontent.com/lunet-io/scriban/master/doc/language.md";
                var httpClient = new HttpClient();

                //download file 
                string markdowntext = httpClient.GetStringAsync(url).ConfigureAwait(false).GetAwaiter().GetResult();

                //create markdown scrollView container
                var container = new MarkdownScrollContainer()
                {
                    RelativeSizeAxes = Axes.Both,
                };
                container.MarkdownText = markdowntext;
                Add(container);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            
        }
    }
}
