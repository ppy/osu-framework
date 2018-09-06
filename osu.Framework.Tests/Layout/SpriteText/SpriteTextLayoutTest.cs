// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using NUnit.Framework;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Colour;
using osu.Framework.Graphics.Containers;
using OpenTK;
using OpenTK.Graphics;

namespace osu.Framework.Tests.Layout.SpriteText
{
    [TestFixture]
    public class SpriteTextLayoutTest : LayoutTest
    {
        [Test]
        public void TestInitiallyInvalid()
        {
            var text = new TestSpriteText();

            Assert.IsFalse(text.CharactersCache.IsValid);
            Assert.IsFalse(text.ScreenSpaceCharactersCache.IsValid);
            Assert.IsFalse(text.ShadowOffsetCache.IsValid);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void TestValidatedOnFirstFrame(bool withShadow)
        {
            var text = new TestSpriteText
            {
                Text = "A",
                Shadow = withShadow
            };

            Run(text, i =>
            {
                if (i == 0)
                    return false;

                Assert.IsTrue(text.CharactersCache.IsValid);
                Assert.IsTrue(text.ScreenSpaceCharactersCache.IsValid);
                Assert.AreEqual(withShadow, text.ShadowOffsetCache.IsValid);
                return true;
            });
        }

        private static readonly object[] property_cases =
        {
            new object[] { nameof(TestSpriteText.Anchor), Anchor.Centre, false, true, false },
            new object[] { nameof(TestSpriteText.Origin), Anchor.Centre, false, true, false },
            new object[] { nameof(TestSpriteText.Text), "B", true, true, false },
            new object[] { nameof(TestSpriteText.TextSize), 40, true, true, true },
            new object[] { nameof(TestSpriteText.RelativeSizeAxes), Axes.Both, true, true, false },
            new object[] { nameof(TestSpriteText.Width), 40, true, true, false },
            new object[] { nameof(TestSpriteText.Height), 40, true, true, false },
            new object[] { nameof(TestSpriteText.Size), new Vector2(40), true, true, false },
            new object[] { nameof(TestSpriteText.Scale), new Vector2(40), false, true, false },
            new object[] { nameof(TestSpriteText.Shear), new Vector2(40), false, true, false },
            new object[] { nameof(TestSpriteText.Padding), new MarginPadding(10), true, true, false },
            new object[] { nameof(TestSpriteText.Margin), new MarginPadding(10), false, true, false },
            new object[] { nameof(TestSpriteText.Spacing), new Vector2(10), true, true, false },
            new object[] { nameof(TestSpriteText.Font), "newFont", true, true, false },
            new object[] { nameof(TestSpriteText.AllowMultiline), false, true, true, false },
            new object[] { nameof(TestSpriteText.UseFullGlyphHeight), false, true, true, false },
            new object[] { nameof(TestSpriteText.FixedWidth), true, true, true, false },
        };

        [Test, TestCaseSource(nameof(property_cases))]
        public void TestInvalidatedWhenPropertyChanges(string property, object value, bool shouldInvalidateCharacters, bool shouldInvalidateScreenSpaceCharacters, bool shouldInvalidateShadow)
        {
            var text = new TestSpriteText { Text = "A" };

            Run(text, i =>
            {
                switch (i)
                {
                    default:
                        return false;
                    case 1:
                        text.Set(property, value);

                        Assert.AreEqual(!shouldInvalidateCharacters, text.CharactersCache.IsValid);
                        Assert.AreEqual(!shouldInvalidateScreenSpaceCharacters, text.ScreenSpaceCharactersCache.IsValid);
                        Assert.AreEqual(!shouldInvalidateShadow, text.ShadowOffsetCache.IsValid);
                        return true;
                }
            });
        }

        [TestCase(false)]
        [TestCase(true)]
        public void TestInvalidatedWhenParentChanges(bool relativeSize)
        {
            var text = new TestSpriteText
            {
                Text = "A",
                RelativeSizeAxes = relativeSize ? Axes.X : Axes.None
            };

            var firstParent = new Container { Child = text };
            var secondParent = new Container();

            Run(new Container { Children = new[] { firstParent, secondParent } }, i =>
            {
                switch (i)
                {
                    default:
                        return false;
                    case 1:
                        firstParent.Remove(text);
                        secondParent.Add(text);

                        // Characters should only be invalidated if the size has possibly changed,
                        // which can only happen if relatively sizing
                        Assert.AreEqual(!relativeSize, text.CharactersCache.IsValid);

                        Assert.IsFalse(text.ScreenSpaceCharactersCache.IsValid);
                        Assert.IsFalse(text.ShadowOffsetCache.IsValid);

                        return true;
                }
            });
        }

        [TestCase(false)]
        [TestCase(true)]
        public void TestInvalidatedWhenSuperParentChanges(bool relativeSize)
        {
            var text = new TestSpriteText
            {
                Text = "A",
                RelativeSizeAxes = relativeSize ? Axes.X : Axes.None
            };

            var textParent = new Container
            {
                Child = text,
                RelativeSizeAxes = text.RelativeSizeAxes
            };

            var firstParent = new Container { Child = textParent };
            var secondParent = new Container();

            Run(new Container { Children = new[] { firstParent, secondParent } }, i =>
            {
                switch (i)
                {
                    default:
                        return false;
                    case 1:
                        firstParent.Remove(textParent);
                        secondParent.Add(textParent);

                        // Characters should only be invalidated if the size has possibly changed,
                        // which can only happen if relatively sizing
                        Assert.AreEqual(!relativeSize, text.CharactersCache.IsValid);

                        Assert.IsFalse(text.ScreenSpaceCharactersCache.IsValid);
                        Assert.IsFalse(text.ShadowOffsetCache.IsValid);

                        return true;
                }
            });
        }

        private static readonly object[] parent_property_cases =
        {
            // These are almost all going to invalidate screen space characters + shadows, since they depend on DrawInfo
            // Although it's probably not necessary for them to be invalidated unless a non-DrawSize/DrawScale component of DrawInfo is changed
            new object[] { true, nameof(Container.RelativeSizeAxes), Axes.Both, true, true, true },
            new object[] { false, nameof(Container.RelativeSizeAxes), Axes.Both, false, true, true },
            new object[] { false, nameof(Container.Scale), new Vector2(2), false, true, true },
            new object[] { false, nameof(Container.Rotation), 45f, false, true, true },
            new object[] { false, nameof(Container.Colour), (ColourInfo)Color4.Red, false, false, false },
            new object[] { false, nameof(Container.AutoSizeAxes), Axes.Both, false, false, false },
            new object[] { true, nameof(Container.Width), 40, true, true, true },
            new object[] { false, nameof(Container.Width), 40, false, true, true },
            new object[] { true, nameof(Container.Height), 40, true, true, true },
            new object[] { false, nameof(Container.Height), 40, false, true, true },
            new object[] { true, nameof(Container.Size), new Vector2(40), true, true, true },
            new object[] { false, nameof(Container.Size), new Vector2(40), false, true, true },
            // new object[] { true, nameof(Container.Padding), new MarginPadding(10), true, true, true }, // Todo: Broken due to CompositeDrawable's implementation
            // new object[] { true, nameof(Container.Margin), new MarginPadding(10), true, true, true }, // Todo: Broken due to CompositeDrawable's implementation
            new object[] { true, nameof(Container.Position), new Vector2(40), false, true, true },
            new object[] { false, nameof(Container.Position), new Vector2(40), false, true, true },
        };

        [Test, TestCaseSource(nameof(parent_property_cases))]
        public void TestInvalidatedWhenParentPropertyChanges(bool relativeSize, string property, object value, bool shouldInvalidateCharacters, bool shouldInvalidateScreenSpaceCharacters,
                                                             bool shouldInvalidateShadow)
        {
            var text = new TestSpriteText
            {
                Text = "A",
                RelativeSizeAxes = relativeSize ? Axes.Both : Axes.None
            };

            var textParent = new Container { Child = text };

            Run(textParent, i =>
            {
                switch (i)
                {
                    default:
                        return false;
                    case 1:
                        textParent.Set(property, value);

                        Assert.AreEqual(!shouldInvalidateCharacters, text.CharactersCache.IsValid);
                        Assert.AreEqual(!shouldInvalidateScreenSpaceCharacters, text.ScreenSpaceCharactersCache.IsValid);
                        Assert.AreEqual(!shouldInvalidateShadow, text.ShadowOffsetCache.IsValid);
                        return true;
                }
            });
        }
    }
}
