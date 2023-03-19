// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Configuration;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Platform;

namespace osu.Framework.Tests.Visual.Platform
{
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

            Add(new BasicDropdown<RendererType>
            {
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                Current = config.GetBindable<RendererType>(FrameworkSetting.Renderer),
                Items = host.GetValidRenderersForCurrentPlatform(),
                Width = 200f,
            });
        });
    }
}
