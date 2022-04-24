// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using Foundation;
using osu.Framework.Configuration;
using osu.Framework.Graphics.Textures;
using osu.Framework.Graphics.Video;
using osu.Framework.Input;
using osu.Framework.Input.Bindings;
using osu.Framework.Input.Handlers;
using osu.Framework.Input.Handlers.Midi;
using osu.Framework.IO.Stores;
using osu.Framework.iOS.Graphics.Textures;
using osu.Framework.iOS.Graphics.Video;
using osu.Framework.iOS.Input;
using osu.Framework.Platform;
using UIKit;

namespace osu.Framework.iOS
{
    public class IOSGameHost : OsuTKGameHost
    {
        private readonly IOSGameView gameView;
        private IOSKeyboardHandler keyboardHandler;
        private IOSRawKeyboardHandler rawKeyboardHandler;

        public IOSGameHost(IOSGameView gameView)
        {
            this.gameView = gameView;

            NSNotificationCenter.DefaultCenter.AddObserver(UIKeyboard.WillShowNotification, handleKeyboardNotification);
            NSNotificationCenter.DefaultCenter.AddObserver(UIKeyboard.DidHideNotification, handleKeyboardNotification);
        }

        /// <summary>
        /// If the keyboard visibility changes (including the hardware keyboard helper bar) we select the keyboard
        /// handler based on the height of the on-screen keyboard at the end of the animation. If the height is above
        /// an arbitrary value, we decide that the software keyboard handler should be enabled. Otherwise, enable the
        /// raw keyboard handler.
        /// This will also cover the case where there is no first responder, in which case the raw handler will still
        /// successfully catch key events.
        /// </summary>
        private void handleKeyboardNotification(NSNotification notification)
        {
            if (notification.UserInfo == null) return;

            NSValue nsKeyboardFrame = (NSValue)notification.UserInfo[UIKeyboard.FrameEndUserInfoKey];
            RectangleF keyboardFrame = nsKeyboardFrame.RectangleFValue;

            bool softwareKeyboard = keyboardFrame.Height > 120;

            if (keyboardHandler != null)
                keyboardHandler.KeyboardActive = softwareKeyboard;

            if (rawKeyboardHandler != null)
                rawKeyboardHandler.KeyboardActive = !softwareKeyboard;

            gameView.KeyboardTextField.SoftwareKeyboard = softwareKeyboard;
        }

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

        protected override TextInputSource CreateTextInput() => new IOSTextInput(gameView);

        protected override IEnumerable<InputHandler> CreateAvailableInputHandlers() =>
            new InputHandler[]
            {
                new IOSTouchHandler(gameView),
                keyboardHandler = new IOSKeyboardHandler(gameView),
                rawKeyboardHandler = new IOSRawKeyboardHandler(),
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
            => new IOSVideoDecoder(stream);

        public override IEnumerable<KeyBinding> PlatformKeyBindings => new[]
        {
            new KeyBinding(new KeyCombination(InputKey.Super, InputKey.X), PlatformAction.Cut),
            new KeyBinding(new KeyCombination(InputKey.Super, InputKey.C), PlatformAction.Copy),
            new KeyBinding(new KeyCombination(InputKey.Super, InputKey.V), PlatformAction.Paste),
            new KeyBinding(new KeyCombination(InputKey.Super, InputKey.A), PlatformAction.SelectAll),
            new KeyBinding(InputKey.Left, PlatformAction.MoveBackwardChar),
            new KeyBinding(InputKey.Right, PlatformAction.MoveForwardChar),
            new KeyBinding(InputKey.BackSpace, PlatformAction.DeleteBackwardChar),
            new KeyBinding(InputKey.Delete, PlatformAction.DeleteForwardChar),
            new KeyBinding(new KeyCombination(InputKey.Shift, InputKey.Left), PlatformAction.SelectBackwardChar),
            new KeyBinding(new KeyCombination(InputKey.Shift, InputKey.Right), PlatformAction.SelectForwardChar),
            new KeyBinding(new KeyCombination(InputKey.Shift, InputKey.BackSpace), PlatformAction.DeleteBackwardChar),
            new KeyBinding(new KeyCombination(InputKey.Shift, InputKey.Delete), PlatformAction.DeleteForwardChar),
            new KeyBinding(new KeyCombination(InputKey.Alt, InputKey.Left), PlatformAction.MoveBackwardWord),
            new KeyBinding(new KeyCombination(InputKey.Alt, InputKey.Right), PlatformAction.MoveForwardWord),
            new KeyBinding(new KeyCombination(InputKey.Alt, InputKey.BackSpace), PlatformAction.DeleteBackwardWord),
            new KeyBinding(new KeyCombination(InputKey.Alt, InputKey.Delete), PlatformAction.DeleteForwardWord),
            new KeyBinding(new KeyCombination(InputKey.Alt, InputKey.Shift, InputKey.Left), PlatformAction.SelectBackwardWord),
            new KeyBinding(new KeyCombination(InputKey.Alt, InputKey.Shift, InputKey.Right), PlatformAction.SelectForwardWord),
            new KeyBinding(new KeyCombination(InputKey.Super, InputKey.Left), PlatformAction.MoveBackwardLine),
            new KeyBinding(new KeyCombination(InputKey.Super, InputKey.Right), PlatformAction.MoveForwardLine),
            new KeyBinding(new KeyCombination(InputKey.Super, InputKey.BackSpace), PlatformAction.DeleteBackwardLine),
            new KeyBinding(new KeyCombination(InputKey.Super, InputKey.Delete), PlatformAction.DeleteForwardLine),
            new KeyBinding(new KeyCombination(InputKey.Super, InputKey.Shift, InputKey.Left), PlatformAction.SelectBackwardLine),
            new KeyBinding(new KeyCombination(InputKey.Super, InputKey.Shift, InputKey.Right), PlatformAction.SelectForwardLine),
            new KeyBinding(new KeyCombination(InputKey.Alt, InputKey.Super, InputKey.Left), PlatformAction.DocumentPrevious),
            new KeyBinding(new KeyCombination(InputKey.Alt, InputKey.Super, InputKey.Right), PlatformAction.DocumentNext),
            new KeyBinding(new KeyCombination(InputKey.Control, InputKey.Tab), PlatformAction.DocumentNext),
            new KeyBinding(new KeyCombination(InputKey.Control, InputKey.Shift, InputKey.Tab), PlatformAction.DocumentPrevious),
            new KeyBinding(new KeyCombination(InputKey.Delete), PlatformAction.Delete),
        };
    }
}
