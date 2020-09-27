﻿// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Bindables;
using osu.Framework.Utils;

namespace osu.Framework.Graphics.Transforms
{
    internal class TransformBindable<TValue, TEasing, T> : Transform<TValue, TEasing, T>
        where T : class, ITransformable
        where TEasing : IEasingFunction
    {
        public override string TargetMember { get; }

        private readonly Bindable<TValue> targetBindable;
        private readonly InterpolationFunc<TValue, TEasing> interpolationFunc;

        public TransformBindable(Bindable<TValue> targetBindable)
        {
            this.targetBindable = targetBindable;

            // Lambda expression is used so that the delegate is cached (see: https://github.com/dotnet/roslyn/issues/5835)
            interpolationFunc = (double d, TValue value, TValue tValue, double time, double endTime, in TEasing type)
                => Interpolation.ValueAt(d, value, tValue, time, endTime, in type);

            TargetMember = $"{targetBindable.GetHashCode()}.Value";
        }

        private TValue valueAt(double time)
        {
            if (time < StartTime) return StartValue;
            if (time >= EndTime) return EndValue;

            return interpolationFunc(time, StartValue, EndValue, StartTime, EndTime, Easing);
        }

        protected override void Apply(T d, double time) => targetBindable.Value = valueAt(time);
        protected override void ReadValues(T d) => StartValue = targetBindable.Value;
    }
}
