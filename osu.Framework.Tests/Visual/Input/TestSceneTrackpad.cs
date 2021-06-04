// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Platform;
using osu.Framework.Logging;
using osuTK;
using osuTK.Graphics;

namespace osu.Framework.Tests.Visual.Input
{
    public class TestSceneTrackpad : FrameworkTestScene
    {
        TrackpadHandler handler;
        public TestSceneTrackpad()
        {
            var flow = new FillFlowContainer
            {
                RelativeSizeAxes = Axes.Both,
            };
            handler = new TrackpadHandler();

            flow.Add(handler);

            Child = flow;
        }

        [Resolved]
        private GameHost host { get; set; }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            AddToggleStep("toggle trackpad handler", enabled =>
            {
                ((SDL2DesktopWindow)host.Window).TrackpadPositionChanged += (point) =>
                {
                    handler.Circle.X = point.X * Child.DrawWidth;
                    handler.Circle.Y = (1 - point.Y) * Child.DrawHeight;
                };
            });
        }

        private class TrackpadHandler : CompositeDrawable
        {
            private readonly Drawable background;
            public readonly Circle Circle;

            public override bool HandleNonPositionalInput => true;

            public TrackpadHandler()
            {
                Size = new Vector2(50);

                InternalChildren = new[]
                {
                    background = new Container
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = Color4.DarkGreen,
                        Alpha = 0,
                        Child = new Box { RelativeSizeAxes = Axes.Both }
                    },
                    Circle = new Circle()
                    {
                        Colour = Color4.Aqua,
                        Width = 40,
                        Height = 40
                    }
                };
            }
        }
    }
}
