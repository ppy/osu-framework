// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu/master/LICENCE

using osu.Framework.Configuration;
using osu.Framework.MathUtils;

namespace osu.Framework.Graphics.Transforms
{
    internal class TransformBindable<TValue, T> : Transform<TValue, T>
        where T : ITransformable
    {
        public override string TargetMember { get; }

        private readonly Bindable<TValue> targetBindable;

        public TransformBindable(Bindable<TValue> targetBindable)
        {
            this.targetBindable = targetBindable;
            TargetMember = $"{targetBindable.GetHashCode()}.Value";
        }

        private TValue valueAt(double time)
        {
            if (time < StartTime) return StartValue;
            if (time >= EndTime) return EndValue;

            return Interpolation<TValue>.ValueAt(time, StartValue, EndValue, StartTime, EndTime, Easing);
        }

        protected override void Apply(T d, double time) => targetBindable.Value = valueAt(time);
        protected override void ReadIntoStartValue(T d) => StartValue = targetBindable.Value;
    }
}
