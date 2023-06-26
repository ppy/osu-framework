// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.ComponentModel;
using osu.Framework.Graphics;
using osuTK;

namespace osu.Framework.Tests.Visual.Performance
{
    [Description("tests blending overhead")]
    public partial class TestSceneBlending : TestSceneTextures
    {
        private BlendingParameters blendingParameters;

        protected override void LoadComplete()
        {
            base.LoadComplete();

            Flow.Spacing = new Vector2(-20f);

            AddStep("disable blending", () => blendingParameters = BlendingParameters.None);
            AddStep("set additive blending", () => blendingParameters = BlendingParameters.Additive);
            AddStep("set mixture blending", () => blendingParameters = BlendingParameters.Mixture);
        }

        protected override Drawable CreateDrawable()
        {
            var drawable = base.CreateDrawable();
            drawable.Alpha = 0.9f;
            drawable.Blending = blendingParameters;
            return drawable;
        }
    }
}
