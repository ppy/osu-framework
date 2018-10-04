// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Cursor;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Testing;
using OpenTK;
using OpenTK.Graphics;

namespace osu.Framework.Tests.Visual
{
    public class TestCaseContextMenu : TestCase
    {
        private const int start_time = 0;
        private const int duration = 1000;

        private readonly ContextMenuBox movingBox;

        private ContextMenuBox makeBox(Anchor anchor)
        {
            return new ContextMenuBox
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
        }

        public TestCaseContextMenu()
        {
            Add(new ContextMenuContainer
            {
                RelativeSizeAxes = Axes.Both,
                Children = new[]
                {
                    makeBox(Anchor.TopLeft),
                    makeBox(Anchor.TopRight),
                    makeBox(Anchor.BottomLeft),
                    makeBox(Anchor.BottomRight),
                    movingBox = makeBox(Anchor.Centre),
                }
            });
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            // Move box along a square trajectory
            movingBox.MoveTo(new Vector2(0, 100), duration)
                .Then().MoveTo(new Vector2(100, 100), duration)
                .Then().MoveTo(new Vector2(100, 0), duration)
                .Then().MoveTo(Vector2.Zero, duration)
                .Loop();
        }

        private class ContextMenuBox : Container, IHasContextMenu
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
