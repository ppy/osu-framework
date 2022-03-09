// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace osu.Framework.Graphics.Visualisation
{
    /// <summary>
    /// An interface for <see cref="Drawable"/>s which can contain <see cref="VisualisedElement"/>s.
    /// </summary>
    internal interface IContainVisualisedElements
    {
        /// <summary>
        /// Adds a <see cref="VisualiserTreeNode"/> to this <see cref="Drawable"/>'s hierarchy.
        /// </summary>
        /// <remarks>
        /// It is not necessary for this <see cref="Drawable"/> to contain the <see cref="VisualiserTreeNode"/> as an immediate child,
        /// but this <see cref="Drawable"/> will receive <see cref="RemoveVisualiser"/> if the <see cref="VisualiserTreeNode"/> should ever be removed.
        /// </remarks>
        /// <param name="visualiser">The <see cref="VisualiserTreeNode"/> to add.</param>
        void AddVisualiser(VisualiserTreeNode visualiser);

        /// <summary>
        /// Removes a <see cref="VisualiserTreeNode"/> from this <see cref="Drawable"/>.
        /// </summary>
        /// <param name="visualiser">The <see cref="VisualiserTreeNode"/> to remove.</param>
        void RemoveVisualiser(VisualiserTreeNode visualiser);
    }
}
