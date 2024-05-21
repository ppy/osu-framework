// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Drawing;
using osu.Framework.Bindables;
using osu.Framework.Input;
using osuTK;
using osuTK.Input;

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

        Bindable<CursorState> CursorStateBindable { get; }

        Point Position { get; }
        Size Size { get; }
        bool MouseAutoCapture { set; }
        bool RelativeMouseMode { get; set; }

        void UpdateMousePosition(Vector2 position);
    }
}
