// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using osu.Framework.Extensions.TypeExtensions;

namespace osu.Framework.Allocation
{
    internal class DependencyActivator
    {
        private static readonly ConcurrentDictionary<Type, DependencyActivator> activator_cache = new ConcurrentDictionary<Type, DependencyActivator>();

        private readonly List<InjectDependencyDelegate> injectionActivators = new List<InjectDependencyDelegate>();
        private readonly List<CacheDependencyDelegate> buildCacheActivators = new List<CacheDependencyDelegate>();

        private readonly DependencyActivator baseActivator;

        private DependencyActivator(Type type)
        {
            injectionActivators.Add(ResolvedAttribute.CreateActivator(type));
            injectionActivators.Add(BackgroundDependencyLoaderAttribute.CreateActivator(type));
            buildCacheActivators.Add(CachedAttribute.CreateActivator(type));

            if (type.BaseType != typeof(object))
                baseActivator = getActivator(type.BaseType);

            activator_cache[type] = this;
        }

        public static void Activate(object obj, DependencyContainer dependencies)
            => getActivator(obj.GetType()).activate(obj, dependencies);

        public static IReadOnlyDependencyContainer BuildDependencies(object obj, IReadOnlyDependencyContainer dependencies)
            => getActivator(obj.GetType()).buildDependencies(obj, dependencies);

        private static DependencyActivator getActivator(Type type)
        {
            if (!activator_cache.TryGetValue(type, out var existing))
                return activator_cache[type] = new DependencyActivator(type);
            return existing;
        }

        private void activate(object obj, DependencyContainer dependencies)
        {
            baseActivator?.activate(obj, dependencies);
            injectionActivators.ForEach(a => a.Invoke(obj, dependencies));
        }

        private IReadOnlyDependencyContainer buildDependencies(object obj, IReadOnlyDependencyContainer dependencies)
        {
            dependencies = baseActivator?.buildDependencies(obj, dependencies) ?? dependencies;
            buildCacheActivators.ForEach(a => dependencies = a.Invoke(obj, dependencies));

            return dependencies;
        }
    }

    public class MultipleDependencyLoaderMethodsException : InvalidOperationException
    {
        public MultipleDependencyLoaderMethodsException(Type type)
            : base($"The type {type.ReadableName()} has more than one method marked with a {nameof(BackgroundDependencyLoaderAttribute)}."
                   + "Any given type may only have one such method.")
        {
        }
    }

    public class DependencyNotRegisteredException : InvalidOperationException
    {
        public DependencyNotRegisteredException(Type type, Type requestedType)
            : base($"The type {type.ReadableName()} has a dependency on {requestedType.ReadableName()}, but the dependency is not registered.")
        {
        }
    }

    internal delegate void InjectDependencyDelegate(object target, DependencyContainer dependencies);
    internal delegate IReadOnlyDependencyContainer CacheDependencyDelegate(object target, IReadOnlyDependencyContainer existingDependencies);
}
