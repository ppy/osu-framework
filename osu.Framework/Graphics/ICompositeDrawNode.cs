// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System.Collections.Generic;
using osu.Framework.Graphics.Containers;

namespace osu.Framework.Graphics
{
    /// <summary>
    /// Interface for <see cref="DrawNode"/>s which compose child <see cref="DrawNode"/>s.
    /// <see cref="DrawNode"/>s implementing this interface can be used in <see cref="CompositeDrawable"/>s.
    /// </summary>
    public interface ICompositeDrawNode
    {
        /// <summary>
        /// The child <see cref="DrawNode"/>s to draw.
        /// </summary>
        List<DrawNode> Children { get; set; }

        /// <summary>
        /// Whether <see cref="Children"/> should be updated by the parenting <see cref="CompositeDrawable"/>.
        /// </summary>
        bool AddChildDrawNodes { get; }
    }
}
