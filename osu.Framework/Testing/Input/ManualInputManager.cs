﻿// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Input;
using osu.Framework.Input.Handlers;
using osu.Framework.Input.StateChanges;
using osu.Framework.Platform;
using osuTK;
using osuTK.Input;

namespace osu.Framework.Testing.Input
{
    public class ManualInputManager : PassThroughInputManager
    {
        private readonly ManualInputHandler handler;

        protected override Container<Drawable> Content => content;

        private bool showVisualCursorGuide = true;

        /// <summary>
        /// Whether to show a visible cursor tracking position and clicks.
        /// Generally should be enabled unless it blocks the test's content.
        /// </summary>
        public bool ShowVisualCursorGuide
        {
            get => showVisualCursorGuide;
            set
            {
                if (value == showVisualCursorGuide)
                    return;

                showVisualCursorGuide = value;
                testCursor.State.Value = value ? Visibility.Visible : Visibility.Hidden;
            }
        }

        private readonly Container content;

        private readonly TestCursorContainer testCursor;

        public ManualInputManager()
        {
            UseParentInput = true;
            AddHandler(handler = new ManualInputHandler());

            InternalChildren = new Drawable[]
            {
                content = new Container { RelativeSizeAxes = Axes.Both },
                testCursor = new TestCursorContainer(),
            };
        }

        public void Input(IInput input)
        {
            UseParentInput = false;
            handler.EnqueueInput(input);
        }

        /// <summary>
        /// Press a key down. Release with <see cref="ReleaseKey"/>.
        /// </summary>
        /// <remarks>
        /// To press and release a key immediately, use <see cref="Key"/>.
        /// </remarks>
        /// <param name="key">The key to press.</param>
        public void PressKey(Key key) => Input(new KeyboardKeyInput(key, true));

        /// <summary>
        /// Release a pressed key.
        /// </summary>
        /// <param name="key">The key to release.</param>
        public void ReleaseKey(Key key) => Input(new KeyboardKeyInput(key, false));

        /// <summary>
        /// Press and release the specified key.
        /// </summary>
        /// <param name="key">The key to press and release.</param>
        public void Key(Key key)
        {
            PressKey(key);
            ReleaseKey(key);
        }

        public void ScrollBy(Vector2 delta, bool isPrecise = false) => Input(new MouseScrollRelativeInput { Delta = delta, IsPrecise = isPrecise });
        public void ScrollHorizontalBy(float delta, bool isPrecise = false) => ScrollBy(new Vector2(delta, 0), isPrecise);
        public void ScrollVerticalBy(float delta, bool isPrecise = false) => ScrollBy(new Vector2(0, delta), isPrecise);

        public void MoveMouseTo(Drawable drawable, Vector2? offset = null) => MoveMouseTo(drawable.ToScreenSpace(drawable.LayoutRectangle.Centre) + (offset ?? Vector2.Zero));
        public void MoveMouseTo(Vector2 position) => Input(new MousePositionAbsoluteInput { Position = position });

        public void MoveTouchTo(Touch touch) => Input(new TouchInput(touch, CurrentState.Touch.IsActive(touch.Source)));

        /// <summary>
        /// Press and release the specified button.
        /// </summary>
        /// <param name="button">The button to press and release.</param>
        public void Click(MouseButton button)
        {
            PressButton(button);
            ReleaseButton(button);
        }

        /// <summary>
        /// Press a mouse button down. Release with <see cref="ReleaseButton"/>.
        /// </summary>
        /// <remarks>
        /// To press and release a mouse button immediately, use <see cref="Click"/>.
        /// </remarks>
        /// <param name="button">The button to press.</param>
        public void PressButton(MouseButton button) => Input(new MouseButtonInput(button, true));

        /// <summary>
        /// Release a pressed mouse button.
        /// </summary>
        /// <param name="button">The button to release.</param>
        public void ReleaseButton(MouseButton button) => Input(new MouseButtonInput(button, false));

        public void PressJoystickButton(JoystickButton button) => Input(new JoystickButtonInput(button, true));
        public void ReleaseJoystickButton(JoystickButton button) => Input(new JoystickButtonInput(button, false));

        public void BeginTouch(Touch touch) => Input(new TouchInput(touch, true));
        public void EndTouch(Touch touch) => Input(new TouchInput(touch, false));

        public void PressMidiKey(MidiKey key, byte velocity) => Input(new MidiKeyInput(key, velocity, true));
        public void ReleaseMidiKey(MidiKey key, byte velocity) => Input(new MidiKeyInput(key, velocity, false));

        private class ManualInputHandler : InputHandler
        {
            public override bool Initialize(GameHost host) => true;
            public override bool IsActive => true;
            public override int Priority => 0;

            public void EnqueueInput(IInput input)
            {
                PendingInputs.Enqueue(input);
            }
        }
    }
}
