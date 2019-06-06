﻿// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Cursor;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.UserInterface;
using osuTK;
using osuTK.Graphics;

namespace osu.Framework.Tests.Visual.UserInterface
{
    public class TestSceneContextMenu : FrameworkTestScene
    {
        private const int start_time = 0;
        private const int duration = 1000;

        private readonly ContextMenuBox movingBox;

        public override IReadOnlyList<Type> RequiredTypes => new[]
        {
            typeof(Menu),
            typeof(BasicMenu),
            typeof(ContextMenuContainer),
            typeof(BasicContextMenuContainer)
        };

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
            Add(new BasicContextMenuContainer
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
