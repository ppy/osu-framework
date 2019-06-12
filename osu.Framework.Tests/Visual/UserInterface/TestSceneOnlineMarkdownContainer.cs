// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using Markdig.Syntax.Inlines;
using NUnit.Framework;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Containers.Markdown;

namespace osu.Framework.Tests.Visual.UserInterface
{
    public class TestSceneOnlineMarkdownContainer : FrameworkTestScene
    {
        private TestOnlineMarkdownContainer markdownContainer;

        [SetUp]
        public void Setup() => Schedule(() =>
        {
            Child = new ScrollContainer
            {
                RelativeSizeAxes = Axes.Both,
                Child = markdownContainer = new TestOnlineMarkdownContainer
                {
                    RelativeSizeAxes = Axes.X,
                    AutoSizeAxes = Axes.Y
                }
            };
        });

        [Test]
        public void TestNoRootUrlOverride()
        {
            AddStep("load document", () => markdownContainer.Url = "https://raw.githubusercontent.com/ppy/osu-wiki/master/wiki/Skinning/en.md");

            AddUntilStep("content loaded", () => !string.IsNullOrEmpty(markdownContainer.Text));
            AddAssert("all links converted to absolute", () => markdownContainer.Links.Any(l => l.Url.StartsWith("https://raw.githubusercontent.com/wiki/")));
        }

        [Test]
        public void TestOverrideRootUrl()
        {
            AddStep("load document", () =>
            {
                markdownContainer.Url = "https://raw.githubusercontent.com/ppy/osu-wiki/master/wiki/Skinning/en.md";
                markdownContainer.RootUrl = "https://raw.githubusercontent.com/ppy/osu-wiki/master/";
            });

            AddUntilStep("content loaded", () => !string.IsNullOrEmpty(markdownContainer.Text));
            AddAssert("all links converted to absolute", () => markdownContainer.Links.TrueForAll(l => l.Url.StartsWith("https://raw.githubusercontent.com/ppy/osu-wiki/master/")));
        }

        [Test]
        public void TestChangeDocument()
        {
            AddStep("load document", () =>
            {
                markdownContainer.Url = "https://raw.githubusercontent.com/ppy/osu-wiki/master/wiki/Skinning/en.md";
                markdownContainer.RootUrl = "https://raw.githubusercontent.com/ppy/osu-wiki/master/";
            });

            AddUntilStep("content loaded", () => !string.IsNullOrEmpty(markdownContainer.Text));

            AddStep("load new document", () =>
            {
                markdownContainer.Text = string.Empty;
                markdownContainer.Url = "https://raw.githubusercontent.com/ppy/osu-wiki/master/wiki/osu!tourney/en.md";
            });

            AddUntilStep("content loaded", () => !string.IsNullOrEmpty(markdownContainer.Text));
        }

        private class TestOnlineMarkdownContainer : OnlineMarkdownContainer
        {
            public readonly List<LinkInline> Links = new List<LinkInline>();

            public override MarkdownTextFlowContainer CreateTextFlow() => new TestMarkdownTextFlowContainer
            {
                UrlAdded = url => Links.Add(url)
            };

            private class TestMarkdownTextFlowContainer : MarkdownTextFlowContainer
            {
                public Action<LinkInline> UrlAdded;

                protected override void AddLinkText(string text, LinkInline linkInline)
                {
                    base.AddLinkText(text, linkInline);

                    UrlAdded?.Invoke(linkInline);
                }
            }
        }
    }
}
