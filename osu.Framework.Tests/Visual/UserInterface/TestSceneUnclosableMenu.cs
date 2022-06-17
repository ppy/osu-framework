// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using NUnit.Framework;
using osu.Framework.Graphics;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Testing;

namespace osu.Framework.Tests.Visual.UserInterface
{
    public class TestSceneUnclosableMenu : MenuTestScene
    {
        [SetUpSteps]
        public void SetUpSteps()
        {
            CreateMenu(() => new TestMenu
            {
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                State = MenuState.Open,
                Items = new[] { new MenuItem("Item #1") { Items = new[] { new MenuItem("Sub-item #1") } } }
            });
        }

        private class TestMenu : BasicMenu
        {
            protected override DrawableMenuItem CreateDrawableMenuItem(MenuItem item) => new TestDrawableMenuItem(item);

            protected override Menu CreateSubMenu() => new TestMenu();

            public TestMenu()
                : base(Direction.Vertical)
            {
            }

            private class TestDrawableMenuItem : BasicDrawableMenuItem
            {
                public override bool CloseMenuOnClick => false;

                public TestDrawableMenuItem(MenuItem item)
                    : base(item)
                {
                }
            }
        }

        [Test]
        public void TestClickMenuUnclosableItem()
        {
            AddStep("click item", () => ClickItem(0, 0));
            AddStep("click item", () => ClickItem(1, 0));
            AddAssert("menu not closed", () =>
                Menus.GetSubMenu(0).State == MenuState.Open &&
                Menus.GetSubMenu(1).State == MenuState.Open);
        }
    }
}
