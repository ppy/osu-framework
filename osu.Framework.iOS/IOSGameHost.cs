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
using osu.Framework.IO.Stores;
using osu.Framework.iOS.Graphics.Textures;
using osu.Framework.iOS.Graphics.Video;
using osu.Framework.iOS.Input;
using osu.Framework.Platform;
using osu.Framework.Threading;
using UIKit;

namespace osu.Framework.iOS
{
    public class IOSGameHost : GameHost
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
            NSValue nsKeyboardFrame = (NSValue)notification.UserInfo[UIKeyboard.FrameEndUserInfoKey];
            RectangleF keyboardFrame = nsKeyboardFrame.RectangleFValue;

            var softwareKeyboard = keyboardFrame.Height > 120;

            if (keyboardHandler != null)
                keyboardHandler.KeyboardActive = softwareKeyboard;

            if (rawKeyboardHandler != null)
                rawKeyboardHandler.KeyboardActive = !softwareKeyboard;

            gameView.KeyboardTextField.SoftwareKeyboard = softwareKeyboard;
        }

        protected override void SetupForRun()
        {
            base.SetupForRun();
            IOSGameWindow.GameView = gameView;
            Window = new IOSGameWindow();
        }

        protected override void SetupConfig(IDictionary<FrameworkSetting, object> gameDefaults)
        {
            base.SetupConfig(gameDefaults);

            DebugConfig.Set(DebugSetting.BypassFrontToBackPass, true);
        }

        protected override void PerformExit(bool immediately)
        {
            // we shouldn't exit on iOS, as Window.Run does not block
        }

        public override bool OnScreenKeyboardOverlapsGameWindow => true;

        protected override bool LimitedMemoryEnvironment => true;

        public override bool CanExit => false;

        public override ITextInputSource GetTextInput() => new IOSTextInput(gameView);

        protected override IEnumerable<InputHandler> CreateAvailableInputHandlers() =>
            new InputHandler[] { new IOSTouchHandler(gameView), keyboardHandler = new IOSKeyboardHandler(gameView), rawKeyboardHandler = new IOSRawKeyboardHandler() };

        protected override Storage GetStorage(string baseName) => new IOSStorage(baseName, this);

        public override void OpenFileExternally(string filename) => throw new NotImplementedException();

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

        public override VideoDecoder CreateVideoDecoder(Stream stream, Scheduler scheduler) => new IOSVideoDecoder(stream, scheduler);

        public override IEnumerable<KeyBinding> PlatformKeyBindings => new[]
        {
            new KeyBinding(new KeyCombination(new[] { InputKey.Super, InputKey.X }), new PlatformAction(PlatformActionType.Cut)),
            new KeyBinding(new KeyCombination(new[] { InputKey.Super, InputKey.C }), new PlatformAction(PlatformActionType.Copy)),
            new KeyBinding(new KeyCombination(new[] { InputKey.Super, InputKey.V }), new PlatformAction(PlatformActionType.Paste)),
            new KeyBinding(new KeyCombination(new[] { InputKey.Super, InputKey.A }), new PlatformAction(PlatformActionType.SelectAll)),
            new KeyBinding(InputKey.Left, new PlatformAction(PlatformActionType.CharPrevious, PlatformActionMethod.Move)),
            new KeyBinding(InputKey.Right, new PlatformAction(PlatformActionType.CharNext, PlatformActionMethod.Move)),
            new KeyBinding(InputKey.BackSpace, new PlatformAction(PlatformActionType.CharPrevious, PlatformActionMethod.Delete)),
            new KeyBinding(InputKey.Delete, new PlatformAction(PlatformActionType.CharNext, PlatformActionMethod.Delete)),
            new KeyBinding(new KeyCombination(new[] { InputKey.Shift, InputKey.Left }), new PlatformAction(PlatformActionType.CharPrevious, PlatformActionMethod.Select)),
            new KeyBinding(new KeyCombination(new[] { InputKey.Shift, InputKey.Right }), new PlatformAction(PlatformActionType.CharNext, PlatformActionMethod.Select)),
            new KeyBinding(new KeyCombination(new[] { InputKey.Shift, InputKey.BackSpace }), new PlatformAction(PlatformActionType.CharPrevious, PlatformActionMethod.Delete)),
            new KeyBinding(new KeyCombination(new[] { InputKey.Shift, InputKey.Delete }), new PlatformAction(PlatformActionType.CharNext, PlatformActionMethod.Delete)),
            new KeyBinding(new KeyCombination(new[] { InputKey.Alt, InputKey.Left }), new PlatformAction(PlatformActionType.WordPrevious, PlatformActionMethod.Move)),
            new KeyBinding(new KeyCombination(new[] { InputKey.Alt, InputKey.Right }), new PlatformAction(PlatformActionType.WordNext, PlatformActionMethod.Move)),
            new KeyBinding(new KeyCombination(new[] { InputKey.Alt, InputKey.BackSpace }), new PlatformAction(PlatformActionType.WordPrevious, PlatformActionMethod.Delete)),
            new KeyBinding(new KeyCombination(new[] { InputKey.Alt, InputKey.Delete }), new PlatformAction(PlatformActionType.WordNext, PlatformActionMethod.Delete)),
            new KeyBinding(new KeyCombination(new[] { InputKey.Alt, InputKey.Shift, InputKey.Left }), new PlatformAction(PlatformActionType.WordPrevious, PlatformActionMethod.Select)),
            new KeyBinding(new KeyCombination(new[] { InputKey.Alt, InputKey.Shift, InputKey.Right }), new PlatformAction(PlatformActionType.WordNext, PlatformActionMethod.Select)),
            new KeyBinding(new KeyCombination(new[] { InputKey.Super, InputKey.Left }), new PlatformAction(PlatformActionType.LineStart, PlatformActionMethod.Move)),
            new KeyBinding(new KeyCombination(new[] { InputKey.Super, InputKey.Right }), new PlatformAction(PlatformActionType.LineEnd, PlatformActionMethod.Move)),
            new KeyBinding(new KeyCombination(new[] { InputKey.Super, InputKey.BackSpace }), new PlatformAction(PlatformActionType.LineStart, PlatformActionMethod.Delete)),
            new KeyBinding(new KeyCombination(new[] { InputKey.Super, InputKey.Delete }), new PlatformAction(PlatformActionType.LineEnd, PlatformActionMethod.Delete)),
            new KeyBinding(new KeyCombination(new[] { InputKey.Super, InputKey.Shift, InputKey.Left }), new PlatformAction(PlatformActionType.LineStart, PlatformActionMethod.Select)),
            new KeyBinding(new KeyCombination(new[] { InputKey.Super, InputKey.Shift, InputKey.Right }), new PlatformAction(PlatformActionType.LineEnd, PlatformActionMethod.Select)),
            new KeyBinding(new KeyCombination(new[] { InputKey.Alt, InputKey.Super, InputKey.Left }), new PlatformAction(PlatformActionType.DocumentPrevious)),
            new KeyBinding(new KeyCombination(new[] { InputKey.Alt, InputKey.Super, InputKey.Right }), new PlatformAction(PlatformActionType.DocumentNext)),
            new KeyBinding(new KeyCombination(new[] { InputKey.Control, InputKey.Tab }), new PlatformAction(PlatformActionType.DocumentNext)),
            new KeyBinding(new KeyCombination(new[] { InputKey.Control, InputKey.Shift, InputKey.Tab }), new PlatformAction(PlatformActionType.DocumentPrevious)),
        };
    }
}
