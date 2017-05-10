﻿// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using osu.Framework.Configuration;
using osu.Framework.Graphics.Containers;
using osu.Framework.Input;
using OpenTK.Input;
using OpenTK;
using System.Diagnostics;

namespace osu.Framework.Graphics.UserInterface
{
    public abstract class SliderBar<T> : Container, IHasCurrentValue<T>
        where T : struct
    {
        /// <summary>
        /// Range padding reduces the range of movement a slider bar is allowed to have
        /// while still receiving input in the padded region. This behavior is necessary
        /// for finite-sized nubs and can not be achieved (currently) by existing
        /// scene graph padding / margin functionality.
        /// </summary>
        public float RangePadding;

        public float UsableWidth => DrawWidth - 2 * RangePadding;

        /// <summary>
        /// A custom step value for each key press which actuates a change on this control.
        /// </summary>
        public float KeyboardStep;

        protected readonly BindableNumber<T> CurrentNumber;

        public Bindable<T> Current => CurrentNumber;

        protected bool PlaySound;

        protected SliderBar()
        {
            if (typeof(T) == typeof(int))
                CurrentNumber = new BindableInt() as BindableNumber<T>;
            else if (typeof(T) == typeof(long))
                CurrentNumber = new BindableLong() as BindableNumber<T>;
            else if (typeof(T) == typeof(double))
                CurrentNumber = new BindableDouble() as BindableNumber<T>;

            if (CurrentNumber == null) throw new NotSupportedException($"We don't support the generic type of {nameof(BindableNumber<T>)}.");

            CurrentNumber.ValueChanged += v => UpdateValue(NormalizedValue);
        }

        protected float NormalizedValue
        {
            get
            {
                if (Current == null)
                    return 0;
                var min = Convert.ToSingle(CurrentNumber.MinValue);
                var max = Convert.ToSingle(CurrentNumber.MaxValue);
                var val = Convert.ToSingle(CurrentNumber.Value);
                return (val - min) / (max - min);
            }
        }

        protected abstract void UpdateValue(float value);

        protected override void LoadComplete()
        {
            base.LoadComplete();
            PlaySound = false;
            UpdateValue(NormalizedValue);
            PlaySound = true;
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

            Vector2 posDiff = state.Mouse.PositionMouseDown.Value - state.Mouse.Position;

            return Math.Abs(posDiff.X) > Math.Abs(posDiff.Y);
        }

        protected override bool OnDragEnd(InputState state) => true;

        protected override bool OnKeyDown(InputState state, KeyDownEventArgs args)
        {
            if (!Hovering)
                return false;

            var step = KeyboardStep != 0 ? KeyboardStep : (Convert.ToSingle(CurrentNumber.MaxValue) - Convert.ToSingle(CurrentNumber.MinValue)) / 20;
            if (CurrentNumber.IsInteger) step = (float)Math.Ceiling(step);

            switch (args.Key)
            {
                case Key.Right:
                    CurrentNumber.Add(step);
                    OnUserChange();
                    return true;
                case Key.Left:
                    CurrentNumber.Add(-step);
                    OnUserChange();
                    return true;
                default:
                    return false;
            }
        }

        private void handleMouseInput(InputState state)
        {
            var xPosition = ToLocalSpace(state.Mouse.NativeState.Position).X - RangePadding;

            if (!CurrentNumber.Disabled)
                CurrentNumber.SetProportional(xPosition / UsableWidth);

            OnUserChange();
        }

        /// <summary>
        /// Triggered when the value is changed based on end-user input to this control.
        /// </summary>
        protected virtual void OnUserChange() { }
    }
}
