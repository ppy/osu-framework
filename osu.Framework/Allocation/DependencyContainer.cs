using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace osu.Framework.Allocation
{
    public class DependencyContainer
    {
        private Dictionary<Type, Func<DependencyContainer, object>> activators =
            new Dictionary<Type, Func<DependencyContainer,object>>();
        private Dictionary<Type, object> cache = new Dictionary<Type, object>();
        private HashSet<Type> cacheable = new HashSet<Type>();

        /// <summary>
        /// Registers a type and configures a default allocator for it that injects its
        /// dependencies.
        /// </summary>
        public void Register<T>() where T : class
        {
            var type = typeof(T);
            if (activators.ContainsKey(type) || this.cache.ContainsKey(type))
                throw new InvalidOperationException($@"Type {typeof(T).Name} is already registered");
            var parameters = type.GetConstructors()[0].GetParameters().Select(p => p.ParameterType)
                .Select(t => (Func<object>)(() =>
                    {
                        var val = Get(t);
                        if (val == null)
                        {
                            throw new InvalidOperationException(
                                $@"Type {t.Name} is not registered, and is a dependency of {type.Name}");
                        }
                        return val;
                    })).ToList();
            // Test that we already have all the dependencies registered
            parameters.ForEach(p => p());
            activators[type] = d =>
            {
                return (T)Activator.CreateInstance(type, parameters.Select(p => p()));
            };
        }
        
        /// <summary>
        /// Registers a type that we can allocate with the default activator.
        /// </summary>
        public void Register<T>(Func<DependencyContainer, T> activator) where T : class
        {
            var type = typeof(T);
            if (activators.ContainsKey(type))
                throw new InvalidOperationException($@"Type {typeof(T).Name} is already registered");
            activators[type] = d => activator(d);
        }
        public void Cache<T>(T instance = null) where T : class
        {
            if (instance == null)
                instance = Get<T>();
            cacheable.Add(typeof(T));
            cache[typeof(T)] = instance;
        }
        private object Get(Type type)
        {
            if (cache.ContainsKey(type))
                return cache[type];
            if (!activators.ContainsKey(type))
                return null; // Or an exception?
            object instance = activators[type](this);
            if (cacheable.Contains(type))
                cache[type] = instance;
            return instance;
        }

        /// <summary>
        /// Gets an instance of the specified type.
        /// </summary>
        public T Get<T>() where T : class
        {
            return (T)Get(typeof(T));
        }
    }
}