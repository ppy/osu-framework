// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using JetBrains.Annotations;
using osu.Framework.Graphics;

namespace osu.Framework.Allocation
{
    /// <summary>
    /// An attribute that is attached to fields of a <see cref="Drawable"/> component to indicate
    /// that the value of the field should be retrieved from a dependency cache.
    /// </summary>
    [MeansImplicitUse]
    [AttributeUsage(AttributeTargets.Field)]
    public class DependencyAttribute : Attribute
    {
        private const BindingFlags activator_flags = BindingFlags.NonPublic | BindingFlags.Instance;

        /// <summary>
        /// Whether a null value can be accepted if the value does not exist in the cache.
        /// </summary>
        public bool CanBeNull;

        internal static InjectDependencyDelegate CreateActivator(Type type)
        {
            var fields = type.GetFields(activator_flags).Where(f => f.GetCustomAttribute<DependencyAttribute>() != null);

            var fieldActivators = new List<Action<object, DependencyContainer>>();

            foreach (var field in fields)
            {
                var attrib = field.GetCustomAttribute<DependencyAttribute>();
                var fieldGetter = getDependency(field.FieldType, type, attrib.CanBeNull);

                fieldActivators.Add((target, dc) => field.SetValue(target, fieldGetter(dc)));
            }

            return (target, dc) => fieldActivators.ForEach(a => a(target, dc));
        }

        private static Func<DependencyContainer, object> getDependency(Type type, Type requestingType, bool permitNulls) => dc =>
        {
            var val = dc.Get(type);
            if (val == null && !permitNulls)
                throw new DependencyNotRegisteredException(requestingType, type);
            return val;
        };
    }
}
