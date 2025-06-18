// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Threading;
using osu.Framework.Bindables;
using osu.Framework.Configuration;
using osu.Framework.Extensions.EnumExtensions;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Input;
using osu.Framework.Input.States;
using osu.Framework.Logging;
using osuTK;
using osuTK.Input;
using SDL;
using RectangleF = osu.Framework.Graphics.Primitives.RectangleF;
using static SDL.SDL3;

namespace osu.Framework.Platform.SDL3
{
    internal unsafe partial class SDL3Window
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
                ScheduleCommand(() => SDL_SetWindowRelativeMouseMode(SDLWindowHandle, value));
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
        public bool MouseAutoCapture
        {
            set => ScheduleCommand(() => SDL_SetHint(SDL_HINT_MOUSE_AUTO_CAPTURE, value ? "1"u8 : "0"u8));
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
                    SDL_ShowCursor();
                else
                    SDL_HideCursor();
            });

        /// <summary>
        /// Updates OS cursor confinement based on the current <see cref="CursorState"/>, <see cref="CursorConfineRect"/> and <see cref="RelativeMouseMode"/>.
        /// </summary>
        private void updateCursorConfinement()
        {
            bool confined = CursorState.HasFlagFast(CursorState.Confined);

            ScheduleCommand(() => SDL_SetWindowMouseGrab(SDLWindowHandle, confined));

            // Don't use SDL_SetWindowMouseRect when relative mode is enabled, as relative mode already confines the OS cursor to the window.
            // This is fine for our use case, as UserInputManager will clamp the mouse position.
            if (CursorConfineRect != null && confined && !RelativeMouseMode)
            {
                ScheduleCommand(() =>
                {
                    var rect = ((RectangleI)(CursorConfineRect / Scale)).ToSDLRect();
                    SDL_SetWindowMouseRect(SDLWindowHandle, &rect);
                });
            }
            else
            {
                ScheduleCommand(() => SDL_SetWindowMouseRect(SDLWindowHandle, null));
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

        private volatile uint pressedButtons;

        private void pollMouse()
        {
            float x, y;
            var pressed = (SDL_MouseButtonFlags)pressedButtons;
            SDL_MouseButtonFlags globalButtons = SDL_GetGlobalMouseState(&x, &y);

            if (previousPolledPoint.X != x || previousPolledPoint.Y != y)
            {
                previousPolledPoint = new PointF(x, y);

                var pos = WindowMode.Value == Configuration.WindowMode.Windowed ? Position : windowDisplayBounds.Location;
                float rx = x - pos.X;
                float ry = y - pos.Y;

                MouseMove?.Invoke(new Vector2(rx * Scale, ry * Scale));
            }

            // a button should be released if it was pressed and its current global state differs (its bit in globalButtons is set to 0)
            SDL_MouseButtonFlags buttonsToRelease = pressed & (globalButtons ^ pressed);

            // the outer if just optimises for the common case that there are no buttons to release.
            if (buttonsToRelease != 0)
            {
                Interlocked.And(ref pressedButtons, (uint)~buttonsToRelease);

                if (buttonsToRelease.HasFlagFast(SDL_MouseButtonFlags.SDL_BUTTON_LMASK)) MouseUp?.Invoke(MouseButton.Left);
                if (buttonsToRelease.HasFlagFast(SDL_MouseButtonFlags.SDL_BUTTON_MMASK)) MouseUp?.Invoke(MouseButton.Middle);
                if (buttonsToRelease.HasFlagFast(SDL_MouseButtonFlags.SDL_BUTTON_RMASK)) MouseUp?.Invoke(MouseButton.Right);
                if (buttonsToRelease.HasFlagFast(SDL_MouseButtonFlags.SDL_BUTTON_X1MASK)) MouseUp?.Invoke(MouseButton.Button1);
                if (buttonsToRelease.HasFlagFast(SDL_MouseButtonFlags.SDL_BUTTON_X2MASK)) MouseUp?.Invoke(MouseButton.Button2);
            }
        }

        private SDL_PropertiesID? currentTextInputProperties;

        public virtual void StartTextInput(TextInputProperties properties) => ScheduleCommand(() =>
        {
            currentTextInputProperties ??= SDL_CreateProperties();

            var props = currentTextInputProperties.Value;
            SDL_SetNumberProperty(props, SDL_PROP_TEXTINPUT_TYPE_NUMBER, (long)properties.Type.ToSDLTextInputType());

            if (!properties.AutoCapitalisation)
                SDL_SetNumberProperty(props, SDL_PROP_TEXTINPUT_CAPITALIZATION_NUMBER, (long)SDL_Capitalization.SDL_CAPITALIZE_NONE);
            else
                SDL_ClearProperty(props, SDL_PROP_TEXTINPUT_CAPITALIZATION_NUMBER);

            if (properties.Type == TextInputType.Code)
                SDL_SetBooleanProperty(props, SDL_PROP_TEXTINPUT_AUTOCORRECT_BOOLEAN, false);
            else
                SDL_ClearProperty(props, SDL_PROP_TEXTINPUT_AUTOCORRECT_BOOLEAN);

            SDL_StartTextInputWithProperties(SDLWindowHandle, props);
        });

        public void StopTextInput() => ScheduleCommand(() => SDL_StopTextInput(SDLWindowHandle));

        /// <summary>
        /// Resets internal state of the platform-native IME.
        /// This will clear its composition text and prepare it for new input.
        /// </summary>
        public virtual void ResetIme() => ScheduleCommand(() =>
        {
            SDL_StopTextInput(SDLWindowHandle);

            if (currentTextInputProperties is SDL_PropertiesID props)
                SDL_StartTextInputWithProperties(SDLWindowHandle, props);
            else
                SDL_StartTextInput(SDLWindowHandle);
        });

        public void SetTextInputRect(RectangleF rect) => ScheduleCommand(() =>
        {
            // TODO: SDL3 allows apps to set cursor position through the third parameter of SDL_SetTextInputArea.
            var sdlRect = ((RectangleI)(rect / Scale)).ToSDLRect();
            SDL_SetTextInputArea(SDLWindowHandle, &sdlRect, 0);
        });

        #region SDL Event Handling

        private void handleDropEvent(SDL_DropEvent evtDrop)
        {
            switch (evtDrop.type)
            {
                case SDL_EventType.SDL_EVENT_DROP_FILE:
                    string? str = evtDrop.GetData();
                    if (str != null)
                        TriggerDragDrop(str);

                    break;
            }
        }

        private readonly SDL_FingerID?[] activeTouches = new SDL_FingerID?[TouchState.MAX_NATIVE_TOUCH_COUNT];

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

            // we only handle up to TouchState.MAX_NATIVE_TOUCH_COUNT. Ignore any further touches for now.
            return null;
        }

        private void handleTouchFingerEvent(SDL_TouchFingerEvent evtTfinger)
        {
            var existingSource = getTouchSource(evtTfinger.fingerID);

            if (evtTfinger.type == SDL_EventType.SDL_EVENT_FINGER_DOWN)
            {
                // TODO: remove when upstream fixes https://github.com/libsdl-org/SDL/issues/9591
                // ignore SDL_EVENT_FINGER_DOWN for fingers that are already pressed
                if (existingSource != null)
                    return;

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
                case SDL_EventType.SDL_EVENT_FINGER_CANCELED:
                    TouchUp?.Invoke(touch);
                    activeTouches[(int)existingSource] = null;
                    break;
            }
        }

        private void handleControllerDeviceEvent(SDL_GamepadDeviceEvent evtCdevice)
        {
            switch (evtCdevice.type)
            {
                case SDL_EventType.SDL_EVENT_GAMEPAD_ADDED:
                    addJoystick(evtCdevice.which);
                    break;

                case SDL_EventType.SDL_EVENT_GAMEPAD_REMOVED:
                    SDL_CloseGamepad(controllers[evtCdevice.which].GamepadHandle);
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

        private void addJoystick(SDL_JoystickID instanceID)
        {
            // if the joystick is already opened, ignore it
            if (controllers.ContainsKey(instanceID))
                return;

            SDL_Joystick* joystick = SDL_OpenJoystick(instanceID);

            SDL_Gamepad* controller = null;
            if (SDL_IsGamepad(instanceID))
                controller = SDL_OpenGamepad(instanceID);

            controllers[instanceID] = new SDL3ControllerBindings(joystick, controller);
        }

        /// <summary>
        /// Populates <see cref="controllers"/> with joysticks that are already connected.
        /// </summary>
        private void populateJoysticks()
        {
            using var joysticks = SDL_GetJoysticks();

            if (joysticks == null)
                return;

            for (int i = 0; i < joysticks.Count; i++)
            {
                addJoystick(joysticks[i]);
            }
        }

        private void handleJoyDeviceEvent(SDL_JoyDeviceEvent evtJdevice)
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

                    SDL_CloseJoystick(controllers[evtJdevice.which].JoystickHandle);
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

            bool precise;

            if (isPrecise(evtWheel.x) || isPrecise(evtWheel.y))
            {
                precise = true;
                lastPreciseScroll = evtWheel.timestamp;
            }
            else
            {
                precise = evtWheel.timestamp < lastPreciseScroll + precise_scroll_debounce;
            }

            // SDL reports horizontal scroll opposite of what framework expects (in non-"natural" mode, scrolling to the right gives positive deltas while we want negative).
            TriggerMouseWheel(new Vector2(-evtWheel.x, evtWheel.y), precise);
        }

        private void handleMouseButtonEvent(SDL_MouseButtonEvent evtButton)
        {
            MouseButton button = mouseButtonFromEvent(evtButton.Button);
            SDL_MouseButtonFlags mask = SDL_BUTTON(evtButton.Button);
            Debug.Assert(Enum.IsDefined(mask));

            switch (evtButton.type)
            {
                case SDL_EventType.SDL_EVENT_MOUSE_BUTTON_DOWN:
                    MouseDown?.Invoke(button);
                    Interlocked.Or(ref pressedButtons, (uint)mask);
                    break;

                case SDL_EventType.SDL_EVENT_MOUSE_BUTTON_UP:
                    Interlocked.And(ref pressedButtons, (uint)~mask);
                    MouseUp?.Invoke(button);
                    break;
            }
        }

        private void handleMouseMotionEvent(SDL_MouseMotionEvent evtMotion)
        {
            if (!SDL_GetWindowRelativeMouseMode(SDLWindowHandle))
                MouseMove?.Invoke(new Vector2(evtMotion.x * Scale, evtMotion.y * Scale));
            else
                MouseMoveRelative?.Invoke(new Vector2(evtMotion.xrel * Scale, evtMotion.yrel * Scale));
        }

        private void handleTextInputEvent(SDL_TextInputEvent evtText)
        {
            string? text = evtText.GetText();
            Debug.Assert(text != null);
            TextInput?.Invoke(text);
        }

        private void handleTextEditingEvent(SDL_TextEditingEvent evtEdit)
        {
            string? text = evtEdit.GetText();
            Debug.Assert(text != null);
            TextEditing?.Invoke(text, evtEdit.start, evtEdit.length);
        }

        private void handleKeyboardEvent(SDL_KeyboardEvent evtKey)
        {
            Key key = evtKey.ToKey();

            if (key == Key.Unknown)
            {
                Logger.Log($"Unknown SDL key: {evtKey.scancode}, {evtKey.key}");
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

        private void handlePenMotionEvent(SDL_PenMotionEvent evtPenMotion)
        {
            PenMove?.Invoke(new Vector2(evtPenMotion.x, evtPenMotion.y) * Scale, evtPenMotion.pen_state.HasFlagFast(SDL_PenInputFlags.SDL_PEN_INPUT_DOWN));
        }

        private void handlePenTouchEvent(SDL_PenTouchEvent evtPenTouch)
        {
            PenTouch?.Invoke(evtPenTouch.down, new Vector2(evtPenTouch.x, evtPenTouch.y) * Scale);
        }

        /// <summary>
        /// The first SDL pen button as defined in https://wiki.libsdl.org/SDL3/SDL_PenButtonEvent.
        /// </summary>
        private const byte first_pen_button = 1;

        private void handlePenButtonEvent(SDL_PenButtonEvent evtPenButton)
        {
            var button = (TabletPenButton)(evtPenButton.button - first_pen_button);

            if (button >= TabletPenButton.Primary && button <= TabletPenButton.Button8)
                PenButton?.Invoke(button, evtPenButton.down);
            else
                Logger.Log($"Dropping SDL_PenButtonEvent with button index={evtPenButton.button} (out of range).");
        }

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

        private void mouseButtonFromPen(bool pressed, byte penButton, out MouseButton button, out SDL_MouseButtonFlags buttonFlag)
        {
            switch (penButton)
            {
                case 0:
                    button = MouseButton.Left;
                    buttonFlag = SDL_MouseButtonFlags.SDL_BUTTON_LMASK;
                    break;

                case 1:
                    button = MouseButton.Right;
                    buttonFlag = SDL_MouseButtonFlags.SDL_BUTTON_RMASK;
                    break;

                case 2:
                    button = MouseButton.Middle;
                    buttonFlag = SDL_MouseButtonFlags.SDL_BUTTON_MMASK;
                    break;

                case 3:
                    button = MouseButton.Button1;
                    buttonFlag = SDL_MouseButtonFlags.SDL_BUTTON_X1MASK;
                    break;

                case 4:
                    button = MouseButton.Button2;
                    buttonFlag = SDL_MouseButtonFlags.SDL_BUTTON_X2MASK;
                    break;

                default:
                    Logger.Log($"unknown pen button index: {penButton}, ignoring...");
                    button = MouseButton.Button3;
                    buttonFlag = 0;
                    break;
            }
        }

        #endregion

        /// <summary>
        /// Update the host window manager's cursor position based on a location relative to window coordinates.
        /// </summary>
        /// <param name="mousePosition">A position inside the window.</param>
        public void UpdateMousePosition(Vector2 mousePosition) => ScheduleCommand(() => SDL_WarpMouseInWindow(SDLWindowHandle, mousePosition.X / Scale, mousePosition.Y / Scale));

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

        /// <summary>
        /// Invoked when an IME text editing event occurs.
        /// </summary>
        public event TextEditingDelegate? TextEditing;

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

        /// <summary>
        /// Invoked when a pen moves. Passes pen position and whether the pen is touching the tablet surface.
        /// </summary>
        public event Action<Vector2, bool>? PenMove;

        /// <summary>
        /// Invoked when a pen touches (<c>true</c>) or lifts (<c>false</c>) from the tablet surface.
        /// Also passes the current position of the pen.
        /// </summary>
        public event Action<bool, Vector2>? PenTouch;

        /// <summary>
        /// Invoked when a <see cref="TabletPenButton">pen button</see> is pressed (<c>true</c>) or released (<c>false</c>).
        /// </summary>
        public event Action<TabletPenButton, bool>? PenButton;

        #endregion
    }
}
