// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Bindables;
using osu.Framework.Graphics.Transforms;

namespace osu.Framework.Graphics.UserInterface
{
    public class CircularProgress : Annulus, IHasCurrentValue<double>
    {
        private readonly Bindable<double> progressStartAngle = new Bindable<double>();
        private readonly Bindable<double> progressEndAngle = new Bindable<double>();
        private readonly Bindable<double> current = new Bindable<double>();

        public Bindable<double> ProgressStartAngle
        {
            get => progressStartAngle;
            set
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(value));

                progressStartAngle.UnbindBindings();
                progressStartAngle.BindTo(value);
            }
        }

        public Bindable<double> ProgressEndAngle
        {
            get => progressEndAngle;
            set
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(value));

                progressEndAngle.UnbindBindings();
                progressEndAngle.BindTo(value);
            }
        }

        public Bindable<double> Current
        {
            get => current;
            set
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(value));

                current.UnbindBindings();
                current.BindTo(value);
            }
        }

        public CircularProgress()
        {
            ProgressStartAngle.ValueChanged += recalculateAngles;
            ProgressEndAngle.ValueChanged += recalculateAngles;
            Current.ValueChanged += recalculateAngles;
        }

        private void recalculateAngles(ValueChangedEvent<double> args)
        {
            StartAngle.Value = ProgressStartAngle.Value;
            EndAngle.Value = ProgressStartAngle.Value + Current.Value * (ProgressEndAngle.Value - ProgressStartAngle.Value);

            Invalidate(Invalidation.DrawNode);
        }

        public TransformSequence<CircularProgress> FillTo(double newValue, double duration = 0, Easing easing = Easing.None)
            => this.TransformBindableTo(Current, newValue, duration, easing);
    }

    public static class CircularProgressExtensions
    {
        public static TransformSequence<CircularProgress> FillTo(this CircularProgress t, double newValue, double duration = 0, Easing easing = Easing.None)
            => t.TransformBindableTo(t.Current, newValue, duration, easing);
    }
}
