// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Numerics;
using osu.Framework.Bindables;
using osu.Framework.Graphics.Containers;
using osu.Framework.Input.Events;
using osuTK.Input;
using Vector2 = osuTK.Vector2;

namespace osu.Framework.Graphics.UserInterface
{
    public abstract partial class SliderBar<T> : Container, IHasCurrentValue<T>
        where T : struct, INumber<T>, IMinMaxValue<T>
    {
        /// <summary>
        /// Range padding reduces the range of movement a slider bar is allowed to have
        /// while still receiving input in the padded region. This behavior is necessary
        /// for finite-sized nubs and can not be achieved (currently) by existing
        /// scene graph padding / margin functionality.
        /// </summary>
        public float RangePadding;

        public float UsableWidth => DrawWidth - 2 * RangePadding;

        private T mouseStep;

        /// <summary>
        /// A custom step value for mouse input which actuates a change on this control.
        /// </summary>
        public T MouseStep
        {
            get => mouseStep;
            set
            {
                T multiple = value / currentNumberInstantaneous.Precision;
                if (!T.IsNaN(multiple) && !T.IsInfinity(multiple) && !T.IsZero(multiple % T.One))
                    throw new ArgumentException("Mouse step must be a multiple of the bindable precision.");

                mouseStep = value;
            }
        }

        private T keyboardStep;

        /// <summary>
        /// A custom step value for each key press which actuates a change on this control.
        /// </summary>
        public T KeyboardStep
        {
            get => keyboardStep;
            set
            {
                T multiple = value / currentNumberInstantaneous.Precision;
                if (!T.IsNaN(multiple) && !T.IsInfinity(multiple) && !T.IsZero(multiple % T.One))
                    throw new ArgumentException("Keyboard step must be a multiple of the bindable precision.");

                keyboardStep = value;
            }
        }

        private T minValue = T.MinValue;

        public T MinValue
        {
            get => T.Max(minValue, currentNumberInstantaneous.MinValue);
            set
            {
                if (value < currentNumberInstantaneous.MinValue)
                    throw new ArgumentException("Minimum value override must be greater than or equal to the bindable minimum value.");

                if (EqualityComparer<T>.Default.Equals(value, minValue))
                    return;

                minValue = value;
                Scheduler.AddOnce(updateValue);
            }
        }

        private T maxValue = T.MaxValue;

        public T MaxValue
        {
            get => T.Min(maxValue, currentNumberInstantaneous.MaxValue);
            set
            {
                if (value > currentNumberInstantaneous.MaxValue)
                    throw new ArgumentException("Maximum value override must be less than or equal to the bindable maximum value.");

                if (EqualityComparer<T>.Default.Equals(value, maxValue))
                    return;

                maxValue = value;
                Scheduler.AddOnce(updateValue);
            }
        }

        private readonly BindableNumber<T> currentNumberInstantaneous;

        /// <summary>
        /// When set, value changes based on user input are only transferred to any bound <see cref="Current"/> on commit.
        /// This is useful if the UI interaction could be adversely affected by the value changing, such as the position of the <see cref="SliderBar{T}"/> on the screen.
        /// </summary>
        public bool TransferValueOnCommit;

        private readonly BindableNumberWithCurrent<T> current = new BindableNumberWithCurrent<T>();

        protected BindableNumber<T> CurrentNumber => current;

        public Bindable<T> Current
        {
            get => current;
            set
            {
                ArgumentNullException.ThrowIfNull(value);

                current.Current = value;

                currentNumberInstantaneous.Default = current.Default;
            }
        }

        protected SliderBar()
        {
            currentNumberInstantaneous = new BindableNumber<T>();

            current.ValueChanged += e => currentNumberInstantaneous.Value = e.NewValue;
            current.MinValueChanged += v => currentNumberInstantaneous.MinValue = v;
            current.MaxValueChanged += v => currentNumberInstantaneous.MaxValue = v;
            current.PrecisionChanged += v => currentNumberInstantaneous.Precision = v;
            current.DisabledChanged += disabled =>
            {
                if (disabled)
                {
                    // revert any changes before disabling to make sure we are in a consistent state.
                    currentNumberInstantaneous.Value = current.Value;
                    uncommittedChanges = false;
                }

                currentNumberInstantaneous.Disabled = disabled;
            };

            currentNumberInstantaneous.ValueChanged += e =>
            {
                if (!TransferValueOnCommit)
                    current.Value = e.NewValue;
            };
        }

        protected bool HasDefinedRange => !EqualityComparer<T>.Default.Equals(MinValue, T.MinValue) ||
                                          !EqualityComparer<T>.Default.Equals(MaxValue, T.MaxValue);

        protected float NormalizedValue
        {
            get
            {
                if (!HasDefinedRange)
                {
                    throw new InvalidOperationException($"A {nameof(SliderBar<T>)} must have user-defined {nameof(MinValue)} and {nameof(MaxValue)}"
                                                        + $" or {nameof(Current)} must have user-defined {nameof(BindableNumber<T>.MinValue)}"
                                                        + $" and {nameof(BindableNumber<T>.MaxValue)} to produce a valid {nameof(NormalizedValue)}.");
                }

                float min = float.CreateTruncating(MinValue);
                float max = float.CreateTruncating(MaxValue);

                if (max - min == 0)
                    return 1;

                float val = float.CreateTruncating(currentNumberInstantaneous.Value);
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

            currentNumberInstantaneous.ValueChanged += _ => Scheduler.AddOnce(updateValue);
            currentNumberInstantaneous.MinValueChanged += _ => Scheduler.AddOnce(updateValue);
            currentNumberInstantaneous.MaxValueChanged += _ => Scheduler.AddOnce(updateValue);

            Scheduler.AddOnce(updateValue);
        }

        private void updateValue() => UpdateValue(NormalizedValue);

        private bool handleClick;
        private float? relativeValueAtMouseDown;

        protected override bool OnMouseDown(MouseDownEvent e)
        {
            if (ShouldHandleAsRelativeDrag(e))
            {
                relativeValueAtMouseDown = NormalizedValue;

                // Click shouldn't be handled if relative dragging is happening (i.e. while holding a nub).
                // This is generally an expectation by most OSes and UIs.
                handleClick = false;
            }
            else
            {
                handleClick = true;
                relativeValueAtMouseDown = null;
            }

            return base.OnMouseDown(e);
        }

        protected override bool OnClick(ClickEvent e)
        {
            if (handleClick)
            {
                handleMouseInput(e);
                Commit();
            }

            return true;
        }

        protected override void OnDrag(DragEvent e)
        {
            handleMouseInput(e);
        }

        protected override bool OnDragStart(DragStartEvent e)
        {
            Vector2 posDiff = e.MouseDownPosition - e.MousePosition;

            if (Math.Abs(posDiff.X) < Math.Abs(posDiff.Y))
            {
                handleClick = false;
                return false;
            }

            GetContainingFocusManager()?.ChangeFocus(this);
            handleMouseInput(e);
            return true;
        }

        protected override void OnDragEnd(DragEndEvent e) => Commit();

        public override bool AcceptsFocus => true;

        protected override bool OnKeyDown(KeyDownEvent e)
        {
            if (currentNumberInstantaneous.Disabled)
                return false;

            if (!IsHovered && !HasFocus)
                return false;

            T step = !T.IsZero(KeyboardStep) ? KeyboardStep : (MaxValue - MinValue) / T.CreateTruncating(20);
            if (currentNumberInstantaneous.IsInteger) step = T.Max(step, T.One);

            T clampedCurrent = clamp(currentNumberInstantaneous.Value);

            switch (e.Key)
            {
                case Key.Right:
                    currentNumberInstantaneous.Set(clamp(clampedCurrent + step));
                    onUserChange(currentNumberInstantaneous.Value);
                    return true;

                case Key.Left:
                    currentNumberInstantaneous.Set(clamp(clampedCurrent - step));
                    onUserChange(currentNumberInstantaneous.Value);
                    return true;

                default:
                    return false;
            }
        }

        private T clamp(T value) => T.Clamp(value, MinValue, MaxValue);

        protected override void OnKeyUp(KeyUpEvent e)
        {
            if (e.Key == Key.Left || e.Key == Key.Right)
                Commit();
        }

        private bool uncommittedChanges;

        protected virtual bool Commit()
        {
            if (!uncommittedChanges)
                return false;

            current.Value = currentNumberInstantaneous.Value;
            uncommittedChanges = false;
            return true;
        }

        /// <summary>
        /// Whether mouse handling should be relative to the distance travelled, or absolute in line with the exact position of the cursor.
        /// </summary>
        /// <remarks>
        /// Generally, this should be overridden and return <c>true</c> when the cursor is hovering a "nub" or "thumb" portion at the point of mouse down
        /// to give the user more correct control.
        /// </remarks>
        /// <param name="e">The mouse down event.</param>
        /// <returns>Whether to perform a relative drag.</returns>
        protected virtual bool ShouldHandleAsRelativeDrag(MouseDownEvent e) => false;

        private void handleMouseInput(MouseButtonEvent e)
        {
            if (currentNumberInstantaneous.Disabled)
                return;

            float localX = ToLocalSpace(e.ScreenSpaceMousePosition).X;

            float newValue;

            if (relativeValueAtMouseDown != null && e is DragEvent drag)
            {
                newValue = relativeValueAtMouseDown.Value + (localX - ToLocalSpace(drag.ScreenSpaceMouseDownPosition).X) / UsableWidth;
            }
            else
            {
                newValue = (localX - RangePadding) / UsableWidth;
            }

            float snap = e.ShiftPressed ? float.CreateTruncating(KeyboardStep) : float.CreateTruncating(MouseStep);
            setProportional(newValue, snap);
            onUserChange(currentNumberInstantaneous.Value);
        }

        /// <summary>
        /// Sets the value of <see cref="currentNumberInstantaneous"/> to <see cref="MinValue"/> + (<see cref="MaxValue"/> - <see cref="MinValue"/>) * amt
        /// <param name="amt">The proportional amount to set, ranging from 0 to 1.</param>
        /// <param name="snap">If greater than 0, snap the final value to the closest multiple of this number.</param>
        /// </summary>
        private void setProportional(float amt, float snap = 0)
        {
            double min = double.CreateTruncating(MinValue);
            double max = double.CreateTruncating(MaxValue);
            double value = min + (max - min) * amt;
            if (snap > 0)
                value = Math.Round(value / snap) * snap;
            value = Math.Clamp(value, min, max);
            currentNumberInstantaneous.Set(value);
        }

        private void onUserChange(T value)
        {
            uncommittedChanges = true;
            OnUserChange(value);
        }

        /// <summary>
        /// Triggered when the value is changed based on end-user input to this control.
        /// </summary>
        protected virtual void OnUserChange(T value)
        {
        }
    }
}
