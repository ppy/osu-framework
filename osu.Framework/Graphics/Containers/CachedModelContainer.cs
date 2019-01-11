// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System.Collections.Generic;
using osu.Framework.Allocation;

namespace osu.Framework.Graphics.Containers
{
    public class CachedModelContainer<TModel> : Container, ICachedModelComposite<TModel>
        where TModel : new()
    {
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

                this.UpdateShadowModel(lastModel, model);
            }
        }

        protected override IReadOnlyDependencyContainer CreateChildDependencies(IReadOnlyDependencyContainer parent)
            => this.CreateDependencies(base.CreateChildDependencies(parent));

        public TModel ShadowModel { get; } = new TModel();
    }
}
