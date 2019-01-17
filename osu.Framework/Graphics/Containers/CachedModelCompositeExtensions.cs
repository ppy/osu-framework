// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Diagnostics;
using System.Reflection;
using osu.Framework.Allocation;
using osu.Framework.Configuration;

namespace osu.Framework.Graphics.Containers
{
    internal static class CachedModelCompositeExtensions
    {
        private const BindingFlags activator_flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly;

        /// <summary>
        /// Helper method to create dependencies for a <see cref="ICachedModelComposite{TModel}"/>.
        /// </summary>
        public static IReadOnlyDependencyContainer CreateDependencies<TModel>(this ICachedModelComposite<TModel> composite, IReadOnlyDependencyContainer parent)
            where TModel : new()
            => DependencyActivator.MergeDependencies(composite.ShadowModel, parent, new CacheInfo(parent: typeof(TModel)));

        /// <summary>
        /// Helper method to perform updates to the shadow model of a <see cref="ICachedModelComposite{TModel}"/>.
        /// </summary>
        public static void UpdateShadowModel<TModel>(this ICachedModelComposite<TModel> composite, TModel lastModel, TModel newModel)
            where TModel : new()
        {
            // Due to static-constructor checks, we are guaranteed that all fields will be IBindable

            var type = typeof(TModel);
            while (type != typeof(object))
            {
                Debug.Assert(type != null);

                foreach (var field in type.GetFields(activator_flags))
                    rebind(field);

                type = type.BaseType;
            }

            void rebind(MemberInfo member)
            {
                object shadowValue = null;
                object lastModelValue = null;
                object newModelValue = null;

                switch (member)
                {
                    case PropertyInfo pi:
                        shadowValue = pi.GetValue(composite.ShadowModel);
                        lastModelValue = lastModel == null ? null : pi.GetValue(lastModel);
                        newModelValue = newModel == null ? null : pi.GetValue(newModel);
                        break;
                    case FieldInfo fi:
                        shadowValue = fi.GetValue(composite.ShadowModel);
                        lastModelValue = lastModel == null ? null : fi.GetValue(lastModel);
                        newModelValue = newModel == null ? null : fi.GetValue(newModel);
                        break;
                }

                if (shadowValue is IBindable shadowBindable)
                {
                    // Unbind from the last model
                    if (lastModelValue is IBindable lastModelBindable)
                        shadowBindable.UnbindFrom(lastModelBindable);

                    // Bind to the new model
                    if (newModelValue is IBindable newModelBindable)
                        shadowBindable.BindTo(newModelBindable);
                }
            }
        }

        public static void VerifyModelType(Type type)
        {
            while (type != null && type != typeof(object))
            {
                foreach (var field in type.GetFields(activator_flags))
                {
                    if (!typeof(IBindable).IsAssignableFrom(field.FieldType))
                        throw new InvalidOperationException($"The field \"{field.Name}\" does not subclass {nameof(IBindable)}. "
                                                            + $"All fields or auto-properties of a cached model container's model must subclass {nameof(IBindable)}");
                }

                type = type.BaseType;
            }
        }
    }
}
