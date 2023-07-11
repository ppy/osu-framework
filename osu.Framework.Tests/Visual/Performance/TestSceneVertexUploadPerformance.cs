// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Utils;
using osuTK;
using osuTK.Graphics;

namespace osu.Framework.Tests.Visual.Performance
{
    public partial class TestSceneVertexUploadPerformance : PerformanceTestScene
    {
        private int count;

        private FillFlowContainer<CircularProgress>? fill;

        protected override void LoadComplete()
        {
            base.LoadComplete();

            AddSliderStep("count", 1, 1000, 400, v =>
            {
                count = v;
                recreate();
            });
        }

        private void recreate()
        {
            Child = fill = new FillFlowContainer<CircularProgress>
            {
                RelativeSizeAxes = Axes.X,
                Width = 0.5f,
                AutoSizeAxes = Axes.Y,
                Direction = FillDirection.Full,
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
            };

            for (int i = 0; i < count; i++)
            {
                fill.Add(new CircularProgress
                {
                    Size = new Vector2(10),
                    Current = { Value = 1 }
                });
            }
        }

        protected override void Update()
        {
            base.Update();

            var col = Interpolation.ValueAt((MathF.Sin((float)Time.Current / 1000) + 1) / 2, Color4.Red, Color4.SkyBlue, 0f, 1f);

            if (fill != null)
            {
                fill.Colour = col;
                fill.Rotation += (float)Time.Elapsed * 0.001f;
            }
        }
    }
}
