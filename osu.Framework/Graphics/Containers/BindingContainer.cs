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
            {
                var shadowField = field.GetValue(shadowModel);
                var lastModelField = lastModel == null ? null : field.GetValue(lastModel);
                var modelField = field.GetValue(model);

                // Todo: This will fail if shadowField is null

                if (shadowField is IBindable bindableShadowField)
                {
                    // Unbind from the last model
                    if (lastModelField is IBindable bindableLastModelField)
                        bindableShadowField.UnbindFrom(bindableLastModelField);

                    // Bind to the new model
                    if (modelField is IBindable bindableModelField)
                        bindableShadowField.BindTo(bindableModelField);
                }
            }
        }
    }
}
