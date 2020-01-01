// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using NUnit.Framework;
using osu.Framework.Graphics;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Testing;

namespace osu.Framework.Tests.Visual.UserInterface
{
    public class TestSceneClosableMenu : MenuTestScene
    {
        protected override Menu CreateMenu() => new BasicMenu(Direction.Vertical)
        {
            Anchor = Anchor.Centre,
            Origin = Anchor.Centre,
            State = MenuState.Open,
            Items = new[]
            {
                new MenuItem("Item #1")
                {
                    Items = new[]
                    {
                        new MenuItem("Sub-item #1"),
                        new MenuItem("Sub-item #2"),
                    }
                },
                new MenuItem("Item #2")
                {
                    Items = new[]
                    {
                        new MenuItem("Sub-item #1"),
                        new MenuItem("Sub-item #2"),
                    }
                },
            }
        };

        [Test]
        public void TestClickItemClosesMenus()
        {
            AddStep("click item", () => ClickItem(0, 0));
            AddStep("click item", () => ClickItem(1, 0));
            AddAssert("all menus closed", () =>
            {
                for (int i = 1; i >= 0; --i)
                {
                    if (Menus.GetSubMenu(i).State == MenuState.Open)
                        return false;
                }

                return true;
            });
        }
    }
}
