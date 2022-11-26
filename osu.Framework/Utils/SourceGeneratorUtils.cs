// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Extensions.TypeExtensions;
using osu.Framework.Graphics;

namespace osu.Framework.Utils
{
    /// <summary>
    /// A set of helper functions for source generators to use in order to simplify their work.
    /// </summary>
    public static class SourceGeneratorUtils
    {
        /// <summary>
        /// Caches an object to a <see cref="DependencyContainer"/>.
        /// </summary>
        /// <param name="container">The <see cref="DependencyContainer"/> which the object will be cached to.</param>
        /// <param name="callerType">The type of object calling this method.</param>
        /// <param name="obj">The object to be cached.</param>
        /// <param name="info">The parenting <see cref="CacheInfo"/> object.</param>
        /// <param name="asType">The type which <paramref name="obj"/> should be cached as.</param>
        /// <param name="cachedName">The name which <paramref name="obj"/> should be cached as.</param>
        /// <param name="propertyName">A fallback name for <paramref name="obj"/> to be cached as.</param>
        /// <exception cref="NullDependencyException">If <paramref name="obj"/> is <c>null</c>.</exception>
        public static void CacheDependency(DependencyContainer container, Type callerType, object? obj, CacheInfo info, Type? asType, string? cachedName, string? propertyName)
        {
            bool allowValueTypes = callerType.Assembly == typeof(Drawable).Assembly;

            if (obj == null)
            {
                if (allowValueTypes)
                    return;

                throw new NullDependencyException($"Attempted to cache a null value: {callerType.ReadableName()}.{propertyName}.");
            }

            CacheInfo cacheInfo = new CacheInfo(info.Name ?? cachedName);

            if (info.Parent != null)
            {
                // When a parent type exists, try to infer the property name if one is not provided.
                cacheInfo = new CacheInfo(cacheInfo.Name ?? propertyName, info.Parent);
            }

            container.CacheAs(asType ?? obj.GetType(), cacheInfo, obj, allowValueTypes);
        }

        /// <summary>
        /// Retrieves an object from an <see cref="IReadOnlyDependencyContainer"/>.
        /// </summary>
        /// <param name="container">The <see cref="IReadOnlyDependencyContainer"/> to retrieve the dependency from.</param>
        /// <param name="callerType">The type of object calling this method.</param>
        /// <param name="cachedName">The name of the object.</param>
        /// <param name="cachedParent">The parent of the object.</param>
        /// <param name="allowNulls">Whether the returned object is allowed to be <c>null</c>.</param>
        /// <param name="rebindBindables">If the object is a <see cref="IBindable"/>, whether it should be re-bound via <see cref="IBindable.GetBoundCopy"/>.</param>
        /// <typeparam name="T">The type of the object.</typeparam>
        /// <returns>The object.</returns>
        /// <exception cref="DependencyNotRegisteredException">If the dependency is not in <paramref name="container"/>.</exception>
        public static T GetDependency<T>(IReadOnlyDependencyContainer container, Type callerType, string? cachedName, Type? cachedParent, bool allowNulls, bool rebindBindables)
        {
            object val = container.Get(typeof(T), new CacheInfo(cachedName, cachedParent));

            if (val == null && !allowNulls)
                throw new DependencyNotRegisteredException(callerType, typeof(T));

            if (rebindBindables && val is IBindable bindableVal)
                return (T)bindableVal.GetBoundCopy();

            // `(int)(object)null` throws a NRE, so `default` is used instead.
            return val == null ? default! : (T)val;
        }
    }
}
