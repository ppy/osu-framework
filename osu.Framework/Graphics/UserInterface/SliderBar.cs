// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
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
    public abstract class SliderBar<T> : Container where T : struct
    {
        /// <summary>
        /// Range padding reduces the range of movement a slider bar is allowed to have
        /// while still receiving input in the padded region. This behavior is necessary
        /// for finite-sized nubs and can not be achieved (currently) by existing
        /// scene graph padding / margin functionality.
        /// </summary>
        public float RangePadding;
        public float UsableWidth => DrawWidth - 2 * RangePadding;

        private float keyboardStep;
        public float KeyboardStep
        {
            get { return keyboardStep; }
            set
            {
                keyboardStep = value;
                stepInitialized = true;
            }
        }
        private bool stepInitialized;

        public BindableNumber<T> Bindable
        {
            get { return bindable; }
            set
            {
                if (bindable != null)
                    bindable.ValueChanged -= bindableValueChanged;
                bindable = value;
                bindable.ValueChanged += bindableValueChanged;
                UpdateValue(NormalizedValue);
            }
        }

        protected float NormalizedValue
        {
            get
            {
                if (Bindable == null)
                    return 0;
                var min = Convert.ToSingle(Bindable.MinValue);
                var max = Convert.ToSingle(Bindable.MaxValue);
                var val = Convert.ToSingle(Bindable.Value);
                return (val - min) / (max - min);
            }
        }

        private BindableNumber<T> bindable;

        protected abstract void UpdateValue(float value);

        protected override void Dispose(bool isDisposing)
        {
            if (Bindable != null)
                Bindable.ValueChanged -= bindableValueChanged;
            base.Dispose(isDisposing);
        }

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

            Vector2 posDiff = state.Mouse.PositionMouseDown.Value - state.Mouse.Position;

            return Math.Abs(posDiff.X) > Math.Abs(posDiff.Y);
        }

        protected override bool OnDragEnd(InputState state) => true;

        protected override bool OnKeyDown(InputState state, KeyDownEventArgs args)
        {
            if (!Hovering)
                return false;
            if (!stepInitialized)
                KeyboardStep = (Convert.ToSingle(Bindable.MaxValue) - Convert.ToSingle(Bindable.MinValue)) / 20;
            var step = KeyboardStep;
            if (Bindable.IsInteger)
                step = (float)Math.Ceiling(step);
            switch (args.Key)
            {
                case Key.Right:
                    Bindable.Add(step);
                    return true;
                case Key.Left:
                    Bindable.Add(-step);
                    return true;
                default:
                    return false;
            }
        }

        private void bindableValueChanged(object sender, EventArgs e) => UpdateValue(NormalizedValue);

        private void handleMouseInput(InputState state)
        {
            var xPosition = ToLocalSpace(state.Mouse.NativeState.Position).X - RangePadding;
            Bindable.SetProportional(xPosition / UsableWidth);
        }
    }
}
