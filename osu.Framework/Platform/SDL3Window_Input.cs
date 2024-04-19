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
using osu.Framework.Platform.SDL;
using osuTK;
using osuTK.Input;
using SDL;
using RectangleF = osu.Framework.Graphics.Primitives.RectangleF;

namespace osu.Framework.Platform
{
    internal partial class SDL3Window
    {
        private void setupInput(FrameworkConfigManager config)
        {
            config.BindWith(FrameworkSetting.ConfineMouseMode, ConfineMouseMode);

            WindowMode.BindValueChanged(_ => updateConfineMode());
            ConfineMouseMode.BindValueChanged(_ => updateConfineMode());
        }

        private bool relativeMouseMode;

        /// <summary>
        /// Set the state of SDL3's RelativeMouseMode (https://wiki.libsdl.org/SDL_SetRelativeMouseMode).
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
                ScheduleCommand(() => SDL3.SDL_SetRelativeMouseMode(value ? SDL_bool.SDL_TRUE : SDL_bool.SDL_FALSE));
                updateCursorConfinement();
            }
        }

        /// <summary>
        /// Controls whether the mouse is automatically captured when buttons are pressed and the cursor is outside the window.
        /// Only works with <see cref="RelativeMouseMode"/> disabled.
        /// </summary>
        /// <remarks>
        /// If the cursor leaves the window while it's captured, <see cref="SDL_EventType.SDL_EVENT_WINDOW_MOUSE_LEAVE"/> is not sent until the button(s) are released.
        /// And if the cursor leaves and enters the window while captured, <see cref="SDL_EventType.SDL_EVENT_WINDOW_MOUSE_ENTER"/> is not sent either.
        /// We disable relative mode when the cursor exits window bounds (not on the event), but we only enable it again on <see cref="SDL_EventType.SDL_EVENT_WINDOW_MOUSE_ENTER"/>.
        /// The above culminate in <see cref="RelativeMouseMode"/> staying off when the cursor leaves and enters the window bounds when any buttons are pressed.
        /// This is an invalid state, as the cursor is inside the window, and <see cref="RelativeMouseMode"/> is off.
        /// </remarks>
        internal bool MouseAutoCapture
        {
            set => ScheduleCommand(() => SDL3.SDL_SetHint(SDL3.SDL_HINT_MOUSE_AUTO_CAPTURE, value ? "1"u8 : "0"u8));
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

        private readonly Dictionary<SDL_JoystickID, SDL3ControllerBindings> controllers = new Dictionary<SDL_JoystickID, SDL3ControllerBindings>();

        private void updateCursorVisibility(bool cursorVisible) =>
            ScheduleCommand(() =>
            {
                if (cursorVisible)
                    SDL3.SDL_ShowCursor();
                else
                    SDL3.SDL_HideCursor();
            });

        /// <summary>
        /// Updates OS cursor confinement based on the current <see cref="CursorState"/>, <see cref="CursorConfineRect"/> and <see cref="RelativeMouseMode"/>.
        /// </summary>
        private unsafe void updateCursorConfinement()
        {
            bool confined = CursorState.HasFlagFast(CursorState.Confined);

            ScheduleCommand(() => SDL3.SDL_SetWindowMouseGrab(SDLWindowHandle, confined ? SDL_bool.SDL_TRUE : SDL_bool.SDL_FALSE));

            // Don't use SDL_SetWindowMouseRect when relative mode is enabled, as relative mode already confines the OS cursor to the window.
            // This is fine for our use case, as UserInputManager will clamp the mouse position.
            if (CursorConfineRect != null && confined && !RelativeMouseMode)
            {
                ScheduleCommand(() =>
                {
                    var rect = ((RectangleI)(CursorConfineRect / Scale)).ToSDLRect();
                    SDL3.SDL_SetWindowMouseRect(SDLWindowHandle, &rect);
                });
            }
            else
            {
                ScheduleCommand(() => SDL3.SDL_SetWindowMouseRect(SDLWindowHandle, null));
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

        private PointF previousPolledPoint = PointF.Empty;

        private SDLButtonMask pressedButtons;

        private unsafe void pollMouse()
        {
            float x, y;
            SDLButtonMask globalButtons = (SDLButtonMask)SDL3.SDL_GetGlobalMouseState(&x, &y);

            if (previousPolledPoint.X != x || previousPolledPoint.Y != y)
            {
                previousPolledPoint = new PointF(x, y);

                var pos = WindowMode.Value == Configuration.WindowMode.Windowed ? Position : windowDisplayBounds.Location;
                float rx = x - pos.X;
                float ry = y - pos.Y;

                MouseMove?.Invoke(new Vector2(rx * Scale, ry * Scale));
            }

            // a button should be released if it was pressed and its current global state differs (its bit in globalButtons is set to 0)
            SDLButtonMask buttonsToRelease = pressedButtons & (globalButtons ^ pressedButtons);

            // the outer if just optimises for the common case that there are no buttons to release.
            if (buttonsToRelease != 0)
            {
                if (buttonsToRelease.HasFlagFast(SDLButtonMask.SDL_BUTTON_LMASK)) MouseUp?.Invoke(MouseButton.Left);
                if (buttonsToRelease.HasFlagFast(SDLButtonMask.SDL_BUTTON_MMASK)) MouseUp?.Invoke(MouseButton.Middle);
                if (buttonsToRelease.HasFlagFast(SDLButtonMask.SDL_BUTTON_RMASK)) MouseUp?.Invoke(MouseButton.Right);
                if (buttonsToRelease.HasFlagFast(SDLButtonMask.SDL_BUTTON_X1MASK)) MouseUp?.Invoke(MouseButton.Button1);
                if (buttonsToRelease.HasFlagFast(SDLButtonMask.SDL_BUTTON_X2MASK)) MouseUp?.Invoke(MouseButton.Button2);
            }
        }

        public virtual void StartTextInput(bool allowIme) => ScheduleCommand(SDL3.SDL_StartTextInput);

        public void StopTextInput() => ScheduleCommand(SDL3.SDL_StopTextInput);

        /// <summary>
        /// Resets internal state of the platform-native IME.
        /// This will clear its composition text and prepare it for new input.
        /// </summary>
        public virtual void ResetIme() => ScheduleCommand(() =>
        {
            SDL3.SDL_StopTextInput();
            SDL3.SDL_StartTextInput();
        });

        public unsafe void SetTextInputRect(RectangleF rect) => ScheduleCommand(() =>
        {
            var sdlRect = ((RectangleI)(rect / Scale)).ToSDLRect();
            SDL3.SDL_SetTextInputRect(&sdlRect);
        });

        #region SDL Event Handling

        private void handleDropEvent(SDL_DropEvent evtDrop)
        {
            switch (evtDrop.type)
            {
                case SDL_EventType.SDL_EVENT_DROP_FILE:
                    string? str = evtDrop.GetData();
                    if (str != null)
                        DragDrop?.Invoke(str);

                    break;
            }
        }

        private readonly SDL_FingerID?[] activeTouches = new SDL_FingerID?[TouchState.MAX_TOUCH_COUNT];

        private TouchSource? getTouchSource(SDL_FingerID fingerId)
        {
            for (int i = 0; i < activeTouches.Length; i++)
            {
                if (fingerId == activeTouches[i])
                    return (TouchSource)i;
            }

            return null;
        }

        private TouchSource? assignNextAvailableTouchSource(SDL_FingerID fingerId)
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

        protected virtual void HandleTouchFingerEvent(SDL_TouchFingerEvent evtTfinger)
        {
            var existingSource = getTouchSource(evtTfinger.fingerID);

            if (evtTfinger.type == SDL_EventType.SDL_EVENT_FINGER_DOWN)
            {
                Debug.Assert(existingSource == null);
                existingSource = assignNextAvailableTouchSource(evtTfinger.fingerID);
            }

            if (existingSource == null)
                return;

            float x = evtTfinger.x * ClientSize.Width;
            float y = evtTfinger.y * ClientSize.Height;

            var touch = new Touch(existingSource.Value, new Vector2(x, y));

            switch (evtTfinger.type)
            {
                case SDL_EventType.SDL_EVENT_FINGER_DOWN:
                case SDL_EventType.SDL_EVENT_FINGER_MOTION:
                    TouchDown?.Invoke(touch);
                    break;

                case SDL_EventType.SDL_EVENT_FINGER_UP:
                    TouchUp?.Invoke(touch);
                    activeTouches[(int)existingSource] = null;
                    break;
            }
        }

        private unsafe void handleControllerDeviceEvent(SDL_GamepadDeviceEvent evtCdevice)
        {
            switch (evtCdevice.type)
            {
                case SDL_EventType.SDL_EVENT_GAMEPAD_ADDED:
                    addJoystick(evtCdevice.which);
                    break;

                case SDL_EventType.SDL_EVENT_GAMEPAD_REMOVED:
                    SDL3.SDL_CloseGamepad(controllers[evtCdevice.which].GamepadHandle);
                    controllers.Remove(evtCdevice.which);
                    break;

                case SDL_EventType.SDL_EVENT_GAMEPAD_REMAPPED:
                    if (controllers.TryGetValue(evtCdevice.which, out var state))
                        state.PopulateBindings();

                    break;
            }
        }

        private void handleControllerButtonEvent(SDL_GamepadButtonEvent evtCbutton)
        {
            var button = evtCbutton.Button.ToJoystickButton();

            switch (evtCbutton.type)
            {
                case SDL_EventType.SDL_EVENT_GAMEPAD_BUTTON_DOWN:
                    enqueueJoystickButtonInput(button, true);
                    break;

                case SDL_EventType.SDL_EVENT_GAMEPAD_BUTTON_UP:
                    enqueueJoystickButtonInput(button, false);
                    break;
            }
        }

        private void handleControllerAxisEvent(SDL_GamepadAxisEvent evtCaxis) =>
            enqueueJoystickAxisInput(evtCaxis.Axis.ToJoystickAxisSource(), evtCaxis.value);

        private unsafe void addJoystick(SDL_JoystickID instanceID)
        {
            // if the joystick is already opened, ignore it
            if (controllers.ContainsKey(instanceID))
                return;

            SDL_Joystick* joystick = SDL3.SDL_OpenJoystick(instanceID);

            SDL_Gamepad* controller = null;
            if (SDL3.SDL_IsGamepad(instanceID) == SDL_bool.SDL_TRUE)
                controller = SDL3.SDL_OpenGamepad(instanceID);

            controllers[instanceID] = new SDL3ControllerBindings(joystick, controller);
        }

        /// <summary>
        /// Populates <see cref="controllers"/> with joysticks that are already connected.
        /// </summary>
        private void populateJoysticks()
        {
            using var joysticks = SDL3.SDL_GetJoysticks();

            if (joysticks == null)
                return;

            for (int i = 0; i < joysticks.Count; i++)
            {
                addJoystick(joysticks[i]);
            }
        }

        private unsafe void handleJoyDeviceEvent(SDL_JoyDeviceEvent evtJdevice)
        {
            switch (evtJdevice.type)
            {
                case SDL_EventType.SDL_EVENT_JOYSTICK_ADDED:
                    addJoystick(evtJdevice.which);
                    break;

                case SDL_EventType.SDL_EVENT_JOYSTICK_REMOVED:
                    // if the joystick is already closed, ignore it
                    if (!controllers.ContainsKey(evtJdevice.which))
                        break;

                    SDL3.SDL_CloseJoystick(controllers[evtJdevice.which].JoystickHandle);
                    controllers.Remove(evtJdevice.which);
                    break;
            }
        }

        private void handleJoyButtonEvent(SDL_JoyButtonEvent evtJbutton)
        {
            // if this button exists in the controller bindings, skip it
            if (controllers.TryGetValue(evtJbutton.which, out var state) && state.IsJoystickButtonBound(evtJbutton.button))
                return;

            var button = JoystickButton.FirstButton + evtJbutton.button;

            switch (evtJbutton.type)
            {
                case SDL_EventType.SDL_EVENT_JOYSTICK_BUTTON_DOWN:
                    enqueueJoystickButtonInput(button, true);
                    break;

                case SDL_EventType.SDL_EVENT_JOYSTICK_BUTTON_UP:
                    enqueueJoystickButtonInput(button, false);
                    break;
            }
        }

        // ReSharper disable once UnusedParameter.Local
        private void handleJoyHatEvent(SDL_JoyHatEvent evtJhat)
        {
        }

        // ReSharper disable once UnusedParameter.Local
        private void handleJoyBallEvent(SDL_JoyBallEvent evtJball)
        {
        }

        private void handleJoyAxisEvent(SDL_JoyAxisEvent evtJaxis)
        {
            // if this axis exists in the controller bindings, skip it
            if (controllers.TryGetValue(evtJaxis.which, out var state) && state.IsJoystickAxisBound(evtJaxis.axis))
                return;

            enqueueJoystickAxisInput(JoystickAxisSource.Axis1 + evtJaxis.axis, evtJaxis.axis);
        }

        private ulong lastPreciseScroll;
        private const uint precise_scroll_debounce = 100;

        private void handleMouseWheelEvent(SDL_MouseWheelEvent evtWheel)
        {
            bool isPrecise(float f) => f % 1 != 0;

            if (isPrecise(evtWheel.x) || isPrecise(evtWheel.y))
                lastPreciseScroll = evtWheel.timestamp;

            bool precise = evtWheel.timestamp < lastPreciseScroll + precise_scroll_debounce;

            // SDL reports horizontal scroll opposite of what framework expects (in non-"natural" mode, scrolling to the right gives positive deltas while we want negative).
            TriggerMouseWheel(new Vector2(-evtWheel.x, evtWheel.y), precise);
        }

        private void handleMouseButtonEvent(SDL_MouseButtonEvent evtButton)
        {
            MouseButton button = mouseButtonFromEvent(evtButton.Button);
            SDLButtonMask mask = SDL3.SDL_BUTTON(evtButton.Button);
            Debug.Assert(Enum.IsDefined(mask));

            switch (evtButton.type)
            {
                case SDL_EventType.SDL_EVENT_MOUSE_BUTTON_DOWN:
                    pressedButtons |= mask;
                    MouseDown?.Invoke(button);
                    break;

                case SDL_EventType.SDL_EVENT_MOUSE_BUTTON_UP:
                    pressedButtons &= ~mask;
                    MouseUp?.Invoke(button);
                    break;
            }
        }

        private void handleMouseMotionEvent(SDL_MouseMotionEvent evtMotion)
        {
            if (SDL3.SDL_GetRelativeMouseMode() == SDL_bool.SDL_FALSE)
                MouseMove?.Invoke(new Vector2(evtMotion.x * Scale, evtMotion.y * Scale));
            else
                MouseMoveRelative?.Invoke(new Vector2(evtMotion.xrel * Scale, evtMotion.yrel * Scale));
        }

        protected virtual void HandleTextInputEvent(SDL_TextInputEvent evtText)
        {
            string? text = evtText.GetText();
            Debug.Assert(text != null);
            TriggerTextInput(text);
        }

        protected virtual void HandleTextEditingEvent(SDL_TextEditingEvent evtEdit)
        {
            string? text = evtEdit.GetText();
            Debug.Assert(text != null);
            TriggerTextEditing(text, evtEdit.start, evtEdit.length);
        }

        private void handleKeyboardEvent(SDL_KeyboardEvent evtKey)
        {
            Key key = evtKey.keysym.ToKey();

            if (key == Key.Unknown)
            {
                Logger.Log($"Unknown SDL key: {evtKey.keysym.scancode}, {evtKey.keysym.sym}");
                return;
            }

            switch (evtKey.type)
            {
                case SDL_EventType.SDL_EVENT_KEY_DOWN:
                    KeyDown?.Invoke(key);
                    break;

                case SDL_EventType.SDL_EVENT_KEY_UP:
                    KeyUp?.Invoke(key);
                    break;
            }
        }

        private void handleKeymapChangedEvent() => KeymapChanged?.Invoke();

        private MouseButton mouseButtonFromEvent(SDLButton button)
        {
            switch (button)
            {
                case SDLButton.SDL_BUTTON_LEFT:
                    return MouseButton.Left;

                case SDLButton.SDL_BUTTON_RIGHT:
                    return MouseButton.Right;

                case SDLButton.SDL_BUTTON_MIDDLE:
                    return MouseButton.Middle;

                case SDLButton.SDL_BUTTON_X1:
                    return MouseButton.Button1;

                case SDLButton.SDL_BUTTON_X2:
                    return MouseButton.Button2;

                default:
                    Logger.Log($"unknown mouse button: {button}, defaulting to left button");
                    return MouseButton.Left;
            }
        }

        #endregion

        /// <summary>
        /// Update the host window manager's cursor position based on a location relative to window coordinates.
        /// </summary>
        /// <param name="mousePosition">A position inside the window.</param>
        public unsafe void UpdateMousePosition(Vector2 mousePosition) => ScheduleCommand(() => SDL3.SDL_WarpMouseInWindow(SDLWindowHandle, mousePosition.X / Scale, mousePosition.Y / Scale));

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

        protected void TriggerMouseMove(float x, float y) => MouseMove?.Invoke(new Vector2(x, y));

        /// <summary>
        /// Invoked when the user moves the mouse cursor within the window (via relative / raw input).
        /// </summary>
        public event Action<Vector2>? MouseMoveRelative;

        /// <summary>
        /// Invoked when the user presses a mouse button.
        /// </summary>
        public event Action<MouseButton>? MouseDown;

        protected void TriggerMouseDown(MouseButton button) => MouseDown?.Invoke(button);

        /// <summary>
        /// Invoked when the user releases a mouse button.
        /// </summary>
        public event Action<MouseButton>? MouseUp;

        protected void TriggerMouseUp(MouseButton button) => MouseUp?.Invoke(button);

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
