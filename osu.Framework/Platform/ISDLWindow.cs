// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Bindables;
using osu.Framework.Input;
using osuTK;
using osuTK.Input;
using RectangleF = osu.Framework.Graphics.Primitives.RectangleF;

namespace osu.Framework.Platform
{
    internal interface ISDLWindow : IWindow
    {
        event Action<JoystickButton> JoystickButtonDown;
        event Action<JoystickButton> JoystickButtonUp;
        event Action<JoystickAxisSource, float> JoystickAxisChanged;
        event Action<Touch> TouchDown;
        event Action<Touch> TouchUp;
        event Action<Vector2> MouseMove;
        event Action<Vector2> MouseMoveRelative;
        event Action<MouseButton> MouseDown;
        event Action<MouseButton> MouseUp;
        event Action<Vector2, bool> MouseWheel;
        event Action<Key> KeyDown;
        event Action<Key> KeyUp;
        event Action<string> TextInput;
        event TextEditingDelegate TextEditing;

        Bindable<CursorState> CursorStateBindable { get; }

        bool MouseAutoCapture { set; }
        bool RelativeMouseMode { get; set; }
        bool CapsLockPressed { get; }
        bool KeyboardAttached { get; }

        void UpdateMousePosition(Vector2 position);

        void StartTextInput(TextInputProperties properties);
        void StopTextInput();
        void SetTextInputRect(RectangleF rectangle);
        void ResetIme();
    }

    /// <summary>
    /// Fired when text is edited, usually via IME composition.
    /// </summary>
    /// <param name="text">The composition text.</param>
    /// <param name="start">The index of the selection start.</param>
    /// <param name="length">The length of the selection.</param>
    public delegate void TextEditingDelegate(string text, int start, int length);
}
