// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Bindables;
using osu.Framework.Configuration;
using osu.Framework.Platform.Sdl;

namespace osu.Framework.Platform
{
    public class DesktopWindow : Window
    {
        private readonly BindableSize sizeWindowed = new BindableSize();

        /// <summary>
        /// Initialises a window for desktop platforms.
        /// Uses <see cref="Sdl2WindowBackend"/> and <see cref="PassthroughGraphicsBackend"/>.
        /// </summary>
        public DesktopWindow()
            : base(new Sdl2WindowBackend(), new PassthroughGraphicsBackend())
        {
        }

        public override void SetupWindow(FrameworkConfigManager config)
        {
            base.SetupWindow(config);

            sizeWindowed.ValueChanged += evt =>
            {
                if (!evt.NewValue.IsEmpty && WindowState.Value == Platform.WindowState.Normal)
                    Size.Value = evt.NewValue;
            };

            config.BindWith(FrameworkSetting.WindowedSize, sizeWindowed);

            Resized += onResized;
        }

        private void onResized()
        {
            if (!Size.Value.IsEmpty && WindowMode.Value == Configuration.WindowMode.Windowed)
                sizeWindowed.Value = Size.Value;
        }
    }
}
