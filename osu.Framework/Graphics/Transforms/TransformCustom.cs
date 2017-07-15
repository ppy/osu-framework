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
        private delegate void TransformAction(TransformCustom<TValue, T> transform, T transformable);

        private static Dictionary<string, TransformAction> applyMethods = new Dictionary<string, TransformAction>();
        private static Dictionary<string, TransformAction> readIntoStartValueMethods = new Dictionary<string, TransformAction>();
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

        private static TransformAction getApplyAction(string propertyOrFieldName)
        {
            TransformAction result;
            if (applyMethods.TryGetValue(propertyOrFieldName, out result))
                return result;

            var method = new DynamicMethod(
                $"{typeof(T).ReadableName()}_{propertyOrFieldName}_{Guid.NewGuid().ToString("N")}",
                typeof(void),
                new[] { typeof(TransformCustom<TValue, T>), typeof(T) },
                true
            );

            var property = typeof(T).GetProperty(propertyOrFieldName);
            var currentValueProperty = typeof(TransformCustom<TValue, T>).GetProperty(nameof(currentValue), BindingFlags.NonPublic | BindingFlags.Instance);

            var ilGen = method.GetILGenerator();
            ilGen.Emit(OpCodes.Ldarg_1);
            ilGen.Emit(OpCodes.Ldarg_0);
            ilGen.Emit(OpCodes.Callvirt, currentValueProperty.GetMethod);
            ilGen.Emit(OpCodes.Callvirt, property.SetMethod);
            ilGen.Emit(OpCodes.Ret);

            result = (TransformAction)method.CreateDelegate(typeof(TransformAction));
            applyMethods.Add(propertyOrFieldName, result);

            return result;
        }

        private static TransformAction getReadIntoStartValueAction(string propertyOrFieldName)
        {
            TransformAction result;
            if (readIntoStartValueMethods.TryGetValue(propertyOrFieldName, out result))
                return result;

            var method = new DynamicMethod(
                $"{typeof(T).ReadableName()}_{propertyOrFieldName}_{Guid.NewGuid().ToString("N")}",
                typeof(void),
                new[] { typeof(TransformCustom<TValue, T>), typeof(T) },
                true
            );

            var property = typeof(T).GetProperty(propertyOrFieldName);
            var startValueProperty = typeof(Transform<TValue, T>).GetProperty(nameof(StartValue), BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            var ilGen = method.GetILGenerator();
            ilGen.Emit(OpCodes.Ldarg_0);
            ilGen.Emit(OpCodes.Ldarg_1);
            ilGen.Emit(OpCodes.Callvirt, property.GetMethod);
            ilGen.Emit(OpCodes.Callvirt, startValueProperty.SetMethod);
            ilGen.Emit(OpCodes.Ret);

            result = (TransformAction)method.CreateDelegate(typeof(TransformAction));
            readIntoStartValueMethods.Add(propertyOrFieldName, result);

            return result;
        }

        private TransformAction applyAction;
        private TransformAction readIntoStartValueAction;
        private CurrentValueFunc<TValue> currentValueFunc;

        public TransformCustom(string propertyOrFieldName, CurrentValueFunc<TValue> currentValueFunc = null)
        {
            applyAction = getApplyAction(propertyOrFieldName);
            readIntoStartValueAction = getReadIntoStartValueAction(propertyOrFieldName);
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

        public override void Apply(T d) => applyAction(this, d);

        public override void ReadIntoStartValue(T d) => readIntoStartValueAction(this, d);
    }
}
