// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Diagnostics;
using System.Reflection;
using osu.Framework.Configuration;

namespace osu.Framework.Allocation
{
    public class CachedModelDependencyContainer<TModel> : IReadOnlyDependencyContainer
        where TModel : class, new()
    {
        private const BindingFlags activator_flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly;

        public readonly Bindable<TModel> Model = new Bindable<TModel>();

        private readonly TModel shadowModel = new TModel();

        private readonly IReadOnlyDependencyContainer parent;
        private readonly IReadOnlyDependencyContainer shadowDependencies;

        private TModel currentModel;

        public CachedModelDependencyContainer(IReadOnlyDependencyContainer parent)
        {
            this.parent = parent;

            shadowDependencies = DependencyActivator.MergeDependencies(shadowModel, null, new CacheInfo(parent: typeof(TModel)));

            Model.BindValueChanged(newModel =>
            {
                // When setting a null model, we actually want to reset the shadow model to a default state
                // rather than leaving the current state on-going
                newModel = newModel ?? new TModel();

                updateShadowModel(shadowModel, currentModel, newModel);

                currentModel = newModel;
            });
        }

        public object Get(Type type) => Get(type, default);

        public object Get(Type type, CacheInfo info)
        {
            if (info.Parent == null)
                return type == typeof(TModel) ? createChildShadowModel() : parent?.Get(type, info);
            if (info.Parent == typeof(TModel))
                return shadowDependencies.Get(type, info) ?? parent?.Get(type, info);
            return parent?.Get(type, info);
        }

        public void Inject<T>(T instance) where T : class => DependencyActivator.Activate(instance, this);

        /// <summary>
        /// Creates a new shadow model bound to <see cref="shadowModel"/>.
        /// </summary>
        private TModel createChildShadowModel()
        {
            var result = new TModel();
            updateShadowModel(result, default, shadowModel);
            return result;
        }

        /// <summary>
        /// Updates a shadow model by unbinding from a previous model and binding to a new model.
        /// </summary>
        /// <param name="shadowModel">The shadow model to update.</param>
        /// <param name="lastModel">The model to unbind from.</param>
        /// <param name="newModel">The model to bind to.</param>
        private void updateShadowModel(TModel shadowModel, TModel lastModel, TModel newModel)
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
                        shadowValue = pi.GetValue(shadowModel);
                        lastModelValue = lastModel == null ? null : pi.GetValue(lastModel);
                        newModelValue = newModel == null ? null : pi.GetValue(newModel);
                        break;
                    case FieldInfo fi:
                        shadowValue = fi.GetValue(shadowModel);
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

        static CachedModelDependencyContainer()
        {
            var type = typeof(TModel);

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
