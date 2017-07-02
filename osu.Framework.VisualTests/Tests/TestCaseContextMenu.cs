// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using OpenTK;
using OpenTK.Graphics;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Cursor;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Transforms;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Testing;

namespace osu.Framework.VisualTests.Tests
{
    internal class TestCaseContextMenu : TestCase
    {
        public override string Description => @"Menu visible on right click";

        private const int start_time = 0;
        private const int duration = 1000;

        private ContextMenuBox movingBox;

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

        public override void Reset()
        {
            base.Reset();

            Add(makeBox(Anchor.TopLeft));
            Add(makeBox(Anchor.TopRight));
            Add(makeBox(Anchor.BottomLeft));
            Add(makeBox(Anchor.BottomRight));
            Add(movingBox = makeBox(Anchor.Centre));

            movingBox.Transforms.Add(new TransformPosition
            {
                StartValue = Vector2.Zero,
                EndValue = new Vector2(0, 100),
                StartTime = start_time,
                EndTime = start_time + duration,
                LoopCount = -1,
                LoopDelay = duration * 3
            });
            movingBox.Transforms.Add(new TransformPosition
            {
                StartValue = new Vector2(0, 100),
                EndValue = new Vector2(100, 100),
                StartTime = start_time + duration,
                EndTime = start_time + duration * 2,
                LoopCount = -1,
                LoopDelay = duration * 3
            });
            movingBox.Transforms.Add(new TransformPosition
            {
                StartValue = new Vector2(100, 100),
                EndValue = new Vector2(100, 0),
                StartTime = start_time + duration * 2,
                EndTime = start_time + duration * 3,
                LoopCount = -1,
                LoopDelay = duration * 3
            });
            movingBox.Transforms.Add(new TransformPosition
            {
                StartValue = new Vector2(100, 0),
                EndValue = Vector2.Zero,
                StartTime = start_time + duration * 3,
                EndTime = start_time + duration * 4,
                LoopCount = -1,
                LoopDelay = duration * 3
            });

            Add(new ContextMenuContainer());
        }

        private class ContextMenuBox : Container, IHasContextMenu
        {
            public ContextMenuItem[] ContextMenuItems => new []
            {
                new ContextMenuItem(@"Change width") { Action = () => ResizeWidthTo(Width * 2, 100, EasingTypes.OutQuint) },
                new ContextMenuItem(@"Change height") { Action = () => ResizeHeightTo(Height * 2, 100, EasingTypes.OutQuint) },
                new ContextMenuItem(@"Change width back") { Action = () => ResizeWidthTo(Width / 2, 100, EasingTypes.OutQuint) },
                new ContextMenuItem(@"Change height back") { Action = () => ResizeHeightTo(Height / 2, 100, EasingTypes.OutQuint) },
            };
        }
    }
}
