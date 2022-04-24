// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Bindables;
using osu.Framework.Graphics.Containers;
using osuTK.Input;
using osuTK;
using osu.Framework.Input.Events;

namespace osu.Framework.Graphics.UserInterface
{
    public abstract class SliderBar<T> : Container, IHasCurrentValue<T>
        where T : struct, IComparable<T>, IConvertible, IEquatable<T>
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
                if (value == null)
                    throw new ArgumentNullException(nameof(value));

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
            current.DisabledChanged += v => currentNumberInstantaneous.Disabled = v;

            currentNumberInstantaneous.ValueChanged += e =>
            {
                if (!TransferValueOnCommit)
                    current.Value = e.NewValue;
            };
        }

        protected float NormalizedValue
        {
            get
            {
                if (!currentNumberInstantaneous.HasDefinedRange)
                {
                    throw new InvalidOperationException($"A {nameof(SliderBar<T>)}'s {nameof(Current)} must have user-defined {nameof(BindableNumber<T>.MinValue)}"
                                                        + $" and {nameof(BindableNumber<T>.MaxValue)} to produce a valid {nameof(NormalizedValue)}.");
                }

                float min = Convert.ToSingle(currentNumberInstantaneous.MinValue);
                float max = Convert.ToSingle(currentNumberInstantaneous.MaxValue);

                if (max - min == 0)
                    return 1;

                float val = Convert.ToSingle(currentNumberInstantaneous.Value);
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

        protected override bool OnMouseDown(MouseDownEvent e)
        {
            handleClick = true;
            return base.OnMouseDown(e);
        }

        protected override bool OnClick(ClickEvent e)
        {
            if (handleClick)
            {
                handleMouseInput(e);
                commit();
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

            handleMouseInput(e);
            return true;
        }

        protected override void OnDragEnd(DragEndEvent e)
        {
            handleMouseInput(e);
            commit();
        }

        protected override bool OnKeyDown(KeyDownEvent e)
        {
            if (currentNumberInstantaneous.Disabled)
                return false;

            if (!IsHovered)
                return false;

            float step = KeyboardStep != 0 ? KeyboardStep : (Convert.ToSingle(currentNumberInstantaneous.MaxValue) - Convert.ToSingle(currentNumberInstantaneous.MinValue)) / 20;
            if (currentNumberInstantaneous.IsInteger) step = MathF.Ceiling(step);

            switch (e.Key)
            {
                case Key.Right:
                    currentNumberInstantaneous.Add(step);
                    onUserChange(currentNumberInstantaneous.Value);
                    return true;

                case Key.Left:
                    currentNumberInstantaneous.Add(-step);
                    onUserChange(currentNumberInstantaneous.Value);
                    return true;

                default:
                    return false;
            }
        }

        protected override void OnKeyUp(KeyUpEvent e)
        {
            if (e.Key == Key.Left || e.Key == Key.Right)
                commit();
        }

        private bool uncommittedChanges;

        private bool commit()
        {
            if (!uncommittedChanges)
                return false;

            current.Value = currentNumberInstantaneous.Value;
            uncommittedChanges = false;
            return true;
        }

        private void handleMouseInput(UIEvent e)
        {
            float xPosition = ToLocalSpace(e.ScreenSpaceMousePosition).X - RangePadding;

            if (currentNumberInstantaneous.Disabled)
                return;

            currentNumberInstantaneous.SetProportional(xPosition / UsableWidth, e.ShiftPressed ? KeyboardStep : 0);
            onUserChange(currentNumberInstantaneous.Value);
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
