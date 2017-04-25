// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using OpenTK;
using OpenTK.Graphics;
using osu.Framework.Graphics;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Testing;

namespace osu.Framework.VisualTests.Tests
{
    internal class TestCaseCircularProgress : TestCase
    {
        public override string Description => @"Circular progress bar";

        private CircularProgress clock1;
        private CircularProgress clock2;
        private CircularProgress clock3;

        public override void Reset()
        {
            base.Reset();

            Children = new Drawable[]
            {
                new Container
                {
                    Depth = 3,

                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,

                    Width = 320,
                    Height = 320,
                    CornerRadius = 8,

                    Masking = true,

                    Children = new Drawable[]
                    {
                        new Box
                        {
                            Anchor = Anchor.Centre,
                            Origin = Anchor.Centre,
                            RelativeSizeAxes = Axes.Both,
                            Colour = new Color4(100, 100, 100, 255),
                        },
                        clock1 = new CircularProgress
                        {
                            Anchor = Anchor.Centre,
                            Origin = Anchor.Centre,

                            Width = 300,
                            Height = 300,
                            Colour = new Color4(128, 255, 128, 255),
                        },
                    },
                },
                clock2 = new CircularProgress
                {
                    Anchor = Anchor.TopLeft,
                    Origin = Anchor.TopLeft,
                    Position = new Vector2(20, 20),

                    Width = 100,
                    Height = 100,
                },
                clock3 = new CircularProgress
                {
                    Anchor = Anchor.TopLeft,
                    Origin = Anchor.TopLeft,
                    Position = new Vector2(220, 20),

                    Width = 100,
                    Height = 100,

                    Scale = new Vector2(-0.6f, 1),
                },
            };

            //AddStep("Right to left", () => graph.Direction = BarDirection.RightToLeft);
        }

        protected override void Update()
        {
            base.Update();
            clock1.Current.Value = Time.Current % 500 / 500;
            clock2.Current.Value = Time.Current % 730 / 730;
            clock3.Current.Value = Time.Current % 800 / 800;
        }
    }
}
