// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Configuration;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Utils;
using osuTK;
using osuTK.Graphics;

namespace osu.Framework.Tests.Visual.Performance
{
    public abstract partial class PerformanceTestScene : FrameworkTestScene
    {
        protected override Container<Drawable> Content => content;

        private Container content = null!;

        private bool rotation;
        private bool cycleColour;

        [Resolved]
        private FrameworkDebugConfigManager debugConfig { get; set; } = null!;

        private Bindable<bool> bypassFrontToBack = null!;

        private BufferedContainer buffer = null!;

        protected override void LoadComplete()
        {
            base.LoadComplete();

            bypassFrontToBack = debugConfig.GetBindable<bool>(DebugSetting.BypassFrontToBackPass);

            base.Content.Child = buffer = new BufferedContainer(pixelSnapping: true)
            {
                RelativeSizeAxes = Axes.Both,
                Child = content = new Container
                {
                    RelativeSizeAxes = Axes.Both,
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                },
            };

            AddLabel("General");

            AddStep("do nothing", () => { });

            AddToggleStep("hide content", v => Content.Alpha = v ? 0 : 1);
            AddToggleStep("enable front to back", v => bypassFrontToBack.Value = !v);
            AddSliderStep("render scale", 0.01f, 1f, 1f, v => buffer.FrameBufferScale = new Vector2(v));
            AddToggleStep("rotate everything", v => rotation = v);
            AddToggleStep("cycle colour", v => cycleColour = v);
        }

        protected override void Update()
        {
            base.Update();

            if (rotation)
            {
                content.Rotation += (float)Time.Elapsed * 0.01f;
                content.Scale = new Vector2(0.5f);
            }
            else
            {
                content.Scale = Vector2.One;
                content.Rotation = 0;
            }

            if (cycleColour)
            {
                var col = Interpolation.ValueAt((MathF.Sin((float)Time.Current / 1000) + 1) / 2, Color4.Red, Color4.SkyBlue, 0f, 1f);

                content.Colour = col;
            }
            else
            {
                content.Colour = Color4.White;
            }
        }
    }
}
