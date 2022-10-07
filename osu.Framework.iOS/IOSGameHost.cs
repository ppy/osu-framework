// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System.Collections.Generic;
using System.IO;
using Foundation;
using osu.Framework.Configuration;
using osu.Framework.Graphics.Rendering;
using osu.Framework.Graphics.Textures;
using osu.Framework.Graphics.Video;
using osu.Framework.Input;
using osu.Framework.Input.Bindings;
using osu.Framework.Input.Handlers;
using osu.Framework.Input.Handlers.Midi;
using osu.Framework.IO.Stores;
using osu.Framework.iOS.Graphics.OpenGL;
using osu.Framework.iOS.Graphics.Textures;
using osu.Framework.iOS.Graphics.Video;
using osu.Framework.iOS.Input;
using osu.Framework.Platform;
using osu.Framework.Platform.MacOS;
using UIKit;

namespace osu.Framework.iOS
{
    public class IOSGameHost : OsuTKGameHost
    {
        private readonly IOSGameView gameView;

        public IOSTextFieldKeyboardHandler TextFieldHandler { get; private set; }

        public IOSGameHost(IOSGameView gameView)
        {
            this.gameView = gameView;
        }

        protected override IRenderer CreateRenderer() => new IOSGLRenderer(gameView);

        protected override void SetupForRun()
        {
            base.SetupForRun();

            AllowScreenSuspension.Result.BindValueChanged(allow =>
                    InputThread.Scheduler.Add(() => UIApplication.SharedApplication.IdleTimerDisabled = !allow.NewValue),
                true);
        }

        protected override IWindow CreateWindow() => new IOSGameWindow(gameView);

        protected override void SetupConfig(IDictionary<FrameworkSetting, object> defaultOverrides)
        {
            if (!defaultOverrides.ContainsKey(FrameworkSetting.ExecutionMode))
                defaultOverrides.Add(FrameworkSetting.ExecutionMode, ExecutionMode.SingleThread);

            base.SetupConfig(defaultOverrides);

            DebugConfig.SetValue(DebugSetting.BypassFrontToBackPass, true);
        }

        public override bool OnScreenKeyboardOverlapsGameWindow => true;

        public override bool CanExit => false;

        protected override TextInputSource CreateTextInput() => new IOSTextInput(this, gameView);

        protected override IEnumerable<InputHandler> CreateAvailableInputHandlers() =>
            new InputHandler[]
            {
                new IOSTouchHandler(gameView),
                TextFieldHandler = new IOSTextFieldKeyboardHandler(gameView),
                new IOSHardwareKeyboardHandler(gameView),
                new IOSMouseHandler(gameView),
                new MidiHandler()
            };

        public override Storage GetStorage(string path) => new IOSStorage(path, this);

        public override bool OpenFileExternally(string filename) => false;

        public override bool PresentFileExternally(string filename) => false;

        public override void OpenUrlExternally(string url)
        {
            UIApplication.SharedApplication.InvokeOnMainThread(() =>
            {
                NSUrl nsurl = NSUrl.FromString(url);
                if (UIApplication.SharedApplication.CanOpenUrl(nsurl))
                    UIApplication.SharedApplication.OpenUrl(nsurl, new NSDictionary(), null);
            });
        }

        public override Clipboard GetClipboard() => new IOSClipboard(gameView);

        public override IResourceStore<TextureUpload> CreateTextureLoaderStore(IResourceStore<byte[]> underlyingStore)
            => new IOSTextureLoaderStore(underlyingStore);

        public override VideoDecoder CreateVideoDecoder(Stream stream)
            => new IOSVideoDecoder(Renderer, stream);

        public override IEnumerable<KeyBinding> PlatformKeyBindings => MacOSGameHost.KeyBindings;
    }
}
