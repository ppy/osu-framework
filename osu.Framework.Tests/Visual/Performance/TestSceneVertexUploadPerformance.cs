// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using osu.Framework.Graphics;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Utils;

namespace osu.Framework.Tests.Visual.Performance
{
    public partial class TestSceneVertexUploadPerformance : RepeatedDrawablePerformanceTestScene
    {
        protected override Drawable CreateDrawable() => new CircularProgress();

        protected override void Update()
        {
            base.Update();

            foreach (var p in Flow.OfType<CircularProgress>())
                p.Current.Value = (p.Current.Value + (Time.Elapsed * RNG.NextSingle()) / 1000) % 1;
        }
    }
}
