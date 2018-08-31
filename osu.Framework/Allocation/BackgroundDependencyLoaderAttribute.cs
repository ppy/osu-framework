// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
using JetBrains.Annotations;
using osu.Framework.Extensions.TypeExtensions;

namespace osu.Framework.Allocation
{
    /// <summary>
    /// Marks a method as the loader-Method of a <see cref="osu.Framework.Graphics.Drawable"/>, allowing for automatic injection of dependencies via the parameters of the method.
    /// </summary>
    [MeansImplicitUse]
    [AttributeUsage(AttributeTargets.Method)]
    public class BackgroundDependencyLoaderAttribute : Attribute
    {
        private const BindingFlags activator_flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly;

        private bool permitNulls { get; }

        /// <summary>
        /// Marks this method as the initializer for a class in the context of dependency injection.
        /// </summary>
        public BackgroundDependencyLoaderAttribute()
        {
        }

        /// <summary>
        /// Marks this method as the initializer for a class in the context of dependency injection.
        /// </summary>
        /// <param name="permitNulls">If true, the initializer may be passed null for the dependencies we can't fulfill.</param>
        public BackgroundDependencyLoaderAttribute(bool permitNulls)
        {
            this.permitNulls = permitNulls;
        }

        internal static InjectDependencyDelegate CreateActivator(Type type)
        {
            var loaderMethods = type.GetMethods(activator_flags).Where(m => m.GetCustomAttribute<BackgroundDependencyLoaderAttribute>() != null).ToArray();

            switch (loaderMethods.Length)
            {
                case 0:
                    return (_, __) => { };
                case 1:
                    var method = loaderMethods[0];

                    var modifier = method.GetAccessModifier();
                    if (modifier != AccessModifier.Private)
                        throw new AccessModifierNotAllowedForLoaderMethodException(modifier, method);

                    var permitNulls = method.GetCustomAttribute<BackgroundDependencyLoaderAttribute>().permitNulls;
                    var parameterGetters = method.GetParameters().Select(p => p.ParameterType).Select(t => getDependency(t, type, permitNulls || t.IsNullable()));

                    return (target, dc) =>
                    {
                        try
                        {
                            method.Invoke(target, parameterGetters.Select(p => p(dc)).ToArray());
                        }
                        catch (TargetInvocationException exc) // During non-await invocations
                        {
                            switch (exc.InnerException)
                            {
                                case OperationCanceledException _:
                                    // This activator is cancelled - propagate the cancellation as-is (it will be handled silently)
                                    throw exc.InnerException;
                                case DependencyInjectionException die:
                                    // A nested activator has failed (multiple Invoke() calls) - propagate the original error
                                    throw die;
                            }

                            // This activator has failed (single reflection call) - preserve the original stacktrace while notifying of the error
                            throw new DependencyInjectionException { DispatchInfo = ExceptionDispatchInfo.Capture(exc.InnerException) };
                        }
                    };
                default:
                    throw new MultipleDependencyLoaderMethodsException(type);
            }
        }

        private static Func<IReadOnlyDependencyContainer, object> getDependency(Type type, Type requestingType, bool permitNulls) => dc =>
        {
            var val = dc.Get(type);
            if (val == null && !permitNulls)
                throw new DependencyNotRegisteredException(requestingType, type);
            return val;
        };
    }
}
