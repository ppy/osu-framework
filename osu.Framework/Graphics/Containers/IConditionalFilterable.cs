// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Bindables;

namespace osu.Framework.Graphics.Containers
{
    /// <summary>
    /// Am <see cref="IFilterable"/> that additionally allows specifying whether the item can be shown
    /// based on additional criteria.
    /// The item will be visible in a <see cref="SearchContainer"/>
    /// if and only if <see cref="IHasFilterTerms.FilterTerms"/> match
    /// and <see cref="CanBeShown"/> is <see langword="true"/>.
    /// </summary>
    public interface IConditionalFilterable : IFilterable
    {
        IBindable<bool> CanBeShown { get; }
    }
}
