// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Collections.Generic;
using System.IO;
using Foundation;
using osu.Framework.Configuration;
using osu.Framework.Extensions;
using osu.Framework.Graphics.OpenGL;
using osu.Framework.Graphics.Rendering;
using osu.Framework.Graphics.Textures;
using osu.Framework.Graphics.Video;
using osu.Framework.Input;
using osu.Framework.Input.Bindings;
using osu.Framework.Input.Handlers;
using osu.Framework.Input.Handlers.Joystick;
using osu.Framework.Input.Handlers.Keyboard;
using osu.Framework.Input.Handlers.Midi;
using osu.Framework.Input.Handlers.Mouse;
using osu.Framework.Input.Handlers.Tablet;
using osu.Framework.Input.Handlers.Touch;
using osu.Framework.IO.Stores;
using osu.Framework.iOS.Graphics.Textures;
using osu.Framework.iOS.Graphics.Video;
using osu.Framework.Platform;
using osu.Framework.Platform.MacOS;
using UIKit;

namespace osu.Framework.iOS
{
    public class IOSGameHost : GameHost
    {
        public IOSGameHost()
            : base(string.Empty)
        {
        }

        protected override IRenderer CreateGLRenderer() => new IOSGLRenderer();

        protected override void SetupForRun()
        {
            base.SetupForRun();

            AllowScreenSuspension.Result.BindValueChanged(allow =>
                    InputThread.Scheduler.Add(() => UIApplication.SharedApplication.IdleTimerDisabled = !allow.NewValue),
                true);
        }

        protected override IWindow CreateWindow(GraphicsSurfaceType preferredSurface) => new IOSWindow(preferredSurface, this);

        protected override void SetupConfig(IDictionary<FrameworkSetting, object> defaultOverrides)
        {
            if (!defaultOverrides.ContainsKey(FrameworkSetting.ExecutionMode))
                defaultOverrides.Add(FrameworkSetting.ExecutionMode, ExecutionMode.SingleThread);

            base.SetupConfig(defaultOverrides);

            DebugConfig.SetValue(DebugSetting.BypassFrontToBackPass, true);
        }

        public override bool OnScreenKeyboardOverlapsGameWindow => true;

        public override bool CanExit => false;

        protected override TextInputSource CreateTextInput() => new SDL2WindowTextInput((SDL2Window)Window);

        protected override IEnumerable<InputHandler> CreateAvailableInputHandlers() =>
            // todo: maybe time for SDL2GameHost?
            new InputHandler[]
            {
                new KeyboardHandler(),
                // tablet should get priority over mouse to correctly handle cases where tablet drivers report as mice as well.
                new OpenTabletDriverHandler(),
                new MouseHandler(),
                new TouchHandler(),
                new JoystickHandler(),
                new MidiHandler(),
            };

        public override Storage GetStorage(string path) => new IOSStorage(path, this);

        public override bool OpenFileExternally(string filename) => false;

        public override bool PresentFileExternally(string filename) => false;

        public override void OpenUrlExternally(string url)
        {
            if (!url.CheckIsValidUrl())
                throw new ArgumentException("The provided URL must be one of either http://, https:// or mailto: protocols.", nameof(url));

            UIApplication.SharedApplication.InvokeOnMainThread(() =>
            {
                NSUrl nsurl = NSUrl.FromString(url);
                if (UIApplication.SharedApplication.CanOpenUrl(nsurl))
                    UIApplication.SharedApplication.OpenUrl(nsurl, new NSDictionary(), null);
            });
        }

        public override Clipboard GetClipboard() => new IOSClipboard();

        public override IResourceStore<TextureUpload> CreateTextureLoaderStore(IResourceStore<byte[]> underlyingStore)
            => new IOSTextureLoaderStore(underlyingStore);

        public override VideoDecoder CreateVideoDecoder(Stream stream)
            => new IOSVideoDecoder(Renderer, stream);

        public override IEnumerable<KeyBinding> PlatformKeyBindings => MacOSGameHost.KeyBindings;
    }
}
