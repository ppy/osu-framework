// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Allocation;
using osu.Framework.Configuration;

namespace osu.Framework.Graphics.Containers
{
    internal interface ICachedModelComposite<TModel>
        where TModel : new()
    {
        /// <summary>
        /// The model which provides the values for <see cref="BoundModel"/>.
        /// </summary>
        /// <remarks>
        /// Children of this <see cref="ICachedModelComposite{TModel}"/> can resolve the cached members by using <see cref="ResolvedAttribute.Parent"/> = typeof(<see cref="TModel"/>).
        /// Any non-bindable members are not updated in children when this value is set.
        /// </remarks>
        TModel Model { set; }

        /// <summary>
        /// The <see cref="TModel"/> which is cached. All <see cref="IBindable"/>s in this object are bound to those of <see cref="Model"/>.
        /// </summary>
        /// <remarks>
        /// It is safe to directly bind to <see cref="IBindable"/>s of this object.
        /// </remarks>
        TModel BoundModel { get; set; }
    }
}