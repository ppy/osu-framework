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

        private readonly DependencyActivator baseActivator;

        private DependencyActivator(Type type)
        {
            injectionActivators.Add(DependencyAttribute.CreateActivator(type));
            injectionActivators.Add(BackgroundDependencyLoaderAttribute.CreateActivator(type));

            if (type.BaseType != typeof(object))
                baseActivator = getActivator(type.BaseType);

            activator_cache[type] = this;
        }

        public static void Activate(object obj, DependencyContainer dependencies)
            => getActivator(obj.GetType()).activate(obj, dependencies);

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

        private Action<object, DependencyContainer> buildFieldActivator()
        {
            var fields = type.GetFields(activator_flags).Where(f => f.GetCustomAttribute<DependencyAttribute>() != null);

            var fieldActivators = new List<Action<object, DependencyContainer>>();

            foreach (var field in fields)
            {
                var attrib = field.GetCustomAttribute<DependencyAttribute>();
                var fieldGetter = getDependency(field.FieldType, attrib.CanBeNull);

                fieldActivators.Add((target, dc) => field.SetValue(target, fieldGetter(dc)));
            }

            return (target, dc) => fieldActivators.ForEach(a => a(target, dc));
        }

        private Action<object, DependencyContainer> buildLoaderActivator()
        {
            var loaderMethods = type.GetMethods(activator_flags).Where(m => m.GetCustomAttribute<BackgroundDependencyLoaderAttribute>() != null).ToArray();

            switch (loaderMethods.Length)
            {
                case 0:
                    return (_,__) => { };
                case 1:
                    var method = loaderMethods[0];
                    var permitNulls = method.GetCustomAttribute<BackgroundDependencyLoaderAttribute>().PermitNulls;
                    var parameterGetters = method.GetParameters().Select(p => p.ParameterType).Select(t => getDependency(t, permitNulls));

                    return (target, dc) =>
                    {
                        var parameters = parameterGetters.Select(p => p(dc)).ToArray();
                        method.Invoke(target, parameters);
                    };
                default:
                    throw new MultipleDependencyLoaderMethodsException(type);
            }
        }

        private Func<DependencyContainer, object> getDependency(Type type, bool permitNulls) => dc =>
        {
            var val = dc.Get(type);
            if (val == null && !permitNulls)
                throw new DependencyNotRegisteredException(this.type, type);
            return val;
        };
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
}
