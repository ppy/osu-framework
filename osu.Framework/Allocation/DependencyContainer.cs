// Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace osu.Framework.Allocation
{
    public class DependencyContainer
    {
        // Activators are object Activate(DependencyContainer container, object instance),
        // where the latter may be null.
        private Dictionary<Type, Func<DependencyContainer, object, object>> activators =
            new Dictionary<Type, Func<DependencyContainer, object, object>>();
        private Dictionary<Type, object> cache = new Dictionary<Type, object>();
        private HashSet<Type> cacheable = new HashSet<Type>();

        public DependencyContainer()
        {
            Cache(this);
        }

        private MethodInfo GetLoaderMethod(Type type)
        {
            return type.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance).SingleOrDefault(
                mi => mi.CustomAttributes.Any(attr => attr.AttributeType == typeof(BackgroundDependencyLoader)));
        }

        private void Register(Type type, bool lazy)
        {
            Debug.Assert(!activators.ContainsKey(type), $@"Type {type.FullName} should not be registered twice");

            var initialize = GetLoaderMethod(type);
            var constructor = type.GetConstructors().SingleOrDefault(c => c.GetParameters().Length == 0);

            var initializerMethods = new List<MethodInfo>();
            Type parent = type.BaseType;
            while (parent != typeof(object))
            {
                var init = GetLoaderMethod(parent);
                if (init != null)
                    initializerMethods.Insert(0, init);
                parent = parent.BaseType;
            }
            if (initialize != null)
                initializerMethods.Add(initialize);

            var initializers = initializerMethods.Select(initializer =>
            {
                var permitNull = initializer.GetCustomAttribute<BackgroundDependencyLoader>().PermitNulls;
                var parameters = initializer.GetParameters().Select(p => p.ParameterType)
                    .Select(t => (Func<object>)(() =>
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
        public void Register<T>(bool lazy = false) where T : class => Register(typeof(T), lazy);

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
        /// Caches an instance of a type. This instance will be returned each time you Get<T>.
        /// </summary>
        public T Cache<T>(T instance = null, bool overwrite = false, bool lazy = false) where T : class
        {
            Debug.Assert(overwrite || !cache.ContainsKey(typeof(T)), @"We have already cached one of these");
            if (instance == null)
                instance = Get<T>(false, false);
            cacheable.Add(typeof(T));
            cache[typeof(T)] = instance;
            return instance;
        }

        private object Get(Type type)
        {
            if (cache.ContainsKey(type))
                return cache[type];
            if (!activators.ContainsKey(type))
                return null; // Or an exception?
            object instance = activators[type](this, null);
            if (cacheable.Contains(type))
                cache[type] = instance;
            return instance;
        }

        /// <summary>
        /// Gets an instance of the specified type.
        /// </summary>
        public T Get<T>(bool autoRegister = true, bool lazy = false) where T : class
        {
            T instance = (T)Get(typeof(T));
            if (autoRegister && instance == null)
            {
                Register<T>(lazy);
                instance = (T)Get(typeof(T));
            }
            return instance;
        }

        public T Initialize<T>(T instance, bool autoRegister = true, bool lazy = false) where T : class
        {
            var type = instance.GetType();
            if (autoRegister && !activators.ContainsKey(type))
                Register(type, lazy);
            return (T)activators[type](this, instance);
        }
    }
}