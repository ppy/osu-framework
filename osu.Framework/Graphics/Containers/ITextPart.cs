// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;

namespace osu.Framework.Graphics.Containers
{
    /// <summary>
    /// Represents a part of text inside a <see cref="TextFlowContainer"/>.
    /// This implementation allows for the contents of a text part to be swapped out,
    /// in order to support things like text localisation, for instance.
    /// </summary>
    public interface ITextPart
    {
        /// <summary>
        /// The drawables which currently correspond to this text part.
        /// </summary>
        IEnumerable<Drawable> Drawables { get; }

        /// <summary>
        /// Raised when <see cref="Drawables"/> is reconstructed (e.g. when the user language was changed).
        /// Can be used by consumers to re-apply manual adjustments to the appearance of <see cref="Drawables"/>.
        /// </summary>
        event Action<IEnumerable<Drawable>> DrawablePartsRecreated;

        /// <summary>
        /// Recreates the drawables for this text part, in order for them to be appended to the <paramref name="textFlowContainer"/>.
        /// </summary>
        void RecreateDrawablesFor(TextFlowContainer textFlowContainer);
    }
}
