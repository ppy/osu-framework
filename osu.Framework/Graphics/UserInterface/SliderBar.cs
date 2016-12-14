using System;
using osu.Framework.Configuration;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Transformations;
using osu.Framework.Input;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Input;

namespace osu.Framework.Graphics.UserInterface
{
    public abstract class SliderBar<T> : Container where T : struct,
        IComparable, IFormattable, IConvertible, IComparable<T>, IEquatable<T>
    {
        protected enum SliderBarEventSource
        {
            External,
            Keyboard,
            Mouse,
        }
    
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
        private bool stepInitialized = false;

        private SliderBarEventSource eventSource = SliderBarEventSource.External;

        public BindableNumber<T> Bindable
        {
            get { return bindable; }
            set
            {
                if (bindable != null)
                    bindable.ValueChanged -= bindableValueChanged;
                bindable = value;
                bindable.ValueChanged += bindableValueChanged;
                UpdateValue(NormalizedValue, eventSource);
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

        protected abstract void UpdateValue(float value, SliderBarEventSource eventSource);
        
        protected override void Dispose(bool isDisposing)
        {
            if (Bindable != null)
                Bindable.ValueChanged -= bindableValueChanged;
            base.Dispose(isDisposing);
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();
            UpdateValue(NormalizedValue, eventSource);
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

        protected override bool OnDragStart(InputState state) => true;

        protected override bool OnDragEnd(InputState state) => true;

        protected override bool OnKeyDown(InputState state, KeyDownEventArgs args)
        {
            if (!stepInitialized)
                KeyboardStep = (Convert.ToSingle(Bindable.MaxValue) - Convert.ToSingle(Bindable.MinValue)) / 20;
            var step = KeyboardStep;
            if (Bindable.IsInteger)
                step = (float)Math.Ceiling(step);
            eventSource = SliderBarEventSource.Keyboard;
            try
            {
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
            finally
            {
                eventSource = SliderBarEventSource.External;
            }
        }

        private void bindableValueChanged(object sender, EventArgs e) => UpdateValue(NormalizedValue, eventSource);

        private void handleMouseInput(InputState state)
        {
            var xPosition = ToLocalSpace(state.Mouse.NativeState.Position).X;
            eventSource = SliderBarEventSource.Mouse;
            try
            {
                Bindable.SetProportional(xPosition / DrawWidth);
            }
            finally
            {
                eventSource = SliderBarEventSource.External;
            }
        }
    }
}
