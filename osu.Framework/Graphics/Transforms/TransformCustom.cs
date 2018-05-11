// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.MathUtils;
using System;
using System.Collections.Concurrent;
using System.Reflection.Emit;
using osu.Framework.Extensions.TypeExtensions;
using System.Reflection;
using System.Diagnostics;

namespace osu.Framework.Graphics.Transforms
{
    /// <summary>
    /// A transform which operates on arbitrary fields or properties of a given target.
    /// </summary>
    /// <typeparam name="TValue">The type of the field or property to operate upon.</typeparam>
    /// <typeparam name="T">The type of the target to operate upon.</typeparam>
    internal class TransformCustom<TValue, T> : Transform<TValue, T> where T : ITransformable
    {
        private delegate TValue ReadFunc(T transformable);
        private delegate void WriteFunc(T transformable, TValue value);

        private struct Accessor
        {
            public ReadFunc Read;
            public WriteFunc Write;
        }

        private static readonly ConcurrentDictionary<string, Accessor> accessors = new ConcurrentDictionary<string, Accessor>();

        private static ReadFunc createFieldGetter(FieldInfo field)
        {
            string methodName = $"{typeof(T).ReadableName()}.{field.Name}.get_{Guid.NewGuid():N}";
            DynamicMethod setterMethod = new DynamicMethod(methodName, typeof(TValue), new[] { typeof(T) }, true);
            ILGenerator gen = setterMethod.GetILGenerator();
            gen.Emit(OpCodes.Ldarg_0);
            gen.Emit(OpCodes.Ldfld, field);
            gen.Emit(OpCodes.Ret);
            return (ReadFunc)setterMethod.CreateDelegate(typeof(ReadFunc));
        }

        private static WriteFunc createFieldSetter(FieldInfo field)
        {
            string methodName = $"{typeof(T).ReadableName()}.{field.Name}.set_{Guid.NewGuid():N}";
            DynamicMethod setterMethod = new DynamicMethod(methodName, null, new[] { typeof(T), typeof(TValue) }, true);
            ILGenerator gen = setterMethod.GetILGenerator();
            gen.Emit(OpCodes.Ldarg_0);
            gen.Emit(OpCodes.Ldarg_1);
            gen.Emit(OpCodes.Stfld, field);
            gen.Emit(OpCodes.Ret);
            return (WriteFunc)setterMethod.CreateDelegate(typeof(WriteFunc));
        }

        private static Accessor findAccessor(Type type, string propertyOrFieldName)
        {
            PropertyInfo property = type.GetProperty(propertyOrFieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (property != null)
            {
                if (property.PropertyType != typeof(TValue))
                    throw new InvalidOperationException(
                        $"Cannot create {nameof(TransformCustom<TValue, T>)} for property {type.ReadableName()}.{propertyOrFieldName} " +
                        $"since its type should be {typeof(TValue).ReadableName()}, but is {property.PropertyType.ReadableName()}.");

                var getter = property.GetGetMethod(true);
                var setter = property.GetSetMethod(true);

                if (getter == null || setter == null)
                    throw new InvalidOperationException(
                        $"Cannot create {nameof(TransformCustom<TValue, T>)} for property {type.ReadableName()}.{propertyOrFieldName} " +
                        "since it needs to have both a getter and a setter.");

                if (getter.IsStatic || setter.IsStatic)
                    throw new NotSupportedException(
                        $"Cannot create {nameof(TransformCustom<TValue, T>)} for property {type.ReadableName()}.{propertyOrFieldName} because static fields are not supported.");

                return new Accessor
                {
                    Read = (ReadFunc)getter.CreateDelegate(typeof(ReadFunc)),
                    Write = (WriteFunc)setter.CreateDelegate(typeof(WriteFunc)),
                };
            }

            FieldInfo field = type.GetField(propertyOrFieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
            if (field != null)
            {
                if (field.FieldType != typeof(TValue))
                    throw new InvalidOperationException(
                        $"Cannot create {nameof(TransformCustom<TValue, T>)} for field {type.ReadableName()}.{propertyOrFieldName} " +
                        $"since its type should be {typeof(TValue).ReadableName()}, but is {field.FieldType.ReadableName()}.");

                if (field.IsStatic)
                    throw new NotSupportedException(
                        $"Cannot create {nameof(TransformCustom<TValue, T>)} for field {type.ReadableName()}.{propertyOrFieldName} because static fields are not supported.");

                return new Accessor
                {
                    Read = createFieldGetter(field),
                    Write = createFieldSetter(field),
                };
            }

            if (type.BaseType == null)
                throw new InvalidOperationException($"Cannot create {nameof(TransformCustom<TValue, T>)} for non-existent property or field {typeof(T).ReadableName()}.{propertyOrFieldName}.");

            // Private members aren't visible unless we check the base type explicitly, so let's try our luck.
            return findAccessor(type.BaseType, propertyOrFieldName);
        }

        private static Accessor getAccessor(string propertyOrFieldName) => accessors.GetOrAdd(propertyOrFieldName, _ => findAccessor(typeof(T), propertyOrFieldName));

        private readonly Accessor accessor;
        private readonly InterpolationFunc<TValue> interpolationFunc;

        /// <summary>
        /// Creates a new instance operating on a property or field of <see cref="T"/>. The property or field is
        /// denoted by its name, passed as <paramref name="propertyOrFieldName"/>.
        /// By default, an interpolation method "ValueAt" from <see cref="Interpolation"/> with suitable signature is
        /// picked for interpolating between <see cref="Transform{TValue}.StartValue"/> and
        /// <see cref="Transform{TValue}.EndValue"/> according to <see cref="Transform.StartTime"/>,
        /// <see cref="Transform.EndTime"/>, and a current time.
        /// Optionally, or when no suitable "ValueAt" from <see cref="Interpolation"/> exists, a custom function can be supplied
        /// via <paramref name="interpolationFunc"/>.
        /// </summary>
        /// <param name="propertyOrFieldName">The property or field name to be operated upon.</param>
        /// <param name="interpolationFunc">
        /// The function to be used for interpolating between <see cref="Transform{TValue}.StartValue"/> and
        /// <see cref="Transform{TValue}.EndValue"/> according to <see cref="Transform.StartTime"/>,
        /// <see cref="Transform.EndTime"/>, and a current time.
        /// If null, an interpolation method "ValueAt" from <see cref="Interpolation"/> with a suitable signature is picked.
        /// If none exists, then this parameter must not be null.
        /// </param>
        public TransformCustom(string propertyOrFieldName, InterpolationFunc<TValue> interpolationFunc = null)
        {
            TargetMember = propertyOrFieldName;

            accessor = getAccessor(propertyOrFieldName);
            Trace.Assert(accessor.Read != null && accessor.Write != null, $"Failed to populate {nameof(accessor)}.");

            this.interpolationFunc = interpolationFunc ?? Interpolation<TValue>.ValueAt;

            if (this.interpolationFunc == null)
                throw new InvalidOperationException(
                    $"Need to pass a custom {nameof(interpolationFunc)} since no default {nameof(Interpolation)}.{nameof(Interpolation.ValueAt)} exists.");
        }

        private TValue valueAt(double time)
        {
            if (time < StartTime) return StartValue;
            if (time >= EndTime) return EndValue;

            return interpolationFunc(time, StartValue, EndValue, StartTime, EndTime, Easing);
        }

        public override string TargetMember { get; }

        protected override void Apply(T d, double time) => accessor.Write(d, valueAt(time));

        protected override void ReadIntoStartValue(T d) => StartValue = accessor.Read(d);
    }
}
