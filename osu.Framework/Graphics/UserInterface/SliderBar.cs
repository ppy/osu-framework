// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using osu.Framework.Configuration;
using osu.Framework.Graphics.Containers;
using osu.Framework.Input;
using OpenTK;
using System.Diagnostics;
using osu.Framework.Input.Bindings;

namespace osu.Framework.Graphics.UserInterface
{
    public abstract class SliderBar<T> : Container, IHasCurrentValue<T>, IKeyBindingHandler<PlatformAction>
        where T : struct, IComparable, IConvertible
    {
        /// <summary>
        /// Range padding reduces the range of movement a slider bar is allowed to have
        /// while still receiving input in the padded region. This behavior is necessary
        /// for finite-sized nubs and can not be achieved (currently) by existing
        /// scene graph padding / margin functionality.
        /// </summary>
        public float RangePadding;

        public float UsableWidth => DrawWidth - 2 * RangePadding;

        private float smallKeyboardStep;
        private float defaultSmallKeyboardStep => (Convert.ToSingle(CurrentNumber.MaxValue) - Convert.ToSingle(CurrentNumber.MinValue)) / 20;
        /// <summary>
        /// A custom step value for each Left/Right key press which actuates a change on this control.
        /// </summary>
        public float SmallKeyboardStep
        {
            get => smallKeyboardStep != default(float) ? smallKeyboardStep : defaultSmallKeyboardStep;
            set => smallKeyboardStep = value;
        }

        private float largeKeyboardStep;
        private float defaultLargeKeyboardStep => (Convert.ToSingle(CurrentNumber.MaxValue) - Convert.ToSingle(CurrentNumber.MinValue)) / 4;
        /// <summary>
        /// A custom step value for each PgUp/PgDn key press which actuates a change on this control.
        /// </summary>
        public float LargeKeyboardStep
        {
            get => largeKeyboardStep != default(float) ? largeKeyboardStep : defaultLargeKeyboardStep;
            set => largeKeyboardStep = value;
        }

        protected readonly BindableNumber<T> CurrentNumber;

        public Bindable<T> Current => CurrentNumber;

        protected SliderBar()
        {
            if (typeof(T) == typeof(int))
                CurrentNumber = new BindableInt() as BindableNumber<T>;
            else if (typeof(T) == typeof(long))
                CurrentNumber = new BindableLong() as BindableNumber<T>;
            else if (typeof(T) == typeof(double))
                CurrentNumber = new BindableDouble() as BindableNumber<T>;
            else if (typeof(T) == typeof(float))
                CurrentNumber = new BindableFloat() as BindableNumber<T>;

            if (CurrentNumber == null)
                throw new NotSupportedException($"We don't support the generic type of {nameof(BindableNumber<T>)}.");

            CurrentNumber.ValueChanged += v => UpdateValue(NormalizedValue);
        }

        protected float NormalizedValue
        {
            get
            {
                if (Current == null)
                    return 0;

                if (!CurrentNumber.HasDefinedRange)
                    throw new InvalidOperationException($"A {nameof(SliderBar<T>)}'s {nameof(Current)} must have user-defined {nameof(BindableNumber<T>.MinValue)}"
                                                        + $" and {nameof(BindableNumber<T>.MaxValue)} to produce a valid {nameof(NormalizedValue)}.");

                var min = Convert.ToSingle(CurrentNumber.MinValue);
                var max = Convert.ToSingle(CurrentNumber.MaxValue);

                if (max - min == 0)
                    return 1;

                var val = Convert.ToSingle(CurrentNumber.Value);
                return (val - min) / (max - min);
            }
        }

        /// <summary>
        /// Triggered when the <see cref="Current"/> value has changed. Used to update the displayed value.
        /// </summary>
        /// <param name="value">The normalized <see cref="Current"/> value.</param>
        protected abstract void UpdateValue(float value);

        protected override void LoadComplete()
        {
            base.LoadComplete();
            UpdateValue(NormalizedValue);
        }

        protected override bool OnClick(InputState state)
        {
            handleMouseInput(state);
            return true;
        }

        protected override bool OnDrag(InputState state)
        {
            handleMouseInput(state);
            return true;
        }

        protected override bool OnDragStart(InputState state)
        {
            Trace.Assert(state.Mouse.PositionMouseDown.HasValue,
                $@"Can not start a {nameof(SliderBar<T>)} drag without knowing the mouse down position.");

            // ReSharper disable once PossibleInvalidOperationException
            Vector2 posDiff = state.Mouse.PositionMouseDown.Value - state.Mouse.Position;

            return Math.Abs(posDiff.X) > Math.Abs(posDiff.Y);
        }

        protected override bool OnDragEnd(InputState state) => true;

        public bool OnPressed(PlatformAction action)
        {
            if (!IsHovered || CurrentNumber.Disabled)
                return false;

            switch (action.ActionType)
            {
                case PlatformActionType.CharPrevious:
                    CurrentNumber.Add(CurrentNumber.IsInteger ? -(float)Math.Ceiling(SmallKeyboardStep) : -SmallKeyboardStep);
                    return OnInputHandled();
                case PlatformActionType.CharNext:
                    CurrentNumber.Add(CurrentNumber.IsInteger ? (float)Math.Ceiling(SmallKeyboardStep) : SmallKeyboardStep);
                    return OnInputHandled();
                case PlatformActionType.WordPrevious:
                    CurrentNumber.Add(CurrentNumber.IsInteger ? -(float)Math.Ceiling(LargeKeyboardStep) : -LargeKeyboardStep);
                    return OnInputHandled();
                case PlatformActionType.WordNext:
                    CurrentNumber.Add(CurrentNumber.IsInteger ? (float)Math.Ceiling(LargeKeyboardStep) : LargeKeyboardStep);
                    return OnInputHandled();
                default:
                    return false;
            }

            bool OnInputHandled()
            {
                OnUserChange();
                return true;
            }
        }

        public bool OnReleased(PlatformAction action)
        {
            if (!IsHovered || CurrentNumber.Disabled)
                return false;

            switch (action.ActionType)
            {
                case PlatformActionType.CharPrevious:
                case PlatformActionType.CharNext:
                case PlatformActionType.WordPrevious:
                case PlatformActionType.WordNext:
                    return true;
                default:
                    return false;
            }
        }

        private void handleMouseInput(InputState state)
        {
            var xPosition = ToLocalSpace(state?.Mouse.NativeState.Position ?? Vector2.Zero).X - RangePadding;

            if (!CurrentNumber.Disabled)
                CurrentNumber.SetProportional(xPosition / UsableWidth, state != null && state.Keyboard.ShiftPressed ? SmallKeyboardStep : 0);

            OnUserChange();
        }

        /// <summary>
        /// Triggered when the value is changed based on end-user input to this control.
        /// </summary>
        protected virtual void OnUserChange() { }
    }
}
