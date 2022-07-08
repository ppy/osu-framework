// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Markdig.Syntax.Inlines;
using NUnit.Framework;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Containers.Markdown;
using osu.Framework.Graphics.Sprites;
using osu.Framework.IO.Network;

namespace osu.Framework.Tests.Visual.UserInterface
{
    public class TestSceneMarkdownContainer : FrameworkTestScene
    {
        private TestMarkdownContainer markdownContainer;

        [SetUp]
        public void Setup() => Schedule(() =>
        {
            Child = new BasicScrollContainer
            {
                RelativeSizeAxes = Axes.Both,
                Child = markdownContainer = new TestMarkdownContainer
                {
                    RelativeSizeAxes = Axes.X,
                    AutoSizeAxes = Axes.Y
                }
            };
        });

        [Test]
        public void TestHeading()
        {
            AddStep("Markdown Heading", () =>
            {
                markdownContainer.Text = @"# Header 1
## Header 2
### Header 3
#### Header 4
##### Header 5";
            });
        }

        [Test]
        public void TestSeparator()
        {
            AddStep("Markdown Seperator", () =>
            {
                markdownContainer.Text = @"Line above

---

Line below";
            });
        }

        [Test]
        public void TestUnorderedList()
        {
            AddStep("Markdown Unordered List", () =>
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
        }

        [Test]
        public void TestQuote()
        {
            AddStep("Markdown Quote", () => { markdownContainer.Text = @"> **input**"; });
        }

        [Test]
        public void TestFencedCode()
        {
            AddStep("Markdown Fenced Code", () =>
            {
                markdownContainer.Text = @"```scriban-html

[Escape me]
[[Escape me]]

{{
  x = ""5""   # This assignment will not output anything
  x         # This expression will print 5
  x + 1     # This expression will print 6
}}
```";
            });
        }

        [Test]
        public void TestTable()
        {
            AddStep("Markdown Table", () =>
            {
                markdownContainer.Text =
                    @"|Operator            | Description
|--------------------|------------
| `'left' + <right>` | concatenates left to right string: `""ab"" + ""c"" -> ""abc""`
| `'left' * <right>` | concatenates the left string `right` times: `'a' * 5  -> aaaaa`. left and right and be swapped as long as there is one string and one number.";
            });
        }

        [Test]
        public void TestTableAlignment()
        {
            AddStep("Markdown Table (Aligned)", () =>
            {
                markdownContainer.Text =
                    @"| Left-Aligned  | Center Aligned  | Right Aligned |
| :------------ |:---------------:| -----:|
| col 3 is      | some wordy text | $1600 |
| col 2 is      | centered        |   $12 |
| zebra stripes | are neat        |    $1 |";
            });
        }

        [Test]
        public void TestParagraph()
        {
            AddStep("Markdown Paragraph", () =>
            {
                markdownContainer.Text = @"A text enclosed by `{{` and `}}` is a scriban **code block** that will be evaluated by the scriban templating engine.

The greedy mode using the character - (e.g {{- or -}}), removes any whitespace, including newlines Examples with the variable name = ""foo"":";
            });
        }

        [Test]
        public void TestLink()
        {
            AddStep("MarkdownLink", () => { markdownContainer.Text = @"[click the circles to the beat](https://osu.ppy.sh)"; });
        }

        [Test]
        public void TestImage()
        {
            AddStep("MarkdownImage", () => { markdownContainer.Text = @"![peppy!](https://a.ppy.sh/2)"; });
        }

        [Test]
        public void TestMarkdownFromInternet()
        {
            WebRequest req = null;

            AddStep("MarkdownFromInternet", () =>
            {
                req = new WebRequest("https://raw.githubusercontent.com/ppy/osu-wiki/master/wiki/Skinning/skin.ini/en.md");
                req.Finished += () => Schedule(() => markdownContainer.Text = req.GetResponseString());

                Task.Run(() => req.PerformAsync());
            });

            AddUntilStep("wait for request completed", () => req.Completed);

            AddAssert("ensure content", () => !string.IsNullOrEmpty(markdownContainer.Text));
        }

        [Test]
        public void TestEmphases()
        {
            AddStep("Emphases", () =>
            {
                markdownContainer.Text = @"_italic with underscore_
*italic with asterisk*
__bold with underscore__
**bold with asterisk**
*__italic with asterisk, bold with underscore__*
_**italic with underscore, bold with asterisk**_";
            });
        }

        [Test]
        public void TestLineBreaks()
        {
            AddStep("new lines", () =>
            {
                markdownContainer.Text = @"line 1
soft break\
soft break with '\'";
            });
        }

        [Test]
        public void TestRootRelativeLink()
        {
            AddStep("set content", () =>
            {
                markdownContainer.DocumentUrl = "https://some.test.url/some/path/2";
                markdownContainer.Text = "[link](/file)";
            });

            AddAssert("has correct link", () => markdownContainer.Links[0].Url == "https://some.test.url/file");
        }

        [Test]
        public void TestDocumentRelativeLink()
        {
            AddStep("set content", () => markdownContainer.DocumentUrl = "https://some.test.url/some/path/2");

            AddStep("set 'file'", () => markdownContainer.Text = "[link](file)");
            AddAssert("has correct link", () => markdownContainer.Links[0].Url == "https://some.test.url/some/path/file");

            AddStep("set './file'", () => markdownContainer.Text = "[link](./file)");
            AddAssert("has correct link", () => markdownContainer.Links[1].Url == "https://some.test.url/some/path/file");

            AddStep("set '../folder/file'", () => markdownContainer.Text = "[link](../folder/file)");
            AddAssert("has correct link", () => markdownContainer.Links[2].Url == "https://some.test.url/some/folder/file");
        }

        [Test]
        public void TestDocumentRelativeLinkWithNoUri()
        {
            AddStep("set content", () => { markdownContainer.Text = "[link](file)"; });

            AddAssert("has correct link", () => markdownContainer.Links[0].Url == "file");
        }

        [Test]
        public void TestRootRelativeLinkWithNoUri()
        {
            AddStep("set content", () => { markdownContainer.Text = "[link](/file)"; });

            AddAssert("has correct link", () => markdownContainer.Links[0].Url == "/file");
        }

        [Test]
        public void TestDocumentRelativeLinkWithRootOverride()
        {
            AddStep("set content", () =>
            {
                markdownContainer.DocumentUrl = "https://some.test.url/some/path/2";
                markdownContainer.RootUrl = "https://some.test.url/some/";
                markdownContainer.Text = "[link](file)";
            });

            AddAssert("has correct link", () => markdownContainer.Links[0].Url == "https://some.test.url/some/path/file");
        }

        [Test]
        public void TestRootRelativeLinkWithRootOverride()
        {
            AddStep("set content", () =>
            {
                markdownContainer.DocumentUrl = "https://some.test.url/some/path/2";
                markdownContainer.RootUrl = "https://some.test.url/some/";
                markdownContainer.Text = "[link](/file)";
            });

            AddAssert("has correct link", () => markdownContainer.Links[0].Url == "https://some.test.url/some/file");
        }

        [Test]
        public void TestRootRelativeLinkWithRootOverrideCantEscape()
        {
            AddStep("set content", () =>
            {
                markdownContainer.DocumentUrl = "https://some.test.url/some/path/2";
                markdownContainer.RootUrl = "https://some.test.url/some/";
                markdownContainer.Text = "[link](/../../../file)";
            });

            AddAssert("has correct link", () => markdownContainer.Links[0].Url == "https://some.test.url/file");
        }

        [Test]
        public void TestAbsoluteLinkWithDifferentScheme()
        {
            AddStep("set content", () =>
            {
                markdownContainer.DocumentUrl = "https://some.test.url/some/path/2";
                markdownContainer.RootUrl = "https://some.test.url/some/";
                markdownContainer.Text = "[link](mailto:contact@ppy.sh)";
            });

            AddAssert("has correct link", () => markdownContainer.Links[0].Url == "mailto:contact@ppy.sh");
        }

        [Test]
        public void TestAutoLinkInline()
        {
            AddStep("set content", () =>
            {
                markdownContainer.Text = "<https://discord.gg/ppy>";
            });

            AddAssert("has correct autolink", () => markdownContainer.AutoLinks[0].Url == "https://discord.gg/ppy");
        }

        [Test]
        public void TestUnbalancedFencedBlock()
        {
            AddStep("set unbalanced fenced block", () => markdownContainer.Text = @"```");
        }

        [Test]
        public void TestEmptyFencedBlock()
        {
            AddStep("set empty fenced block", () => markdownContainer.Text = @"```
```");
        }

        private class TestMarkdownContainer : MarkdownContainer
        {
            public new string DocumentUrl
            {
                get => base.DocumentUrl;
                set => base.DocumentUrl = value;
            }

            public new string RootUrl
            {
                get => base.RootUrl;
                set => base.RootUrl = value;
            }

            public readonly List<LinkInline> Links = new List<LinkInline>();

            public readonly List<AutolinkInline> AutoLinks = new List<AutolinkInline>();

            public override MarkdownTextFlowContainer CreateTextFlow() => new TestMarkdownTextFlowContainer
            {
                UrlAdded = url => Links.Add(url),
                AutoLinkAdded = autolink => AutoLinks.Add(autolink),
            };

            public override SpriteText CreateSpriteText() => base.CreateSpriteText().With(t => t.Font = t.Font.With("Roboto", weight: "Regular"));

            private class TestMarkdownTextFlowContainer : MarkdownTextFlowContainer
            {
                public Action<LinkInline> UrlAdded;

                public Action<AutolinkInline> AutoLinkAdded;

                protected override void AddLinkText(string text, LinkInline linkInline)
                {
                    base.AddLinkText(text, linkInline);

                    UrlAdded?.Invoke(linkInline);
                }

                protected override void AddAutoLink(AutolinkInline autolinkInline)
                {
                    base.AddAutoLink(autolinkInline);

                    AutoLinkAdded?.Invoke(autolinkInline);
                }
            }
        }
    }
}
