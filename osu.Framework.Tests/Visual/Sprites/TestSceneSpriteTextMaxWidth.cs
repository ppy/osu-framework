// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using NUnit.Framework;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Text;
using osuTK.Graphics;

namespace osu.Framework.Tests.Visual.Sprites
{
    public class TestSceneSpriteTextMaxWidth : FrameworkTestScene
    {
        private VisualDisplay display;

        [Test]
        public void TestAutoSizeLessThanMaxWidth()
        {
            createTest(s =>
            {
                s.MaxWidth = 100;
                s.Text = "test";
            });

            AddAssert("size < max", () => display.Text.DrawWidth < 100);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void TestAutoSizeMoreThanMaxWidth(bool truncate)
        {
            createTest(s =>
            {
                s.MaxWidth = 50;
                s.Text = "some very long text that should exceed the max width";
                s.Truncate = truncate;
            });

            AddAssert("size <= max", () => display.Text.DrawWidth <= 50);
        }

        [Test]
        public void TestFixedSizeLessThanMaxWidth()
        {
            createTest(s =>
            {
                s.MaxWidth = 50;
                s.Width = 40;
                s.Text = "test";
            });

            AddAssert("size <= 40", () => display.Text.DrawWidth <= 40);
        }

        [Test]
        public void TestFixedSizeMoreThanMaxWidth()
        {
            createTest(s =>
            {
                s.MaxWidth = 50;
                s.Width = 100;
                s.Text = "test";
            });

            AddAssert("size == 50", () => display.Text.DrawWidth <= 50);
        }

        [Test]
        public void TestMaxWidthWithRelativeSize()
        {
            float textWidth = 0;

            createTest(s =>
            {
                s.RelativeSizeAxes = Axes.X;
                s.MaxWidth = 0.5f;
                s.Text = "some very long text that should exceed the max width";
                s.Truncate = true;
            }, Axes.Y);
            AddStep("store text width", () => textWidth = display.Text.TextBuilder.Bounds.X);

            AddStep("set parent size", () => display.Width = 100);
            AddAssert("size <= max", () => display.Text.DrawWidth <= 50);
            AddAssert("width increased", () => display.Text.TextBuilder.Bounds.X > textWidth);
        }

        private void createTest(Action<SpriteText> initFunc, Axes autoSizeAxes = Axes.Both)
        {
            AddStep("create test", () =>
            {
                Clear();
                Add(display = new VisualDisplay(initFunc, autoSizeAxes));
            });
        }

        private class VisualDisplay : CompositeDrawable
        {
            public readonly TestSpriteText Text;

            public VisualDisplay(Action<SpriteText> initFunc, Axes autoSizeAxes = Axes.Both)
            {
                Anchor = Anchor.Centre;
                Origin = Anchor.Centre;
                AutoSizeAxes = autoSizeAxes;

                InternalChildren = new Drawable[]
                {
                    new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Alpha = 0.2f,
                        Colour = Color4.Pink
                    },
                    Text = new TestSpriteText { AllowMultiline = false }
                };

                initFunc?.Invoke(Text);
            }
        }

        private class TestSpriteText : SpriteText
        {
            public TextBuilder TextBuilder { get; private set; }

            protected override TextBuilder CreateTextBuilder(ITexturedGlyphLookupStore store)
                => TextBuilder = base.CreateTextBuilder(store);
        }
    }
}
