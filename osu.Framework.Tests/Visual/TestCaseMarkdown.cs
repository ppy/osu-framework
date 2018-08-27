// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.ComponentModel;
using System.Net.Http;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers.Markdown;
using osu.Framework.Testing;

namespace osu.Framework.Tests.Visual
{
    [Description("markdown reader")]
    public class TestCaseMarkdown : TestCase
    {
        public TestCaseMarkdown()
        {
            MarkdownContainer markdownContainer;
            Add(markdownContainer = new MarkdownContainer
            {
                RelativeSizeAxes = Axes.Both,
            });

            AddStep("Markdown Heading", () =>
            {
                markdownContainer.Text = @"# Header 1
## Header 2
### Header 3
#### Header 4
##### Header 5";
            });

            AddStep("Markdown Seperator", () =>
            {
                markdownContainer.Text = @"# Language

";
            });

            AddStep("Markdown Heading", () =>
            {
                markdownContainer.Text = @"- [1. Blocks](#1-blocks)
  - [1.1 Code block](#11-code-block)
  - [1.2 Text block](#12-text-block)
  - [1.3 Escape block](#13-escape-block)
  - [1.4 Whitespace control](#14-whitespace-control)
- [2 Comments](#2-comments)
- [3 Literals](#3-literals)
  - [3.1 Strings](#31-strings)
  - [3.2 Numbers](#32-numbers)
  - [3.3 Boolean](#33-boolean)
  - [3.4 null](#34-null)";
            });

            AddStep("Markdown Quote", () =>
            {
                markdownContainer.Text = @"> **input**";
            });

            AddStep("Markdown Fenced Code", () =>
            {
                markdownContainer.Text = @"```scriban-html
{{
  x = ""5""   # This assignment will not output anything
  x         # This expression will print 5
  x + 1     # This expression will print 6
}}
```";
            });

            AddStep("Markdown Table", () =>
            {
                markdownContainer.Text =
                    @"|Operator            | Description
|--------------------|------------
| `'left' + <right>` | concatenates left to right string: `""ab"" + ""c"" -> ""abc""`
| `'left' * <right>` | concatenates the left string `right` times: `'a' * 5  -> aaaaa`. left and right and be swapped as long as there is one string and one number.";
            });

            AddStep("Markdown Table (Aligned)", () =>
            {
                markdownContainer.Text =
                    @"| Left-Aligned  | Center Aligned  | Right Aligned |
| :------------ |:---------------:| -----:|
| col 3 is      | some wordy text | $1600 |
| col 2 is      | centered        |   $12 |
| zebra stripes | are neat        |    $1 |";
            });

            AddStep("Markdown Paragraph 1", () =>
            {
                markdownContainer.Text = @"A text enclosed by `{{` and `}}` is a scriban **code block** that will be evaluated by the scriban templating engine.";
            });

            AddStep("Markdown Paragraph 2", () =>
            {
                markdownContainer.Text =
                    @"The greedy mode using the character - (e.g {{- or -}}), removes any whitespace, including newlines Examples with the variable name = ""foo"":";
            });

            AddStep("MarkdownImage", () =>
            {
                markdownContainer.Text = @"![Drag Racing](https://www.wonderplugin.com/videos/demo-image0.jpg)
![Drag Racing](https://www.wonderplugin.com/videos/demo-image0.jpg)
![Drag Racing](https://www.wonderplugin.com/videos/demo-image0.jpg)
![Drag Racing](https://www.wonderplugin.com/videos/demo-image0.jpg)
![Drag Racing](https://www.wonderplugin.com/videos/demo-image0.jpg)";
            });

            AddStep("MarkdownFromInternet", () =>
            {
                try
                {
                    //test readme in https://github.com/lunet-io/scriban/blob/master/doc/language.md#92-if-expression-else-else-if-expression
                    const string url = "https://raw.githubusercontent.com/lunet-io/scriban/master/doc/language.md";
                    var httpClient = new HttpClient();
                    markdownContainer.Text = httpClient.GetStringAsync(url).ConfigureAwait(false).GetAwaiter().GetResult();
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }

            });
        }
    }
}
