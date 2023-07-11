// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Configuration;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osuTK;

namespace osu.Framework.Tests.Visual.Performance
{
    public abstract partial class PerformanceTestScene : FrameworkTestScene
    {
        protected override Container<Drawable> Content => content;

        private BufferedContainer content = null!;

        [Resolved]
        private FrameworkDebugConfigManager debugConfig { get; set; } = null!;

        private Bindable<bool> bypassFrontToBack = null!;

        protected override void LoadComplete()
        {
            base.LoadComplete();

            bypassFrontToBack = debugConfig.GetBindable<bool>(DebugSetting.BypassFrontToBackPass);

            base.Content.Child = content = new BufferedContainer
            {
                RelativeSizeAxes = Axes.Both,
            };

            AddLabel("General");
            AddToggleStep("hide content", v => Content.Alpha = v ? 0 : 1);
            AddToggleStep("bypass front to back", v => bypassFrontToBack.Value = v);
            AddSliderStep("render scale", 0.01f, 1f, 1f, v => content.FrameBufferScale = new Vector2(v));
        }
    }
}
