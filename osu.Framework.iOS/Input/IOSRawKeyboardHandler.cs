using System;
using System.Collections.Generic;
using UIKit;
using osu.Framework.Input.Handlers;
using osu.Framework.Input.StateChanges;
using osu.Framework.Platform;
using osuTK.Input;

namespace osu.Framework.iOS.Input {
    public class IOSRawKeyboardHandler : InputHandler {

        public override bool IsActive => true;

        public override int Priority => 0;

        Dictionary<int, Key> keyMap;

        void handleKey(int keyCode, bool isDown) {
            if (keyMap.ContainsKey(keyCode)) {
                PendingInputs.Enqueue(new KeyboardKeyInput(keyMap[keyCode], isDown));
            }
        }

        public override bool Initialize(GameHost host) {
            ((GameUIApplication)UIApplication.SharedApplication).keyEvent += handleKey;
            // Build key map
            keyMap = new Dictionary<int, Key>();
            keyMap.Add(4, Key.A);
            keyMap.Add(5, Key.B);
            keyMap.Add(6, Key.C);
            keyMap.Add(7, Key.D);
            keyMap.Add(8, Key.E);
            keyMap.Add(9, Key.F);
            keyMap.Add(10, Key.G);
            keyMap.Add(11, Key.H);
            keyMap.Add(12, Key.I);
            keyMap.Add(13, Key.J);
            keyMap.Add(14, Key.K);
            keyMap.Add(15, Key.L);
            keyMap.Add(16, Key.M);
            keyMap.Add(17, Key.N);
            keyMap.Add(18, Key.O);
            keyMap.Add(19, Key.P);
            keyMap.Add(20, Key.Q);
            keyMap.Add(21, Key.R);
            keyMap.Add(22, Key.S);
            keyMap.Add(23, Key.T);
            keyMap.Add(24, Key.U);
            keyMap.Add(25, Key.V);
            keyMap.Add(26, Key.W);
            keyMap.Add(27, Key.X);
            keyMap.Add(28, Key.Y);
            keyMap.Add(29, Key.Z);
            keyMap.Add(30, Key.Number1);
            keyMap.Add(31, Key.Number2);
            keyMap.Add(32, Key.Number3);
            keyMap.Add(33, Key.Number4);
            keyMap.Add(34, Key.Number5);
            keyMap.Add(35, Key.Number6);
            keyMap.Add(36, Key.Number7);
            keyMap.Add(37, Key.Number8);
            keyMap.Add(38, Key.Number9);
            keyMap.Add(39, Key.Number0);
            keyMap.Add(40, Key.Enter);
            keyMap.Add(43, Key.Tab);
            keyMap.Add(44, Key.Space);
            keyMap.Add(45, Key.Minus);
            keyMap.Add(46, Key.Plus);
            keyMap.Add(47, Key.BracketLeft);
            keyMap.Add(48, Key.BracketRight);
            keyMap.Add(49, Key.BackSlash);
            keyMap.Add(51, Key.Semicolon);
            keyMap.Add(52, Key.Quote);
            keyMap.Add(53, Key.Grave);
            keyMap.Add(54, Key.Comma);
            keyMap.Add(55, Key.Period);
            keyMap.Add(56, Key.Slash);
            keyMap.Add(57, Key.CapsLock);
            keyMap.Add(79, Key.Right);
            keyMap.Add(80, Key.Left);
            keyMap.Add(81, Key.Down);
            keyMap.Add(82, Key.Up);
            keyMap.Add(224, Key.ControlLeft);
            keyMap.Add(225, Key.ShiftLeft);
            keyMap.Add(226, Key.WinLeft);
            keyMap.Add(227, Key.AltLeft);
            keyMap.Add(229, Key.ShiftRight);
            keyMap.Add(230, Key.WinRight);
            keyMap.Add(231, Key.AltRight);
            return true;
        }
    }
}
