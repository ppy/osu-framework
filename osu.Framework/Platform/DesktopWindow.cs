// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Drawing;
using System.IO;
using System.Linq;
using osu.Framework.Bindables;
using osu.Framework.Configuration;
using osu.Framework.Input;
using osu.Framework.Platform.Sdl;
using osu.Framework.Platform.Windows.Native;
using osuTK;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Image = SixLabors.ImageSharp.Image;

namespace osu.Framework.Platform
{
    /// <summary>
    /// Implementation of <see cref="Window"/> used for desktop platforms.
    /// Uses <see cref="Sdl2WindowBackend"/> and <see cref="Sdl2GraphicsBackend"/> by default.
    /// </summary>
    public class DesktopWindow : Window
    {
        private const int default_icon_size = 256;

        private readonly BindableSize sizeFullscreen = new BindableSize();
        private readonly BindableSize sizeWindowed = new BindableSize();
        private readonly BindableDouble windowPositionX = new BindableDouble();
        private readonly BindableDouble windowPositionY = new BindableDouble();
        private readonly Bindable<DisplayIndex> windowDisplayIndex = new Bindable<DisplayIndex>();

        public readonly Bindable<ConfineMouseMode> ConfineMouseMode = new Bindable<ConfineMouseMode>();

        /// <summary>
        /// Gets or sets the window's position on the current screen given a relative value between 0 and 1.
        /// The position is calculated with respect to the window's size such that:
        ///   (0, 0) indicates that the window is aligned to the top left of the screen,
        ///   (1, 1) indicates that the window is aligned to the bottom right of the screen, and
        ///   (0.5, 0.5) indicates that the window is centred on the screen.
        /// </summary>
        protected Vector2 RelativePosition
        {
            get
            {
                var displayBounds = CurrentDisplay.Value.Bounds;
                var windowX = Position.Value.X - displayBounds.X;
                var windowY = Position.Value.Y - displayBounds.Y;
                var windowSize = sizeWindowed.Value;

                return new Vector2(
                    displayBounds.Width > windowSize.Width ? (float)windowX / (displayBounds.Width - windowSize.Width) : 0,
                    displayBounds.Height > windowSize.Height ? (float)windowY / (displayBounds.Height - windowSize.Height) : 0);
            }
            set
            {
                if (WindowMode.Value != Configuration.WindowMode.Windowed)
                    return;

                var displayBounds = CurrentDisplay.Value.Bounds;
                var windowSize = sizeWindowed.Value;
                var windowX = (int)Math.Round((displayBounds.Width - windowSize.Width) * value.X);
                var windowY = (int)Math.Round((displayBounds.Height - windowSize.Height) * value.Y);

                Position.Value = new Point(windowX + displayBounds.X, windowY + displayBounds.Y);
            }
        }

        protected override IWindowBackend CreateWindowBackend() => new Sdl2WindowBackend();
        protected override IGraphicsBackend CreateGraphicsBackend() => new Sdl2GraphicsBackend();

        /// <summary>
        /// Enables or disables <see cref="Window.RelativeMouseMode"/> based on the given <paramref name="position"/>.
        /// If the position is within the window and relative mode is disabled, relative mode will be enabled.
        /// If the position is outside the window and relative mode is enabled, relative mode will be disabled
        /// and the mouse cursor will be warped to <paramref name="position"/>.
        /// <param name="position">The given screen location to check, relative to the top left corner of the window, in scaled coordinates. If null, defaults to current position.</param>
        /// </summary>
        public void UpdateRelativeMode(Vector2? position = null) => WindowBackend.UpdateRelativeMode(position);

        public override void SetupWindow(FrameworkConfigManager config)
        {
            base.SetupWindow(config);

            CurrentDisplay.ValueChanged += evt =>
            {
                windowDisplayIndex.Value = (DisplayIndex)evt.NewValue.Index;
                windowPositionX.Value = 0.5;
                windowPositionY.Value = 0.5;
            };

            config.BindWith(FrameworkSetting.LastDisplayDevice, windowDisplayIndex);
            windowDisplayIndex.BindValueChanged(evt => CurrentDisplay.Value = Displays.ElementAtOrDefault((int)evt.NewValue) ?? PrimaryDisplay, true);

            sizeFullscreen.ValueChanged += evt =>
            {
                if (evt.NewValue.IsEmpty || CurrentDisplay.Value == null)
                    return;

                var mode = CurrentDisplay.Value.FindDisplayMode(evt.NewValue);
                if (mode.Size != System.Drawing.Size.Empty)
                    WindowBackend.CurrentDisplayMode = mode;
            };

            sizeWindowed.ValueChanged += evt =>
            {
                if (evt.NewValue.IsEmpty)
                    return;

                WindowBackend.Size = evt.NewValue;
                Size.Value = evt.NewValue;
            };

            IsActive.ValueChanged += _ => UpdateRelativeMode();

            config.BindWith(FrameworkSetting.SizeFullscreen, sizeFullscreen);
            config.BindWith(FrameworkSetting.WindowedSize, sizeWindowed);

            config.BindWith(FrameworkSetting.WindowedPositionX, windowPositionX);
            config.BindWith(FrameworkSetting.WindowedPositionY, windowPositionY);

            RelativePosition = new Vector2((float)windowPositionX.Value, (float)windowPositionY.Value);

            config.BindWith(FrameworkSetting.WindowMode, WindowMode);
            WindowMode.BindValueChanged(evt => UpdateWindowMode(evt.NewValue), true);

            config.BindWith(FrameworkSetting.ConfineMouseMode, ConfineMouseMode);
            ConfineMouseMode.BindValueChanged(confineMouseModeChanged, true);

            Resized += onResized;
            Moved += onMoved;
        }

        public override void CycleMode()
        {
            var currentValue = WindowMode.Value;

            do
            {
                switch (currentValue)
                {
                    case Configuration.WindowMode.Windowed:
                        currentValue = Configuration.WindowMode.Borderless;
                        break;

                    case Configuration.WindowMode.Borderless:
                        currentValue = Configuration.WindowMode.Fullscreen;
                        break;

                    case Configuration.WindowMode.Fullscreen:
                        currentValue = Configuration.WindowMode.Windowed;
                        break;
                }
            } while (!SupportedWindowModes.Contains(currentValue) && currentValue != WindowMode.Value);

            WindowMode.Value = currentValue;
        }

        protected override void UpdateWindowMode(WindowMode mode)
        {
            base.UpdateWindowMode(mode);

            ConfineMouseMode.TriggerChange();
        }

        public virtual void SetIconFromStream(Stream stream)
        {
            using (var ms = new MemoryStream())
            {
                stream.CopyTo(ms);
                ms.Position = 0;

                var imageInfo = Image.Identify(ms);

                if (imageInfo != null)
                    SetIconFromImage(Image.Load<Rgba32>(ms.GetBuffer()));
                else if (IconGroup.TryParse(ms.GetBuffer(), out var iconGroup))
                    SetIconFromGroup(iconGroup);
            }
        }

        internal virtual void SetIconFromImage(Image<Rgba32> iconImage) => WindowBackend.SetIcon(iconImage);

        internal virtual void SetIconFromGroup(IconGroup iconGroup)
        {
            // LoadRawIcon returns raw PNG data if available, which avoids any Windows-specific pinvokes
            var bytes = iconGroup.LoadRawIcon(default_icon_size, default_icon_size);
            if (bytes == null)
                return;

            SetIconFromImage(Image.Load<Rgba32>(bytes));
        }

        private void onResized()
        {
            if (WindowState.Value == Platform.WindowState.Normal)
            {
                sizeWindowed.Value = WindowBackend.Size;
                Size.Value = sizeWindowed.Value;
                updateWindowPositionConfig();
            }
        }

        private void onMoved(Point point)
        {
            if (WindowState.Value == Platform.WindowState.Normal)
                updateWindowPositionConfig();
        }

        private void updateWindowPositionConfig()
        {
            var relativePosition = RelativePosition;
            windowPositionX.Value = relativePosition.X;
            windowPositionY.Value = relativePosition.Y;
        }

        private void confineMouseModeChanged(ValueChangedEvent<ConfineMouseMode> args)
        {
            bool confine = false;

            switch (args.NewValue)
            {
                case Input.ConfineMouseMode.Fullscreen:
                    confine = WindowMode.Value != Configuration.WindowMode.Windowed;
                    break;

                case Input.ConfineMouseMode.Always:
                    confine = true;
                    break;
            }

            if (confine)
                CursorState.Value |= Platform.CursorState.Confined;
            else
                CursorState.Value &= ~Platform.CursorState.Confined;
        }
    }
}
