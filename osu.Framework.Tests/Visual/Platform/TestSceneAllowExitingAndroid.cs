// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Input.Events;
using osu.Framework.Platform;
using osuTK.Graphics;
using osuTK.Input;

namespace osu.Framework.Tests.Visual.Platform
{
    public class TestSceneAllowExitingAndroid : FrameworkTestScene
    {
        [Resolved]
        private GameHost host { get; set; }

        private readonly BindableBool allowExit = new BindableBool(true);

        public TestSceneAllowExitingAndroid()
        {
            Children = new Drawable[]
            {
                new ExitVisualiser
                {
                    Width = 0.5f,
                    RelativeSizeAxes = Axes.Both,
                },
                new EscapeVisualizer
                {
                    Colour = Color4.Black,
                    RelativeSizeAxes = Axes.Both,
                    Width = 0.5f,
                    Anchor = Anchor.TopRight,
                    Origin = Anchor.TopRight,
                },
            };
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();
            host.AllowExitingAndroid.AddSource(allowExit);
        }

        [Test]
        public void TestToggleSuspension()
        {
            AddToggleStep("toggle allow exit", v => allowExit.Value = v);
        }

        protected override void Dispose(bool isDisposing)
        {
            host.AllowExitingAndroid.RemoveSource(allowExit);
            base.Dispose(isDisposing);
        }

        private class ExitVisualiser : Box
        {
            private readonly IBindable<bool> allowExit = new Bindable<bool>();

            [BackgroundDependencyLoader]
            private void load(GameHost host)
            {
                allowExit.BindTo(host.AllowExitingAndroid.Result);
                allowExit.BindValueChanged(v => Colour = v.NewValue ? Color4.Green : Color4.Red, true);
            }
        }

        private class EscapeVisualizer : Box
        {
            protected override bool OnKeyDown(KeyDownEvent e)
            {
                if (e.Key == Key.Escape)
                    this.FlashColour(Color4.Blue, 700, Easing.OutQuart);

                return base.OnKeyDown(e);
            }
        }
    }
}
