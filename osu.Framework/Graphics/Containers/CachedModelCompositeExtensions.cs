// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using osu.Framework.Allocation;
using osu.Framework.Configuration;

namespace osu.Framework.Graphics.Containers
{
    internal static class CachedModelCompositeExtensions
    {
        private const BindingFlags activator_flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly;

        public static IReadOnlyDependencyContainer CreateDependencies<TModel>(this ICachedModelComposite<TModel> composite, IReadOnlyDependencyContainer parent)
            where TModel : new()
            => new DelegatingDependencyContainer
            {
                Target = createDependencies(composite, parent),
                TargetParent = parent
            };

        private static IReadOnlyDependencyContainer createDependencies<TModel>(ICachedModelComposite<TModel> composite, IReadOnlyDependencyContainer parent)
            where TModel : new()
            => DependencyActivator.MergeDependencies(composite.ShadowModel, parent, new CacheInfo(parent: typeof(TModel)));

        public static void UpdateShadowModel<TModel>(this ICachedModelComposite<TModel> composite, TModel lastModel, TModel newModel)
            where TModel : new()
        {
            // The following code performs the following tasks:
            // 1. Copy all non-bindable fields from the new model into the shadow model, so subclasses can reference ShadowModel.{Field}
            // 2. Re-bind all IBindable fields and properties in the shadow model to point to the new model, so that children's bindables are updated
            // 3. Reconstruct the dependencies so children can retrieve updated dependencies through dependencies.Get()

            var type = typeof(TModel);
            while (type != typeof(object))
            {
                Debug.Assert(type != null);

                foreach (var field in type.GetFields(activator_flags))
                {
                    if (newModel != null)
                    {
                        // Copy non-bindable field to the shadow model
                        var newValue = field.GetValue(newModel);
                        if (!(newValue is IBindable))
                            field.SetValue(composite.ShadowModel, newValue);
                    }

                    if (field.GetCustomAttributes<CachedAttribute>().Any())
                        rebind(field);
                }

                foreach (var property in type.GetProperties(activator_flags))
                {
                    if (property.GetCustomAttributes<CachedAttribute>().Any())
                        rebind(property);
                }

                type = type.BaseType;
            }

            // Re-cache the shadow model to update non-bindable dependencies
            var dependencies = (composite as CompositeDrawable)?.Dependencies;
            if (dependencies is DelegatingDependencyContainer delegatingDependencies)
                delegatingDependencies.Target = createDependencies(composite, delegatingDependencies.TargetParent);

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

        private class DelegatingDependencyContainer : IReadOnlyDependencyContainer
        {
            /// <summary>
            /// The <see cref="IReadOnlyDependencyContainer"/> target for delegation.
            /// </summary>
            public IReadOnlyDependencyContainer Target;

            /// <summary>
            /// The parent of <see cref="Target"/>.
            /// </summary>
            public IReadOnlyDependencyContainer TargetParent;

            public object Get(Type type)
                => Target.Get(type);

            public object Get(Type type, CacheInfo info)
                => Target.Get(type, info);

            public void Inject<T>(T instance) where T : class
                => Target.Inject(instance);
        }
    }
}
