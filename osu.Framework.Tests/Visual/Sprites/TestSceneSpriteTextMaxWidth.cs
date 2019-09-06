// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using NUnit.Framework;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.MathUtils;
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

            AddAssert("size is less than max width", () => display.Text.DrawWidth < 100);
        }

        [Test]
        public void TestAutoSizeMoreThanMaxWidth()
        {
            createTest(s =>
            {
                s.MaxWidth = 50;
                s.Text = "some very long text that should exceed the max width";
            });

            AddAssert("size is capped at the max width", () => Precision.AlmostEquals(50, display.Text.DrawWidth));
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

            AddAssert("size == 40", () => Precision.AlmostEquals(40, display.Text.DrawWidth));
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

            AddAssert("size == 50", () => Precision.AlmostEquals(50, display.Text.DrawWidth));
        }

        private void createTest(Action<SpriteText> initFunc)
        {
            AddStep("create test", () =>
            {
                Clear();
                Add(display = new VisualDisplay(initFunc));
            });
        }

        private class VisualDisplay : CompositeDrawable
        {
            public readonly SpriteText Text;

            public VisualDisplay(Action<SpriteText> initFunc)
            {
                Anchor = Anchor.Centre;
                Origin = Anchor.Centre;
                AutoSizeAxes = Axes.Both;

                InternalChildren = new Drawable[]
                {
                    new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Alpha = 0.2f,
                        Colour = Color4.Pink
                    },
                    Text = new SpriteText()
                };

                initFunc?.Invoke(Text);
            }
        }
    }
}
