// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace osu.Framework.Extensions.TypeExtensions
{
    public static class TypeExtensions
    {
        private static string readableName(Type t, HashSet<Type> usedTypes)
        {
            usedTypes.Add(t);

            string result = t.Name;

            // Trim away amount of type arguments
            int amountTypeArgumentsPos = result.IndexOf('`');
            if (amountTypeArgumentsPos >= 0)
                result = result.Substring(0, amountTypeArgumentsPos);

            // We were declared inside another class. Preprend the name of that class.
            if (t.DeclaringType != null && !usedTypes.Contains(t.DeclaringType))
                result = $"{readableName(t.DeclaringType, usedTypes)}+{result}";

            if (t.IsGenericType)
            {
                var typeArgs = t.GetGenericArguments().Except(usedTypes);
                if (typeArgs.Any())
                    result += $"<{string.Join(',', typeArgs.Select(genType => readableName(genType, usedTypes)))}>";
            }

            return result;
        }

        /// <summary>
        /// Return every base type until (and excluding) <see cref="object"/>
        /// </summary>
        /// <param name="t"></param>
        public static IEnumerable<Type> EnumerateBaseTypes(this Type t)
        {
            while (t != null && t != typeof(object))
            {
                yield return t;

                t = t.BaseType;
            }
        }

        public static string ReadableName(this Type t) => readableName(t, new HashSet<Type>());

        public static AccessModifier GetAccessModifier(this FieldInfo field)
        {
            AccessModifier ret = AccessModifier.None;

            if (field.IsPublic)
                ret |= AccessModifier.Public;
            if (field.IsAssembly)
                ret |= AccessModifier.Internal;
            if (field.IsFamily)
                ret |= AccessModifier.Protected;
            if (field.IsPrivate)
                ret |= AccessModifier.Private;
            if (field.IsFamilyOrAssembly)
                ret |= AccessModifier.Protected | AccessModifier.Internal;

            return ret;
        }

        public static AccessModifier GetAccessModifier(this MethodInfo method)
        {
            AccessModifier ret = AccessModifier.None;

            if (method.IsPublic)
                ret |= AccessModifier.Public;
            if (method.IsAssembly)
                ret |= AccessModifier.Internal;
            if (method.IsFamily)
                ret |= AccessModifier.Protected;
            if (method.IsPrivate)
                ret |= AccessModifier.Private;
            if (method.IsFamilyOrAssembly)
                ret |= AccessModifier.Protected | AccessModifier.Internal;

            return ret;
        }

        private static readonly ConcurrentDictionary<Type, Type> underlying_type_cache = new ConcurrentDictionary<Type, Type>();

        /// <summary>
        /// Determines whether the specified type is a <see cref="Nullable{T}"/> type.
        /// <para>
        /// https://docs.microsoft.com/en-us/dotnet/csharp/programming-guide/nullable-types/how-to-identify-a-nullable-type
        /// </para>
        /// </summary>
        /// <remarks>
        /// When nullable reference types are enabled, prefer to use one of:
        /// <see cref="IsNullable(EventInfo)"/>, <see cref="IsNullable(PropertyInfo)"/>, <see cref="IsNullable(FieldInfo)"/>, or <see cref="IsNullable(ParameterInfo)"/>,
        /// in order to properly handle events/properties/fields/parameters.
        /// </remarks>
        public static bool IsNullable(this Type type) => type.GetUnderlyingNullableType() != null;

        /// <summary>
        /// Gets the underlying type of a <see cref="Nullable{T}"/>.
        /// </summary>
        /// <remarks>This method is cached.</remarks>
        /// <param name="type">The potentially nullable type.</param>
        /// <returns>The underlying type, or null if one does not exist.</returns>
        public static Type GetUnderlyingNullableType(this Type type)
        {
            if (!type.IsGenericType)
                return null;

            // ReSharper disable once ConvertClosureToMethodGroup (see: https://github.com/dotnet/runtime/issues/33747)
            return underlying_type_cache.GetOrAdd(type, t => Nullable.GetUnderlyingType(t));
        }

        /// <summary>
        /// Determines whether the type of an event is nullable.
        /// </summary>
        /// <remarks>
        /// Will be <c>false</c> for reference types if nullable reference types are not enabled.
        /// </remarks>
        /// <param name="eventInfo">The event.</param>
        /// <returns>Whether the event type is nullable.</returns>
        public static bool IsNullable(this EventInfo eventInfo)
        {
            if (IsNullable(eventInfo.EventHandlerType))
                return true;

#if NET6_0_OR_GREATER
            return isNullableInfo(new NullabilityInfoContext().Create(eventInfo));
#else
            return false;
#endif
        }

        /// <summary>
        /// Determines whether the type of a parameter is nullable.
        /// </summary>
        /// <remarks>
        /// Will be <c>false</c> for reference types if nullable reference types are not enabled.
        /// </remarks>
        /// <param name="parameterInfo">The parameter.</param>
        /// <returns>Whether the parameter type is nullable.</returns>
        public static bool IsNullable(this ParameterInfo parameterInfo)
        {
            if (IsNullable(parameterInfo.ParameterType))
                return true;

#if NET6_0_OR_GREATER
            return isNullableInfo(new NullabilityInfoContext().Create(parameterInfo));
#else
            return false;
#endif
        }

        /// <summary>
        /// Determines whether the type of a field is nullable.
        /// </summary>
        /// <remarks>
        /// Will be <c>false</c> for reference types if nullable reference types are not enabled.
        /// </remarks>
        /// <param name="fieldInfo">The field.</param>
        /// <returns>Whether the field type is nullable.</returns>
        public static bool IsNullable(this FieldInfo fieldInfo)
        {
            if (IsNullable(fieldInfo.FieldType))
                return true;

#if NET6_0_OR_GREATER
            return isNullableInfo(new NullabilityInfoContext().Create(fieldInfo));
#else
            return false;
#endif
        }

        /// <summary>
        /// Determines whether the type of a property is nullable.
        /// </summary>
        /// <remarks>
        /// Will be <c>false</c> for reference types if nullable reference types are not enabled.
        /// </remarks>
        /// <param name="propertyInfo">The property.</param>
        /// <returns>Whether the property type is nullable.</returns>
        public static bool IsNullable(this PropertyInfo propertyInfo)
        {
            if (IsNullable(propertyInfo.PropertyType))
                return true;

#if NET6_0_OR_GREATER
            return isNullableInfo(new NullabilityInfoContext().Create(propertyInfo));
#else
            return false;
#endif
        }

#if NET6_0_OR_GREATER
        private static bool isNullableInfo(NullabilityInfo info) => info.WriteState == NullabilityState.Nullable || info.ReadState == NullabilityState.Nullable;
#endif
    }

    [Flags]
    public enum AccessModifier
    {
        None = 0,
        Public = 1,
        Internal = 1 << 1,
        Protected = 1 << 2,
        Private = 1 << 3
    }
}
