// Copyright (c) 2007-2019 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using osu.Framework.Allocation;
using osu.Framework.Configuration;

namespace osu.Framework.Graphics.Containers
{
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
                        newModelValue = pi.GetValue(newModel);
                        break;
                    case FieldInfo fi:
                        shadowValue = fi.GetValue(shadowModel);
                        lastModelValue = lastModel == null ? null : fi.GetValue(lastModel);
                        newModelValue = fi.GetValue(newModel);
                        break;
                }

                // Todo: This will fail if shadowField is null

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
