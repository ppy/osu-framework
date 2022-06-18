// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System.Linq;
using Foundation;
using osu.Framework.Input.Handlers;
using osu.Framework.Input.StateChanges;
using osu.Framework.Platform;
using osuTK.Input;
using UIKit;

namespace osu.Framework.iOS.Input
{
    public class IOSHardwareKeyboardHandler : InputHandler
    {
        private readonly GameUIApplication application;
        private readonly IOSGameView view;
        private IOSGameHost host;

        public override bool IsActive => true;

        public IOSHardwareKeyboardHandler(IOSGameView view)
        {
            this.view = view;

            application = (GameUIApplication)UIApplication.SharedApplication;
        }

        public override bool Initialize(GameHost host)
        {
            this.host = (IOSGameHost)host;

            view.HandlePresses += handlePresses;
            application.HandleGsKeyEvent += handleGsKeyEvent;
            return true;
        }

        /// <summary>
        /// Handles <see cref="UIPressesEvent"/>s and enqueues corresponding <see cref="KeyboardKeyInput"/>s.
        /// </summary>
        private void handlePresses(NSSet<UIPress> presses, UIPressesEvent evt)
        {
            if (!IsActive)
                return;

            foreach (var press in presses.Cast<UIPress>())
            {
                if (press.Key == null)
                    continue;

                var key = keyFromCode(press.Key.KeyCode);
                if (key == Key.Unknown || host.TextFieldHandler.Handled(key))
                    continue;

                switch (press.Phase)
                {
                    case UIPressPhase.Began:
                        PendingInputs.Enqueue(new KeyboardKeyInput(key, true));
                        break;

                    case UIPressPhase.Ended:
                    case UIPressPhase.Cancelled:
                        PendingInputs.Enqueue(new KeyboardKeyInput(key, false));
                        break;
                }
            }
        }

        /// <summary>
        /// Handles GSEvents and enqueues corresponding <see cref="KeyboardKeyInput"/>s.
        /// </summary>
        /// <remarks>
        /// This is still existing as an alternative to <see cref="handlePresses"/>
        /// for early iOS versions in which <see cref="UIPress.Key"/> is unavailable.
        /// </remarks>
        private void handleGsKeyEvent(int keyCode, bool isDown)
        {
            if (!IsActive)
                return;

            var key = keyFromCode((UIKeyboardHidUsage)keyCode);
            if (key == Key.Unknown || host.TextFieldHandler.Handled(key))
                return;

            PendingInputs.Enqueue(new KeyboardKeyInput(key, isDown));
        }

        protected override void Dispose(bool disposing)
        {
            view.HandlePresses -= handlePresses;
            application.HandleGsKeyEvent -= handleGsKeyEvent;

            base.Dispose(disposing);
        }

        private static Key keyFromCode(UIKeyboardHidUsage usage)
        {
            switch (usage)
            {
                case UIKeyboardHidUsage.KeyboardA:
                    return Key.A;

                case UIKeyboardHidUsage.KeyboardB:
                    return Key.B;

                case UIKeyboardHidUsage.KeyboardC:
                    return Key.C;

                case UIKeyboardHidUsage.KeyboardD:
                    return Key.D;

                case UIKeyboardHidUsage.KeyboardE:
                    return Key.E;

                case UIKeyboardHidUsage.KeyboardF:
                    return Key.F;

                case UIKeyboardHidUsage.KeyboardG:
                    return Key.G;

                case UIKeyboardHidUsage.KeyboardH:
                    return Key.H;

                case UIKeyboardHidUsage.KeyboardI:
                    return Key.I;

                case UIKeyboardHidUsage.KeyboardJ:
                    return Key.J;

                case UIKeyboardHidUsage.KeyboardK:
                    return Key.K;

                case UIKeyboardHidUsage.KeyboardL:
                    return Key.L;

                case UIKeyboardHidUsage.KeyboardM:
                    return Key.M;

                case UIKeyboardHidUsage.KeyboardN:
                    return Key.N;

                case UIKeyboardHidUsage.KeyboardO:
                    return Key.O;

                case UIKeyboardHidUsage.KeyboardP:
                    return Key.P;

                case UIKeyboardHidUsage.KeyboardQ:
                    return Key.Q;

                case UIKeyboardHidUsage.KeyboardR:
                    return Key.R;

                case UIKeyboardHidUsage.KeyboardS:
                    return Key.S;

                case UIKeyboardHidUsage.KeyboardT:
                    return Key.T;

                case UIKeyboardHidUsage.KeyboardU:
                    return Key.U;

                case UIKeyboardHidUsage.KeyboardV:
                    return Key.V;

                case UIKeyboardHidUsage.KeyboardW:
                    return Key.W;

                case UIKeyboardHidUsage.KeyboardX:
                    return Key.X;

                case UIKeyboardHidUsage.KeyboardY:
                    return Key.Y;

                case UIKeyboardHidUsage.KeyboardZ:
                    return Key.Z;

                case UIKeyboardHidUsage.Keyboard1:
                    return Key.Number1;

                case UIKeyboardHidUsage.Keyboard2:
                    return Key.Number2;

                case UIKeyboardHidUsage.Keyboard3:
                    return Key.Number3;

                case UIKeyboardHidUsage.Keyboard4:
                    return Key.Number4;

                case UIKeyboardHidUsage.Keyboard5:
                    return Key.Number5;

                case UIKeyboardHidUsage.Keyboard6:
                    return Key.Number6;

                case UIKeyboardHidUsage.Keyboard7:
                    return Key.Number7;

                case UIKeyboardHidUsage.Keyboard8:
                    return Key.Number8;

                case UIKeyboardHidUsage.Keyboard9:
                    return Key.Number9;

                case UIKeyboardHidUsage.Keyboard0:
                    return Key.Number0;

                case UIKeyboardHidUsage.KeyboardReturnOrEnter:
                    return Key.Enter;

                case UIKeyboardHidUsage.KeyboardEscape:
                    return Key.Escape;

                case UIKeyboardHidUsage.KeyboardDeleteOrBackspace:
                    return Key.BackSpace;

                case UIKeyboardHidUsage.KeyboardTab:
                    return Key.Tab;

                case UIKeyboardHidUsage.KeyboardSpacebar:
                    return Key.Space;

                case UIKeyboardHidUsage.KeyboardHyphen:
                    return Key.Minus;

                case UIKeyboardHidUsage.KeyboardEqualSign:
                    return Key.Plus;

                case UIKeyboardHidUsage.KeyboardOpenBracket:
                    return Key.BracketLeft;

                case UIKeyboardHidUsage.KeyboardCloseBracket:
                    return Key.BracketRight;

                case UIKeyboardHidUsage.KeyboardBackslash:
                    return Key.BackSlash;

                case UIKeyboardHidUsage.KeyboardSemicolon:
                    return Key.Semicolon;

                case UIKeyboardHidUsage.KeyboardQuote:
                    return Key.Quote;

                case UIKeyboardHidUsage.KeyboardGraveAccentAndTilde:
                    return Key.Tilde;

                case UIKeyboardHidUsage.KeyboardComma:
                    return Key.Comma;

                case UIKeyboardHidUsage.KeyboardPeriod:
                    return Key.Period;

                case UIKeyboardHidUsage.KeyboardSlash:
                    return Key.Slash;

                case UIKeyboardHidUsage.KeyboardCapsLock:
                    return Key.CapsLock;

                case UIKeyboardHidUsage.KeyboardF1:
                    return Key.F1;

                case UIKeyboardHidUsage.KeyboardF2:
                    return Key.F2;

                case UIKeyboardHidUsage.KeyboardF3:
                    return Key.F3;

                case UIKeyboardHidUsage.KeyboardF4:
                    return Key.F4;

                case UIKeyboardHidUsage.KeyboardF5:
                    return Key.F5;

                case UIKeyboardHidUsage.KeyboardF6:
                    return Key.F6;

                case UIKeyboardHidUsage.KeyboardF7:
                    return Key.F7;

                case UIKeyboardHidUsage.KeyboardF8:
                    return Key.F8;

                case UIKeyboardHidUsage.KeyboardF9:
                    return Key.F9;

                case UIKeyboardHidUsage.KeyboardF10:
                    return Key.F10;

                case UIKeyboardHidUsage.KeyboardF11:
                    return Key.F11;

                case UIKeyboardHidUsage.KeyboardF12:
                    return Key.F12;

                // case UIKeyboardHidUsage.KeyboardPrintScreen:
                //     return Key.PrintScreen;
                //
                // case UIKeyboardHidUsage.KeyboardScrollLock:
                //     return Key.ScrollLock;
                //
                // case UIKeyboardHidUsage.KeyboardPause:
                //     return Key.Pause;
                //
                // case UIKeyboardHidUsage.KeyboardInsert:
                //     return Key.Insert;

                case UIKeyboardHidUsage.KeyboardHome:
                    return Key.Home;

                case UIKeyboardHidUsage.KeyboardPageUp:
                    return Key.PageUp;

                case UIKeyboardHidUsage.KeyboardDeleteForward:
                    return Key.Delete;

                case UIKeyboardHidUsage.KeyboardEnd:
                    return Key.End;

                case UIKeyboardHidUsage.KeyboardPageDown:
                    return Key.PageDown;

                case UIKeyboardHidUsage.KeyboardRightArrow:
                    return Key.Right;

                case UIKeyboardHidUsage.KeyboardLeftArrow:
                    return Key.Left;

                case UIKeyboardHidUsage.KeyboardDownArrow:
                    return Key.Down;

                case UIKeyboardHidUsage.KeyboardUpArrow:
                    return Key.Up;

                case UIKeyboardHidUsage.KeypadNumLock:
                    return Key.NumLock;

                case UIKeyboardHidUsage.KeypadSlash:
                    return Key.KeypadDivide;

                case UIKeyboardHidUsage.KeypadAsterisk:
                    return Key.KeypadMultiply;

                case UIKeyboardHidUsage.KeypadHyphen:
                    return Key.KeypadMinus;

                case UIKeyboardHidUsage.KeypadPlus:
                    return Key.KeypadPlus;

                case UIKeyboardHidUsage.KeypadEnter:
                    return Key.KeypadEnter;

                case UIKeyboardHidUsage.Keypad1:
                    return Key.Keypad1;

                case UIKeyboardHidUsage.Keypad2:
                    return Key.Keypad2;

                case UIKeyboardHidUsage.Keypad3:
                    return Key.Keypad3;

                case UIKeyboardHidUsage.Keypad4:
                    return Key.Keypad4;

                case UIKeyboardHidUsage.Keypad5:
                    return Key.Keypad5;

                case UIKeyboardHidUsage.Keypad6:
                    return Key.Keypad6;

                case UIKeyboardHidUsage.Keypad7:
                    return Key.Keypad7;

                case UIKeyboardHidUsage.Keypad8:
                    return Key.Keypad8;

                case UIKeyboardHidUsage.Keypad9:
                    return Key.Keypad9;

                case UIKeyboardHidUsage.Keypad0:
                    return Key.Keypad0;

                case UIKeyboardHidUsage.KeypadPeriod:
                    return Key.KeypadPeriod;

                case UIKeyboardHidUsage.KeyboardNonUSBackslash:
                    return Key.NonUSBackSlash;

                case UIKeyboardHidUsage.KeyboardApplication:
                    return Key.Menu;

                case UIKeyboardHidUsage.KeyboardF13:
                    return Key.PrintScreen;
                // return Key.F13;

                case UIKeyboardHidUsage.KeyboardF14:
                    return Key.ScrollLock;
                // return Key.F14;

                case UIKeyboardHidUsage.KeyboardF15:
                    return Key.Pause;
                // return Key.F15;

                case UIKeyboardHidUsage.KeyboardF16:
                    return Key.F16;

                case UIKeyboardHidUsage.KeyboardF17:
                    return Key.F17;

                case UIKeyboardHidUsage.KeyboardF18:
                    return Key.F18;

                case UIKeyboardHidUsage.KeyboardF19:
                    return Key.F19;

                case UIKeyboardHidUsage.KeyboardF20:
                    return Key.F20;

                case UIKeyboardHidUsage.KeyboardF21:
                    return Key.F21;

                case UIKeyboardHidUsage.KeyboardF22:
                    return Key.F22;

                case UIKeyboardHidUsage.KeyboardF23:
                    return Key.F23;

                case UIKeyboardHidUsage.KeyboardF24:
                    return Key.F24;

                // todo: is it actually expected for the help key to become insert..?
                case UIKeyboardHidUsage.KeyboardHelp:
                    return Key.Insert;

                // case UIKeyboardHidUsage.KeyboardMenu:
                //     return Key.Menu;

                case UIKeyboardHidUsage.KeyboardClear:
                    return Key.Clear;

                case UIKeyboardHidUsage.KeyboardLeftControl:
                    return Key.ControlLeft;

                case UIKeyboardHidUsage.KeyboardLeftShift:
                    return Key.ShiftLeft;

                case UIKeyboardHidUsage.KeyboardLeftAlt:
                    return Key.AltLeft;

                case UIKeyboardHidUsage.KeyboardLeftGui:
                    return Key.WinLeft;

                case UIKeyboardHidUsage.KeyboardRightControl:
                    return Key.ControlRight;

                case UIKeyboardHidUsage.KeyboardRightShift:
                    return Key.ShiftRight;

                case UIKeyboardHidUsage.KeyboardRightAlt:
                    return Key.AltRight;

                case UIKeyboardHidUsage.KeyboardRightGui:
                    return Key.WinRight;

                default:
                    return Key.Unknown;
            }
        }
    }
}
