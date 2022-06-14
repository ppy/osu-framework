// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using osu.Framework.Utils;
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
    /// <typeparam name="TEasing">The type of easing.</typeparam>
    /// <typeparam name="T">The type of the target to operate upon.</typeparam>
    internal class TransformCustom<TValue, TEasing, T> : Transform<TValue, TEasing, T>
        where T : class, ITransformable
        where TEasing : IEasingFunction
    {
        public override string TargetGrouping => targetGrouping ?? TargetMember;

        private readonly string targetGrouping;

        private delegate TValue ReadFunc(T transformable);

        private delegate void WriteFunc(T transformable, TValue value);

        private class Accessor
        {
            public ReadFunc Read;
            public WriteFunc Write;
        }

        private static readonly ConcurrentDictionary<string, Accessor> accessors = new ConcurrentDictionary<string, Accessor>();

        private static ReadFunc createFieldGetter(FieldInfo field)
        {
            if (!RuntimeInfo.SupportsJIT) return transformable => (TValue)field.GetValue(transformable);

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
            if (!RuntimeInfo.SupportsJIT) return (transformable, value) => field.SetValue(transformable, value);

            string methodName = $"{typeof(T).ReadableName()}.{field.Name}.set_{Guid.NewGuid():N}";
            DynamicMethod setterMethod = new DynamicMethod(methodName, null, new[] { typeof(T), typeof(TValue) }, true);
            ILGenerator gen = setterMethod.GetILGenerator();
            gen.Emit(OpCodes.Ldarg_0);
            gen.Emit(OpCodes.Ldarg_1);
            gen.Emit(OpCodes.Stfld, field);
            gen.Emit(OpCodes.Ret);
            return (WriteFunc)setterMethod.CreateDelegate(typeof(WriteFunc));
        }

        private static ReadFunc createPropertyGetter(MethodInfo getter)
        {
            if (!RuntimeInfo.SupportsJIT) return transformable => (TValue)getter.Invoke(transformable, Array.Empty<object>());

            return (ReadFunc)getter.CreateDelegate(typeof(ReadFunc));
        }

        private static WriteFunc createPropertySetter(MethodInfo setter)
        {
            if (!RuntimeInfo.SupportsJIT) return (transformable, value) => setter.Invoke(transformable, new object[] { value });

            return (WriteFunc)setter.CreateDelegate(typeof(WriteFunc));
        }

        private static Accessor findAccessor(Type type, string propertyOrFieldName)
        {
            PropertyInfo property = type.GetProperty(propertyOrFieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            if (property != null)
            {
                if (property.PropertyType != typeof(TValue))
                {
                    throw new InvalidOperationException(
                        $"Cannot create {nameof(TransformCustom<TValue, T>)} for property {type.ReadableName()}.{propertyOrFieldName} " +
                        $"since its type should be {typeof(TValue).ReadableName()}, but is {property.PropertyType.ReadableName()}.");
                }

                var getter = property.GetGetMethod(true);
                var setter = property.GetSetMethod(true);

                if (getter == null || setter == null)
                {
                    throw new InvalidOperationException(
                        $"Cannot create {nameof(TransformCustom<TValue, T>)} for property {type.ReadableName()}.{propertyOrFieldName} " +
                        "since it needs to have both a getter and a setter.");
                }

                if (getter.IsStatic || setter.IsStatic)
                {
                    throw new NotSupportedException(
                        $"Cannot create {nameof(TransformCustom<TValue, T>)} for property {type.ReadableName()}.{propertyOrFieldName} because static fields are not supported.");
                }

                return new Accessor
                {
                    Read = createPropertyGetter(getter),
                    Write = createPropertySetter(setter),
                };
            }

            FieldInfo field = type.GetField(propertyOrFieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);

            if (field != null)
            {
                if (field.FieldType != typeof(TValue))
                {
                    throw new InvalidOperationException(
                        $"Cannot create {nameof(TransformCustom<TValue, T>)} for field {type.ReadableName()}.{propertyOrFieldName} " +
                        $"since its type should be {typeof(TValue).ReadableName()}, but is {field.FieldType.ReadableName()}.");
                }

                if (field.IsStatic)
                {
                    throw new NotSupportedException(
                        $"Cannot create {nameof(TransformCustom<TValue, T>)} for field {type.ReadableName()}.{propertyOrFieldName} because static fields are not supported.");
                }

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

        private static Accessor getAccessor(string propertyOrFieldName) => accessors.GetOrAdd(propertyOrFieldName, key => findAccessor(typeof(T), key));

        private readonly Accessor accessor;

        /// <summary>
        /// Creates a new instance operating on a property or field of <typeparamref name="T"/>. The property or field is
        /// denoted by its name, passed as <paramref name="propertyOrFieldName"/>.
        /// By default, an interpolation method "ValueAt" from <see cref="Interpolation"/> with suitable signature is
        /// picked for interpolating between <see cref="Transform{TValue}.StartValue"/> and
        /// <see cref="Transform{TValue}.EndValue"/> according to <see cref="Transform.StartTime"/>,
        /// <see cref="Transform.EndTime"/>, and a current time.
        /// </summary>
        /// <param name="propertyOrFieldName">The property or field name to be operated upon.</param>
        /// <param name="grouping">An optional grouping, for a case where the target property can potentially conflict with others.</param>
        public TransformCustom(string propertyOrFieldName, string grouping = null)
        {
            TargetMember = propertyOrFieldName;
            targetGrouping = grouping;

            accessor = getAccessor(propertyOrFieldName);
            Trace.Assert(accessor.Read != null && accessor.Write != null, $"Failed to populate {nameof(accessor)}.");
        }

        private TValue valueAt(double time)
        {
            if (time < StartTime) return StartValue;
            if (time >= EndTime) return EndValue;

            return Interpolation.ValueAt(time, StartValue, EndValue, StartTime, EndTime, Easing);
        }

        public override string TargetMember { get; }

        protected override void Apply(T d, double time) => accessor.Write(d, valueAt(time));

        protected override void ReadIntoStartValue(T d) => StartValue = accessor.Read(d);
    }

    internal class TransformCustom<TValue, T> : TransformCustom<TValue, DefaultEasingFunction, T>
        where T : class, ITransformable
    {
        public TransformCustom(string propertyOrFieldName)
            : base(propertyOrFieldName)
        {
        }
    }
}
