// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Runtime.Versioning;
using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Platform;

namespace osu.Framework.Tests.Visual.Platform
{
    [Ignore("This test cannot run in headless mode (a window instance is required).")]
    [SupportedOSPlatform("windows")]
    [SupportedOSPlatform("linux")]
    [SupportedOSPlatform("macos")]
    public partial class TestSceneWindowFlash : FrameworkTestScene
    {
        private IBindable<bool> isActive = null!;
        private IWindow? window;
        private SpriteText text = null!;
        private readonly Bindable<bool> flashUntilFocused = new BindableBool();

        [BackgroundDependencyLoader]
        private void load(GameHost gameHost)
        {
            isActive = gameHost.IsActive.GetBoundCopy();
            window = gameHost.Window;
            Child = new FillFlowContainer
            {
                Direction = FillDirection.Vertical,
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                Children = new Drawable[]
                {
                    text = new SpriteText
                    {
                        Text = "This window will flash as soon as you un-focus it.",
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre
                    },
                }
            };
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            flashUntilFocused.BindValueChanged(e =>
            {
                window?.CancelFlash();
                text.Text = "This window will flash "
                            + (e.NewValue ? "continuously, until focused again, " : "briefly")
                            + " as soon as it is unfocused.";
            }, true);

            isActive.BindValueChanged(e =>
            {
                if (!e.NewValue)
                    window?.Flash(flashUntilFocused.Value);
            }, true);
        }

        [Test]
        public void TestBasic()
        {
            AddToggleStep("Flash until focused", a => flashUntilFocused.Value = a);
        }
    }
}
