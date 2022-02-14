// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

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
        /// </summary>
        /// <remarks>See: https://docs.microsoft.com/en-us/dotnet/csharp/programming-guide/nullable-types/how-to-identify-a-nullable-type</remarks>
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
