// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System.Collections.Generic;

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

                this.UpdateShadowBindings(lastModel, model);
            }
        }

        public TModel BoundModel { get; } = new TModel();
    }
}