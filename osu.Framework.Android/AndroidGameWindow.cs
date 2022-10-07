// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using osu.Framework.Bindables;
using osu.Framework.Configuration;
using osu.Framework.Platform;
using osuTK;
using osuTK.Graphics;

namespace osu.Framework.Android
{
    public class AndroidGameWindow : OsuTKWindow
    {
        private readonly AndroidGameView view;

        public override IGraphicsContext Context => view.GraphicsContext;

        public override bool Focused => IsActive.Value;

        public override IBindable<bool> IsActive { get; }

        public override Platform.WindowState WindowState
        {
            get => Platform.WindowState.Normal;
            set { }
        }

        public event Action? CursorStateChanged;

        public override CursorState CursorState
        {
            get => base.CursorState;
            set
            {
                // cursor should always be confined on mobile platforms, to have UserInputManager confine the cursor to window bounds
                base.CursorState = value | CursorState.Confined;
                CursorStateChanged?.Invoke();
            }
        }

        public AndroidGameWindow(AndroidGameView view)
            : base(view)
        {
            this.view = view;
            IsActive = view.Activity.IsActive.GetBoundCopy();
        }

        public override void SetupWindow(FrameworkConfigManager config)
        {
            CursorState |= CursorState.Confined;
            SafeAreaPadding.BindTo(view.SafeAreaPadding);
        }

        protected override IEnumerable<WindowMode> DefaultSupportedWindowModes => new[]
        {
            Configuration.WindowMode.Fullscreen,
        };

        public override void Run()
        {
            view.Run();
        }

        protected override DisplayDevice CurrentDisplayDevice
        {
            get => DisplayDevice.Default;
            set => throw new InvalidOperationException();
        }
    }
}
