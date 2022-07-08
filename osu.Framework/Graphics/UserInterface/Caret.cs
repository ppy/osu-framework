// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using osu.Framework.Graphics.Containers;
using osuTK;

namespace osu.Framework.Graphics.UserInterface
{
    /// <summary>
    /// A UI component generally used to show the current cursor location in a text edit field.
    /// </summary>
    public abstract class Caret : CompositeDrawable
    {
        /// <summary>
        /// Request the caret be displayed at a particular location, with an optional selection length.
        /// </summary>
        /// <param name="position">The position (in parent space) where the caret should be displayed.</param>
        /// <param name="selectionWidth">If a selection is active, the length (in parent space) of the selection. The caret should extend to display this selection to the user.</param>
        public abstract void DisplayAt(Vector2 position, float? selectionWidth);
    }
}
