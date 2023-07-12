// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Graphics;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Utils;
using osuTK;
using osuTK.Graphics;

namespace osu.Framework.Tests.Visual.Performance
{
    public partial class TestSceneVertexUploadPerformance : TestSceneBoxPerformance
    {
        protected override Drawable CreateBox() =>
            new CircularProgress
            {
                Size = new Vector2(10),
                Current = { Value = 1 }
            };

        protected override void Update()
        {
            base.Update();

            var col = Interpolation.ValueAt((MathF.Sin((float)Time.Current / 1000) + 1) / 2, Color4.Red, Color4.SkyBlue, 0f, 1f);

            Flow.Colour = col;
        }
    }
}
