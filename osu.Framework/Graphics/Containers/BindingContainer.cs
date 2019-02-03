// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using osu.Framework.Allocation;
using osu.Framework.Configuration;

namespace osu.Framework.Graphics.Containers
{
    /// <summary>
    /// A <see cref="Container"/> which caches all cacheable members of a model for usage by its children.
    /// </summary>
    /// <typeparam name="TModel">The type of model.</typeparam>
    public class BindingContainer<TModel> : Container
        where TModel : new()
    {
        private readonly TModel shadowModel;

        public BindingContainer()
        {
            shadowModel = new TModel();
        }

        protected override IReadOnlyDependencyContainer CreateChildDependencies(IReadOnlyDependencyContainer parent)
            => DependencyActivator.MergeDependencies(shadowModel, base.CreateChildDependencies(parent), new CacheInfo(parent: typeof(TModel)));

        private TModel model;

        /// <summary>
        /// The model to cache.
        /// Children of this <see cref="BindingContainer{TModel}"/> can resolve the cached members by using <see cref="ResolvedAttribute.Parent"/> = typeof(<see cref="TModel"/>).
        /// </summary>
        public TModel Model
        {
            get => model;
            set
            {
                if (EqualityComparer<TModel>.Default.Equals(model, value))
                    return;

                var lastModel = model;

                model = value;

                updateShadowBindings(lastModel, model);
            }
        }

        private void updateShadowBindings(TModel lastModel, TModel newModel)
        {
            foreach (var field in typeof(TModel).GetFields(CachedAttribute.ACTIVATOR_FLAGS).Where(f => f.GetCustomAttributes<CachedAttribute>().Any()))
                rebind(field);

            foreach (var property in typeof(TModel).GetProperties(CachedAttribute.ACTIVATOR_FLAGS).Where(f => f.GetCustomAttributes<CachedAttribute>().Any()))
                rebind(property);

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
    }
}
