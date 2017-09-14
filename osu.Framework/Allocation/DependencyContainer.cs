﻿// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Extensions.TypeExtensions;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace osu.Framework.Allocation
{
    /// <summary>
    /// Hierarchically caches dependencies and can inject those automatically into types registered for dependency injection.
    /// </summary>
    public class DependencyContainer : IReadOnlyDependencyContainer
    {
        private delegate object ObjectActivator(DependencyContainer dc, object instance);

        private readonly ConcurrentDictionary<Type, ObjectActivator> activators = new ConcurrentDictionary<Type, ObjectActivator>();
        private readonly ConcurrentDictionary<Type, object> cache = new ConcurrentDictionary<Type, object>();
        private readonly HashSet<Type> cacheable = new HashSet<Type>();

        private readonly IReadOnlyDependencyContainer parentContainer;

        /// <summary>
        /// Create a new DependencyContainer instance.
        /// </summary>
        /// <param name="parent">An optional parent container which we should use as a fallback for cache lookups.</param>
        public DependencyContainer(IReadOnlyDependencyContainer parent = null)
        {
            parentContainer = parent;
        }

        private MethodInfo getLoaderMethod(Type type)
        {
            var loaderMethods = type.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance).Where(
                mi => mi.GetCustomAttribute<BackgroundDependencyLoader>() != null).ToArray();
            if (loaderMethods.Length == 0)
                return null;
            else if (loaderMethods.Length == 1)
                return loaderMethods[0];
            else
                throw new InvalidOperationException($"The type {type.ReadableName()} has more than one method marked with the {nameof(BackgroundDependencyLoader)}-Attribute. Any given type can only have one such method.");
        }

        private void register(Type type, bool lazy)
        {
            if (activators.ContainsKey(type))
                throw new InvalidOperationException($@"Type {type.FullName} can not be registered twice");

            var initialize = getLoaderMethod(type);
            var constructor = type.GetConstructor(new Type[] { });

            var initializerMethods = new List<MethodInfo>();

            for (Type parent = type.BaseType; parent != typeof(object); parent = parent?.BaseType)
            {
                var init = getLoaderMethod(parent);
                if (init != null)
                    initializerMethods.Insert(0, init);
            }
            if (initialize != null)
                initializerMethods.Add(initialize);

            var initializers = initializerMethods.Select(initializer =>
            {
                var permitNull = initializer.GetCustomAttribute<BackgroundDependencyLoader>().PermitNulls;
                var parameters = initializer.GetParameters().Select(p => p.ParameterType)
                                            .Select(t => new Func<object>(() =>
                                            {
                                                var val = Get(t);
                                                if (val == null && !permitNull)
                                                {
                                                    throw new InvalidOperationException(
                                                        $@"Type {t.FullName} is not registered, and is a dependency of {type.FullName}");
                                                }
                                                return val;
                                            })).ToList();
                // Test that we already have all the dependencies registered
                if (!lazy)
                    parameters.ForEach(p => p());
                return new Action<object>(instance =>
                {
                    var p = parameters.Select(pa => pa()).ToArray();
                    initializer.Invoke(instance, p);
                });
            }).ToList();

            activators[type] = (container, instance) =>
            {
                if (instance == null)
                {
                    if (constructor == null)
                        throw new InvalidOperationException($@"Type {type.FullName} must have a parameterless constructor to initialize one from scratch.");
                    instance = Activator.CreateInstance(type);
                }
                initializers.ForEach(init => init(instance));
                return instance;
            };
        }

        /// <summary>
        /// Registers a type and configures a default allocator for it that injects its
        /// dependencies.
        /// </summary>
        public void Register<T>(bool lazy = false) where T : class => register(typeof(T), lazy);

        /// <summary>
        /// Registers a type that allocates with a custom allocator.
        /// </summary>
        public void Register<T>(Func<DependencyContainer, T> activator) where T : class
        {
            var type = typeof(T);
            if (activators.ContainsKey(type))
                throw new InvalidOperationException($@"Type {typeof(T).FullName} is already registered");
            activators[type] = (d, i) => i ?? activator(d);
        }

        /// <summary>
        /// Caches an instance of a type. This instance will be returned each time you <see cref="Get(Type)"/>.
        /// </summary>
        public T Cache<T>(T instance = null, bool overwrite = false) where T : class
        {
            if (!overwrite && cache.ContainsKey(typeof(T)))
                throw new InvalidOperationException($@"Type {typeof(T).FullName} is already cached");
            if (instance == null)
                instance = this.Get<T>();
            cacheable.Add(typeof(T));
            cache[typeof(T)] = instance;
            return instance;
        }

        /// <summary>
        /// Retrieves a cached dependency of <paramref name="type"/> if it exists. If not, then the parent
        /// <see cref="IReadOnlyDependencyContainer"/> is recursively queried. If no parent contains
        /// <paramref name="type"/>, then null is returned.
        /// </summary>
        /// <param name="type">The dependency type to query for.</param>
        /// <returns>The requested dependency, or null if not found.</returns>
        public object Get(Type type)
        {
            object ret;
            if (cache.TryGetValue(type, out ret))
                return ret;

            return parentContainer?.Get(type);

            //we don't ever want to instantiate for now, as this breaks expectations when using permitNull.
            //need to revisit this when/if it is required.
            //if (!activators.ContainsKey(type))
            //    return null; // Or an exception?
            //object instance = activators[type](this, null);
            //if (cacheable.Contains(type))
            //    cache[type] = instance;
            //return instance;
        }

        /// <summary>
        /// Injects dependencies into the given instance.
        /// </summary>
        /// <typeparam name="T">The type of the instance to inject dependencies into.</typeparam>
        /// <param name="instance">The instance to inject dependencies into.</param>
        /// <param name="autoRegister">True if the instance should be automatically registered as injectable if it isn't already.</param>
        /// <param name="lazy">True if the dependencies should be initialized lazily.</param>
        public void Inject<T>(T instance, bool autoRegister = true, bool lazy = false) where T : class
        {
            var type = instance.GetType();

            // TODO: consider using parentContainer for activator lookups as a potential performance improvement.

            lock (activators)
                if (autoRegister && !activators.ContainsKey(type))
                    register(type, lazy);

            ObjectActivator activator;

            if (!activators.TryGetValue(type, out activator))
                throw new InvalidOperationException("DI Initialisation failed badly.");

            activator(this, instance);
        }
    }
}
