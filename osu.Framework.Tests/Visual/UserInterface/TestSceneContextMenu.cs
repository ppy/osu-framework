// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Cursor;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Testing;
using osuTK;
using osuTK.Graphics;

namespace osu.Framework.Tests.Visual.UserInterface
{
    public class TestSceneContextMenu : TestScene
    {
        public readonly ContextMenuBox MovingBox;

        public readonly TestContextMenuContainer ContextContainer;

        private ContextMenuBox makeBox(Anchor anchor) =>
            new ContextMenuBox
            {
                Size = new Vector2(200),
                Anchor = anchor,
                Origin = anchor,
                Children = new Drawable[]
                {
                    new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = Color4.Blue,
                    }
                }
            };

        public TestSceneContextMenu()
        {
            Add(ContextContainer = new TestContextMenuContainer
            {
                RelativeSizeAxes = Axes.Both,
                Children = new[]
                {
                    makeBox(Anchor.TopLeft),
                    makeBox(Anchor.TopRight),
                    makeBox(Anchor.BottomLeft),
                    makeBox(Anchor.BottomRight),
                    MovingBox = makeBox(Anchor.Centre),
                }
            });
        }

        public class TestContextMenuContainer : BasicContextMenuContainer
        {
            public Menu CurrentMenu;

            protected override Menu CreateMenu() => CurrentMenu = base.CreateMenu();
        }

        public class ContextMenuBox : Container, IHasContextMenu
        {
            public MenuItem[] ContextMenuItems => new[]
            {
                new MenuItem(@"Change width", () => this.ResizeWidthTo(Width * 2, 100, Easing.OutQuint)),
                new MenuItem(@"Change height", () => this.ResizeHeightTo(Height * 2, 100, Easing.OutQuint)),
                new MenuItem(@"Change width back", () => this.ResizeWidthTo(Width / 2, 100, Easing.OutQuint)),
                new MenuItem(@"Change height back", () => this.ResizeHeightTo(Height / 2, 100, Easing.OutQuint)),
            };
        }
    }
}
