// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

namespace osu.Framework.Graphics.Visualisation
{
    /// <summary>
    /// An interface for <see cref="Drawable"/>s which can contain <see cref="VisualisedDrawable"/>s.
    /// </summary>
    internal interface IContainVisualisedDrawables
    {
        /// <summary>
        /// Adds a <see cref="VisualisedDrawable"/> to this <see cref="Drawable"/>'s hierarchy.
        /// </summary>
        /// <remarks>
        /// It is not necessary for this <see cref="Drawable"/> to contain the <see cref="VisualisedDrawable"/> as an immediate child,
        /// but this <see cref="Drawable"/> will receive <see cref="RemoveVisualiser"/> if the <see cref="VisualisedDrawable"/> should ever be removed.
        /// </remarks>
        /// <param name="visualiser">The <see cref="VisualisedDrawable"/> to add.</param>
        void AddVisualiser(VisualisedDrawable visualiser);

        /// <summary>
        /// Removes a <see cref="VisualisedDrawable"/> from this <see cref="Drawable"/>.
        /// </summary>
        /// <param name="visualiser">The <see cref="VisualisedDrawable"/> to remove.</param>
        void RemoveVisualiser(VisualisedDrawable visualiser);
    }
}
