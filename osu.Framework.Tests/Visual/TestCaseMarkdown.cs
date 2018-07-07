// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.ComponentModel;
using System.Net.Http;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Testing;

namespace osu.Framework.Tests.Visual
{
    [Description("markdown reader")]
    public class TestCaseMarkdown : TestCase
    {
        public TestCaseMarkdown()
        {
            string markdownText = "";

            try
            {
                //test readme
                //in https://github.com/lunet-io/scriban/blob/master/doc/language.md#92-if-expression-else-else-if-expression

                var url = "https://raw.githubusercontent.com/lunet-io/scriban/master/doc/language.md";
                var httpClient = new HttpClient();
                //download file
                markdownText = httpClient.GetStringAsync(url).ConfigureAwait(false).GetAwaiter().GetResult();

            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            //create markdown scrollView container
            var container = new MarkdownScrollContainer()
            {
                RelativeSizeAxes = Axes.Both,
                MarkdownText = markdownText,
            };
            Add(container);
        }
    }
}
