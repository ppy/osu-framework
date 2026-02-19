// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Configuration;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Platform;
using osuTK;

namespace osu.Framework.Tests.Visual.Platform
{
    [Ignore("This test cannot be run in headless mode (a renderer is required).")]
    public partial class TestSceneRenderer : FrameworkTestScene
    {
        [Resolved]
        private GameHost host { get; set; } = null!;

        [Resolved]
        private FrameworkConfigManager config { get; set; } = null!;

        [SetUp]
        public void SetUp() => Schedule(() =>
        {
            Add(new SpriteText
            {
                Text = $"Renderer: {host.ResolvedRenderer} ({host.Renderer.GetType().Name} / {host.Window.GraphicsSurface.Type})",
                Font = FrameworkFont.Regular.With(size: 24),
            });

            Add(new FillFlowContainer
            {
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                AutoSizeAxes = Axes.Both,
                Direction = FillDirection.Vertical,
                Spacing = new Vector2(10f),
                Children = new Drawable[]
                {
                    new SpriteText
                    {
                        Anchor = Anchor.CentreLeft,
                        Origin = Anchor.CentreLeft,
                        Text = "Renderer",
                    },
                    new BasicDropdown<RendererType>
                    {
                        Anchor = Anchor.CentreLeft,
                        Origin = Anchor.CentreLeft,
                        Items = host.GetPreferredRenderersForCurrentPlatform().OrderBy(t => t),
                        Current = config.GetBindable<RendererType>(FrameworkSetting.Renderer),
                        Width = 200f,
                    },
                    new SpriteText
                    {
                        Anchor = Anchor.CentreLeft,
                        Origin = Anchor.CentreLeft,
                        Text = "Render Scale",
                    },
                    new BasicSliderBar<float>
                    {
                        Anchor = Anchor.CentreLeft,
                        Origin = Anchor.CentreLeft,
                        Current = config.GetBindable<float>(FrameworkSetting.RenderScale),
                        Size = new Vector2(200f, 30f),
                    }
                }
            });
        });
    }
}
