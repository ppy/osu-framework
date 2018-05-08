// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Configuration;
using osu.Framework.MathUtils;

namespace osu.Framework.Graphics.Transforms
{
    internal class TransformBindable<TValue, T> : Transform<TValue, T>
        where T : ITransformable
    {
        public override string TargetMember { get; }

        private readonly Bindable<TValue> targetBindable;
        private readonly InterpolationFunc<TValue> interpolationFunc;

        public TransformBindable(Bindable<TValue> targetBindable, InterpolationFunc<TValue> interpolationFunc)
        {
            this.targetBindable = targetBindable;
            this.interpolationFunc = interpolationFunc ?? Interpolation<TValue>.ValueAt;

            TargetMember = $"{targetBindable.GetHashCode()}.Value";
        }

        private TValue valueAt(double time)
        {
            if (time < StartTime) return StartValue;
            if (time >= EndTime) return EndValue;

            return interpolationFunc(time, StartValue, EndValue, StartTime, EndTime, Easing);
        }

        protected override void Apply(T d, double time) => targetBindable.Value = valueAt(time);
        protected override void ReadIntoStartValue(T d) => StartValue = targetBindable.Value;
    }
}
