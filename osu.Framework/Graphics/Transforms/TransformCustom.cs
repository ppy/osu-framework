// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.MathUtils;
using System.Collections.Generic;
using System;
using System.Reflection.Emit;
using osu.Framework.Extensions.TypeExtensions;
using System.Reflection;
using System.Linq;

namespace osu.Framework.Graphics.Transforms
{
    public delegate TValue CurrentValueFunc<TValue>(double time, TValue startValue, TValue endValue, double startTime, double endTime, EasingTypes easingType);

    public class TransformCustom<TValue, T> : Transform<TValue, T>
    {
        private delegate TValue ReadFunc(T transformable);
        private delegate void WriteFunc(T transformable, TValue value);

        private struct Accessor
        {
            public ReadFunc Read;
            public WriteFunc Write;
        }

        private static Dictionary<string, Accessor> accessors = new Dictionary<string, Accessor>();
        private static readonly CurrentValueFunc<TValue> current_value_func;

        static TransformCustom()
        {
            current_value_func =
                (CurrentValueFunc<TValue>)typeof(Interpolation).GetMethod(
                    nameof(Interpolation.ValueAt),
                    typeof(CurrentValueFunc<TValue>)
                        .GetMethod(nameof(CurrentValueFunc<TValue>.Invoke))
                        .GetParameters().Select(p => p.ParameterType).ToArray()
                )?.CreateDelegate(typeof(CurrentValueFunc<TValue>));
        }

        private static Accessor getAccessor(string propertyOrFieldName)
        {
            Accessor result;
            if (accessors.TryGetValue(propertyOrFieldName, out result))
                return result;

            var property = typeof(T).GetProperty(propertyOrFieldName);
            result.Write = (WriteFunc)property.GetSetMethod(true).CreateDelegate(typeof(WriteFunc));
            result.Read = (ReadFunc)property.GetGetMethod(true).CreateDelegate(typeof(ReadFunc));

            accessors.Add(propertyOrFieldName, result);

            return result;
        }

        private Accessor accessor;
        private CurrentValueFunc<TValue> currentValueFunc;

        public TransformCustom(string propertyOrFieldName, CurrentValueFunc<TValue> currentValueFunc = null)
        {
            TargetMember = propertyOrFieldName;

            accessor = getAccessor(propertyOrFieldName);
            this.currentValueFunc = currentValueFunc ?? current_value_func;
        }

        private TValue currentValue
        {
            get
            {
                double time = Time?.Current ?? 0;
                if (time < StartTime) return StartValue;
                if (time >= EndTime) return EndValue;

                return currentValueFunc(time, StartValue, EndValue, StartTime, EndTime, Easing);
            }
        }

        public override string TargetMember { get; }

        public override void Apply(T d) => accessor.Write(d, currentValue);

        public override void ReadIntoStartValue(T d) => StartValue = accessor.Read(d);
    }
}
