// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System.Collections.Generic;
using osu.Framework.Allocation;

namespace osu.Framework.Graphics.Containers
{
    public class CachedModelComposite<TModel> : CompositeDrawable, ICachedModelComposite<TModel>
        where TModel : new()
    {
        private TModel model;

        public TModel Model
        {
            private get => model;
            set
            {
                if (EqualityComparer<TModel>.Default.Equals(model, value))
                    return;

                var lastModel = Model;

                model = value;

                this.UpdateShadowModel(lastModel, model);
            }
        }
        
        protected override IReadOnlyDependencyContainer CreateChildDependencies(IReadOnlyDependencyContainer parent)
            => this.CreateDependencies(base.CreateChildDependencies(parent));

        public TModel BoundModel { get; private set; } = new TModel();

        TModel ICachedModelComposite<TModel>.BoundModel
        {
            get => BoundModel;
            set => BoundModel = value;
        }
    }
}