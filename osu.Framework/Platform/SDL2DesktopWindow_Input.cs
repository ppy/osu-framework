// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using osu.Framework.Bindables;
using osu.Framework.Configuration;
using osu.Framework.Extensions.EnumExtensions;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Input;
using osu.Framework.Input.States;
using osu.Framework.Logging;
using osu.Framework.Platform.SDL2;
using osuTK;
using osuTK.Input;
using SDL2;
using RectangleF = osu.Framework.Graphics.Primitives.RectangleF;

namespace osu.Framework.Platform
{
    public partial class SDL2DesktopWindow
    {
        private void setupInput(FrameworkConfigManager config)
        {
            config.BindWith(FrameworkSetting.ConfineMouseMode, ConfineMouseMode);

            WindowMode.BindValueChanged(_ => updateConfineMode());
            ConfineMouseMode.BindValueChanged(_ => updateConfineMode());
        }

        private bool relativeMouseMode;

        /// <summary>
        /// Set the state of SDL2's RelativeMouseMode (https://wiki.libsdl.org/SDL_SetRelativeMouseMode).
        /// On all platforms, this will lock the mouse to the window (although escaping by setting <see cref="ConfineMouseMode"/> is still possible via a local implementation).
        /// On windows, this will use raw input if available.
        /// </summary>
        public bool RelativeMouseMode
        {
            get => relativeMouseMode;
            set
            {
                if (relativeMouseMode == value)
                    return;

                if (value && !CursorState.HasFlagFast(CursorState.Hidden))
                    throw new InvalidOperationException($"Cannot set {nameof(RelativeMouseMode)} to true when the cursor is not hidden via {nameof(CursorState)}.");

                relativeMouseMode = value;
                ScheduleCommand(() => SDL.SDL_SetRelativeMouseMode(value ? SDL.SDL_bool.SDL_TRUE : SDL.SDL_bool.SDL_FALSE));
                updateCursorConfinement();
            }
        }

        /// <summary>
        /// Controls whether the mouse is automatically captured when buttons are pressed and the cursor is outside the window.
        /// Only works with <see cref="RelativeMouseMode"/> disabled.
        /// </summary>
        /// <remarks>
        /// If the cursor leaves the window while it's captured, <see cref="SDL.SDL_WindowEventID.SDL_WINDOWEVENT_LEAVE"/> is not sent until the button(s) are released.
        /// And if the cursor leaves and enters the window while captured, <see cref="SDL.SDL_WindowEventID.SDL_WINDOWEVENT_ENTER"/> is not sent either.
        /// We disable relative mode when the cursor exits window bounds (not on the event), but we only enable it again on <see cref="SDL.SDL_WindowEventID.SDL_WINDOWEVENT_ENTER"/>.
        /// The above culminate in <see cref="RelativeMouseMode"/> staying off when the cursor leaves and enters the window bounds when any buttons are pressed.
        /// This is an invalid state, as the cursor is inside the window, and <see cref="RelativeMouseMode"/> is off.
        /// </remarks>
        internal bool MouseAutoCapture
        {
            set => ScheduleCommand(() => SDL.SDL_SetHint(SDL.SDL_HINT_MOUSE_AUTO_CAPTURE, value ? "1" : "0"));
        }

        /// <summary>
        /// Provides a bindable that controls the window's <see cref="CursorStateBindable"/>.
        /// </summary>
        public Bindable<CursorState> CursorStateBindable { get; } = new Bindable<CursorState>();

        public CursorState CursorState
        {
            get => CursorStateBindable.Value;
            set => CursorStateBindable.Value = value;
        }

        private RectangleF? cursorConfineRect;

        public RectangleF? CursorConfineRect
        {
            get => cursorConfineRect;
            set
            {
                cursorConfineRect = value;
                updateCursorConfinement();
            }
        }

        private readonly Dictionary<int, SDL2ControllerBindings> controllers = new Dictionary<int, SDL2ControllerBindings>();

        private void updateCursorVisibility(bool cursorVisible) =>
            ScheduleCommand(() => SDL.SDL_ShowCursor(cursorVisible ? SDL.SDL_ENABLE : SDL.SDL_DISABLE));

        /// <summary>
        /// Updates OS cursor confinement based on the current <see cref="CursorState"/>, <see cref="CursorConfineRect"/> and <see cref="RelativeMouseMode"/>.
        /// </summary>
        private void updateCursorConfinement()
        {
            bool confined = CursorState.HasFlagFast(CursorState.Confined);

            ScheduleCommand(() => SDL.SDL_SetWindowGrab(SDLWindowHandle, confined ? SDL.SDL_bool.SDL_TRUE : SDL.SDL_bool.SDL_FALSE));

            // Don't use SDL_SetWindowMouseRect when relative mode is enabled, as relative mode already confines the OS cursor to the window.
            // This is fine for our use case, as UserInputManager will clamp the mouse position.
            if (CursorConfineRect != null && confined && !RelativeMouseMode)
            {
                var rect = ((RectangleI)(CursorConfineRect / Scale)).ToSDLRect();
                ScheduleCommand(() => SDL.SDL_SetWindowMouseRect(SDLWindowHandle, ref rect));
            }
            else
            {
                ScheduleCommand(() => SDL.SDL_SetWindowMouseRect(SDLWindowHandle, IntPtr.Zero));
            }
        }

        /// <summary>
        /// Bound to <see cref="FrameworkSetting.ConfineMouseMode"/>.
        /// </summary>
        public readonly Bindable<ConfineMouseMode> ConfineMouseMode = new Bindable<ConfineMouseMode>();

        private void enqueueJoystickAxisInput(JoystickAxisSource axisSource, short axisValue)
        {
            // SDL reports axis values in the range short.MinValue to short.MaxValue, so we scale and clamp it to the range of -1f to 1f
            float clamped = Math.Clamp((float)axisValue / short.MaxValue, -1f, 1f);
            JoystickAxisChanged?.Invoke(axisSource, clamped);
        }

        private void enqueueJoystickButtonInput(JoystickButton button, bool isPressed)
        {
            if (isPressed)
                JoystickButtonDown?.Invoke(button);
            else
                JoystickButtonUp?.Invoke(button);
        }

        private Point previousPolledPoint = Point.Empty;

        private void pollMouse()
        {
            SDL.SDL_GetGlobalMouseState(out int x, out int y);
            if (previousPolledPoint.X == x && previousPolledPoint.Y == y)
                return;

            previousPolledPoint = new Point(x, y);

            var pos = WindowMode.Value == Configuration.WindowMode.Windowed ? Position : windowDisplayBounds.Location;
            int rx = x - pos.X;
            int ry = y - pos.Y;

            MouseMove?.Invoke(new Vector2(rx * Scale, ry * Scale));
        }

        public virtual void StartTextInput(bool allowIme) => ScheduleCommand(SDL.SDL_StartTextInput);

        public void StopTextInput() => ScheduleCommand(SDL.SDL_StopTextInput);

        /// <summary>
        /// Resets internal state of the platform-native IME.
        /// This will clear its composition text and prepare it for new input.
        /// </summary>
        public virtual void ResetIme() => ScheduleCommand(() =>
        {
            SDL.SDL_StopTextInput();
            SDL.SDL_StartTextInput();
        });

        public void SetTextInputRect(RectangleF rect) => ScheduleCommand(() =>
        {
            var sdlRect = ((RectangleI)(rect / Scale)).ToSDLRect();
            SDL.SDL_SetTextInputRect(ref sdlRect);
        });

        #region SDL Event Handling

        private void handleDropEvent(SDL.SDL_DropEvent evtDrop)
        {
            switch (evtDrop.type)
            {
                case SDL.SDL_EventType.SDL_DROPFILE:
                    string str = SDL.UTF8_ToManaged(evtDrop.file, true);
                    if (str != null)
                        DragDrop?.Invoke(str);

                    break;
            }
        }

        private readonly long?[] activeTouches = new long?[TouchState.MAX_TOUCH_COUNT];

        private TouchSource? getTouchSource(long fingerId)
        {
            for (int i = 0; i < activeTouches.Length; i++)
            {
                if (fingerId == activeTouches[i])
                    return (TouchSource)i;
            }

            return null;
        }

        private TouchSource? assignNextAvailableTouchSource(long fingerId)
        {
            for (int i = 0; i < activeTouches.Length; i++)
            {
                if (activeTouches[i] != null) continue;

                activeTouches[i] = fingerId;
                return (TouchSource)i;
            }

            // we only handle up to TouchState.MAX_TOUCH_COUNT. Ignore any further touches for now.
            return null;
        }

        private void handleTouchFingerEvent(SDL.SDL_TouchFingerEvent evtTfinger)
        {
            var eventType = (SDL.SDL_EventType)evtTfinger.type;

            var existingSource = getTouchSource(evtTfinger.fingerId);

            if (eventType == SDL.SDL_EventType.SDL_FINGERDOWN)
            {
                Debug.Assert(existingSource == null);
                existingSource = assignNextAvailableTouchSource(evtTfinger.fingerId);
            }

            if (existingSource == null)
                return;

            float x = evtTfinger.x * Size.Width;
            float y = evtTfinger.y * Size.Height;

            var touch = new Touch(existingSource.Value, new Vector2(x, y));

            switch (eventType)
            {
                case SDL.SDL_EventType.SDL_FINGERDOWN:
                case SDL.SDL_EventType.SDL_FINGERMOTION:
                    TouchDown?.Invoke(touch);
                    break;

                case SDL.SDL_EventType.SDL_FINGERUP:
                    TouchUp?.Invoke(touch);
                    activeTouches[(int)existingSource] = null;
                    break;
            }
        }

        private void handleControllerDeviceEvent(SDL.SDL_ControllerDeviceEvent evtCdevice)
        {
            switch (evtCdevice.type)
            {
                case SDL.SDL_EventType.SDL_CONTROLLERDEVICEADDED:
                    addJoystick(evtCdevice.which);
                    break;

                case SDL.SDL_EventType.SDL_CONTROLLERDEVICEREMOVED:
                    SDL.SDL_GameControllerClose(controllers[evtCdevice.which].ControllerHandle);
                    controllers.Remove(evtCdevice.which);
                    break;

                case SDL.SDL_EventType.SDL_CONTROLLERDEVICEREMAPPED:
                    if (controllers.TryGetValue(evtCdevice.which, out var state))
                        state.PopulateBindings();

                    break;
            }
        }

        private void handleControllerButtonEvent(SDL.SDL_ControllerButtonEvent evtCbutton)
        {
            var button = ((SDL.SDL_GameControllerButton)evtCbutton.button).ToJoystickButton();

            switch (evtCbutton.type)
            {
                case SDL.SDL_EventType.SDL_CONTROLLERBUTTONDOWN:
                    enqueueJoystickButtonInput(button, true);
                    break;

                case SDL.SDL_EventType.SDL_CONTROLLERBUTTONUP:
                    enqueueJoystickButtonInput(button, false);
                    break;
            }
        }

        private void handleControllerAxisEvent(SDL.SDL_ControllerAxisEvent evtCaxis) =>
            enqueueJoystickAxisInput(((SDL.SDL_GameControllerAxis)evtCaxis.axis).ToJoystickAxisSource(), evtCaxis.axisValue);

        private void addJoystick(int which)
        {
            int instanceID = SDL.SDL_JoystickGetDeviceInstanceID(which);

            // if the joystick is already opened, ignore it
            if (controllers.ContainsKey(instanceID))
                return;

            var joystick = SDL.SDL_JoystickOpen(which);

            var controller = IntPtr.Zero;
            if (SDL.SDL_IsGameController(which) == SDL.SDL_bool.SDL_TRUE)
                controller = SDL.SDL_GameControllerOpen(which);

            controllers[instanceID] = new SDL2ControllerBindings(joystick, controller);
        }

        /// <summary>
        /// Populates <see cref="controllers"/> with joysticks that are already connected.
        /// </summary>
        private void populateJoysticks()
        {
            for (int i = 0; i < SDL.SDL_NumJoysticks(); i++)
            {
                addJoystick(i);
            }
        }

        private void handleJoyDeviceEvent(SDL.SDL_JoyDeviceEvent evtJdevice)
        {
            switch (evtJdevice.type)
            {
                case SDL.SDL_EventType.SDL_JOYDEVICEADDED:
                    addJoystick(evtJdevice.which);
                    break;

                case SDL.SDL_EventType.SDL_JOYDEVICEREMOVED:
                    // if the joystick is already closed, ignore it
                    if (!controllers.ContainsKey(evtJdevice.which))
                        break;

                    SDL.SDL_JoystickClose(controllers[evtJdevice.which].JoystickHandle);
                    controllers.Remove(evtJdevice.which);
                    break;
            }
        }

        private void handleJoyButtonEvent(SDL.SDL_JoyButtonEvent evtJbutton)
        {
            // if this button exists in the controller bindings, skip it
            if (controllers.TryGetValue(evtJbutton.which, out var state) && state.GetButtonForIndex(evtJbutton.button) != SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_INVALID)
                return;

            var button = JoystickButton.FirstButton + evtJbutton.button;

            switch (evtJbutton.type)
            {
                case SDL.SDL_EventType.SDL_JOYBUTTONDOWN:
                    enqueueJoystickButtonInput(button, true);
                    break;

                case SDL.SDL_EventType.SDL_JOYBUTTONUP:
                    enqueueJoystickButtonInput(button, false);
                    break;
            }
        }

        // ReSharper disable once UnusedParameter.Local
        private void handleJoyHatEvent(SDL.SDL_JoyHatEvent evtJhat)
        {
        }

        // ReSharper disable once UnusedParameter.Local
        private void handleJoyBallEvent(SDL.SDL_JoyBallEvent evtJball)
        {
        }

        private void handleJoyAxisEvent(SDL.SDL_JoyAxisEvent evtJaxis)
        {
            // if this axis exists in the controller bindings, skip it
            if (controllers.TryGetValue(evtJaxis.which, out var state) && state.GetAxisForIndex(evtJaxis.axis) != SDL.SDL_GameControllerAxis.SDL_CONTROLLER_AXIS_INVALID)
                return;

            enqueueJoystickAxisInput(JoystickAxisSource.Axis1 + evtJaxis.axis, evtJaxis.axisValue);
        }

        private uint lastPreciseScroll;
        private const uint precise_scroll_debounce = 100;

        private void handleMouseWheelEvent(SDL.SDL_MouseWheelEvent evtWheel)
        {
            bool isPrecise(float f) => f % 1 != 0;

            if (isPrecise(evtWheel.preciseX) || isPrecise(evtWheel.preciseY))
                lastPreciseScroll = evtWheel.timestamp;

            bool precise = evtWheel.timestamp < lastPreciseScroll + precise_scroll_debounce;

            // SDL reports horizontal scroll opposite of what framework expects (in non-"natural" mode, scrolling to the right gives positive deltas while we want negative).
            TriggerMouseWheel(new Vector2(-evtWheel.preciseX, evtWheel.preciseY), precise);
        }

        private void handleMouseButtonEvent(SDL.SDL_MouseButtonEvent evtButton)
        {
            MouseButton button = mouseButtonFromEvent(evtButton.button);

            switch (evtButton.type)
            {
                case SDL.SDL_EventType.SDL_MOUSEBUTTONDOWN:
                    MouseDown?.Invoke(button);
                    break;

                case SDL.SDL_EventType.SDL_MOUSEBUTTONUP:
                    MouseUp?.Invoke(button);
                    break;
            }
        }

        private void handleMouseMotionEvent(SDL.SDL_MouseMotionEvent evtMotion)
        {
            if (SDL.SDL_GetRelativeMouseMode() == SDL.SDL_bool.SDL_FALSE)
                MouseMove?.Invoke(new Vector2(evtMotion.x * Scale, evtMotion.y * Scale));
            else
                MouseMoveRelative?.Invoke(new Vector2(evtMotion.xrel * Scale, evtMotion.yrel * Scale));
        }

        protected virtual unsafe void HandleTextInputEvent(SDL.SDL_TextInputEvent evtText)
        {
            if (!SDL2Extensions.TryGetStringFromBytePointer(evtText.text, out string text))
                return;

            TriggerTextInput(text);
        }

        protected virtual unsafe void HandleTextEditingEvent(SDL.SDL_TextEditingEvent evtEdit)
        {
            if (!SDL2Extensions.TryGetStringFromBytePointer(evtEdit.text, out string text))
                return;

            TriggerTextEditing(text, evtEdit.start, evtEdit.length);
        }

        private void handleKeyboardEvent(SDL.SDL_KeyboardEvent evtKey)
        {
            Key key = evtKey.keysym.ToKey();

            if (key == Key.Unknown)
            {
                Logger.Log($"Unknown SDL key: {evtKey.keysym.scancode}, {evtKey.keysym.sym}");
                return;
            }

            switch (evtKey.type)
            {
                case SDL.SDL_EventType.SDL_KEYDOWN:
                    KeyDown?.Invoke(key);
                    break;

                case SDL.SDL_EventType.SDL_KEYUP:
                    KeyUp?.Invoke(key);
                    break;
            }
        }

        private void handleKeymapChangedEvent() => KeymapChanged?.Invoke();

        private MouseButton mouseButtonFromEvent(byte button)
        {
            switch ((uint)button)
            {
                default:
                case SDL.SDL_BUTTON_LEFT:
                    return MouseButton.Left;

                case SDL.SDL_BUTTON_RIGHT:
                    return MouseButton.Right;

                case SDL.SDL_BUTTON_MIDDLE:
                    return MouseButton.Middle;

                case SDL.SDL_BUTTON_X1:
                    return MouseButton.Button1;

                case SDL.SDL_BUTTON_X2:
                    return MouseButton.Button2;
            }
        }

        #endregion

        /// <summary>
        /// Update the host window manager's cursor position based on a location relative to window coordinates.
        /// </summary>
        /// <param name="mousePosition">A position inside the window.</param>
        public void UpdateMousePosition(Vector2 mousePosition) => ScheduleCommand(() =>
            SDL.SDL_WarpMouseInWindow(SDLWindowHandle, (int)(mousePosition.X / Scale), (int)(mousePosition.Y / Scale)));

        private void updateConfineMode()
        {
            bool confine = false;

            switch (ConfineMouseMode.Value)
            {
                case Input.ConfineMouseMode.Fullscreen:
                    confine = WindowMode.Value != Configuration.WindowMode.Windowed;
                    break;

                case Input.ConfineMouseMode.Always:
                    confine = true;
                    break;
            }

            if (confine)
                CursorStateBindable.Value |= CursorState.Confined;
            else
                CursorStateBindable.Value &= ~CursorState.Confined;
        }

        #region Events

        /// <summary>
        /// Invoked when the mouse cursor enters the window.
        /// </summary>
        public event Action? MouseEntered;

        /// <summary>
        /// Invoked when the mouse cursor leaves the window.
        /// </summary>
        public event Action? MouseLeft;

        /// <summary>
        /// Invoked when the user scrolls the mouse wheel over the window.
        /// </summary>
        /// <remarks>
        /// Delta is positive when mouse wheel scrolled to the up or left, in non-"natural" scroll mode (ie. the classic way).
        /// </remarks>
        public event Action<Vector2, bool>? MouseWheel;

        protected void TriggerMouseWheel(Vector2 delta, bool precise) => MouseWheel?.Invoke(delta, precise);

        /// <summary>
        /// Invoked when the user moves the mouse cursor within the window.
        /// </summary>
        public event Action<Vector2>? MouseMove;

        /// <summary>
        /// Invoked when the user moves the mouse cursor within the window (via relative / raw input).
        /// </summary>
        public event Action<Vector2>? MouseMoveRelative;

        /// <summary>
        /// Invoked when the user presses a mouse button.
        /// </summary>
        public event Action<MouseButton>? MouseDown;

        /// <summary>
        /// Invoked when the user releases a mouse button.
        /// </summary>
        public event Action<MouseButton>? MouseUp;

        /// <summary>
        /// Invoked when the user presses a key.
        /// </summary>
        public event Action<Key>? KeyDown;

        /// <summary>
        /// Invoked when the user releases a key.
        /// </summary>
        public event Action<Key>? KeyUp;

        /// <summary>
        /// Invoked when the user enters text.
        /// </summary>
        public event Action<string>? TextInput;

        protected void TriggerTextInput(string text) => TextInput?.Invoke(text);

        /// <summary>
        /// Invoked when an IME text editing event occurs.
        /// </summary>
        public event TextEditingDelegate? TextEditing;

        protected void TriggerTextEditing(string text, int start, int length) => TextEditing?.Invoke(text, start, length);

        /// <inheritdoc cref="IWindow.KeymapChanged"/>
        public event Action? KeymapChanged;

        /// <summary>
        /// Invoked when a joystick axis changes.
        /// </summary>
        public event Action<JoystickAxisSource, float>? JoystickAxisChanged;

        /// <summary>
        /// Invoked when the user presses a button on a joystick.
        /// </summary>
        public event Action<JoystickButton>? JoystickButtonDown;

        /// <summary>
        /// Invoked when the user releases a button on a joystick.
        /// </summary>
        public event Action<JoystickButton>? JoystickButtonUp;

        /// <summary>
        /// Invoked when a finger moves or touches a touchscreen.
        /// </summary>
        public event Action<Touch>? TouchDown;

        /// <summary>
        /// Invoked when a finger leaves the touchscreen.
        /// </summary>
        public event Action<Touch>? TouchUp;

        #endregion

        /// <summary>
        /// Fired when text is edited, usually via IME composition.
        /// </summary>
        /// <param name="text">The composition text.</param>
        /// <param name="start">The index of the selection start.</param>
        /// <param name="length">The length of the selection.</param>
        public delegate void TextEditingDelegate(string text, int start, int length);
    }
}
