// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace osu.Framework.Graphics.Visualisation.Tree
{
    /// <summary>
    /// An interface for <see cref="Drawable"/>s which can contain <see cref="ElementNode"/>s.
    /// </summary>
    internal interface INodeContainer
    {
        /// <summary>
        /// Adds a <see cref="TreeNode"/> to this <see cref="Drawable"/>'s hierarchy.
        /// </summary>
        /// <remarks>
        /// It is not necessary for this <see cref="Drawable"/> to contain the <see cref="TreeNode"/> as an immediate child,
        /// but this <see cref="Drawable"/> will receive <see cref="RemoveVisualiser"/> if the <see cref="TreeNode"/> should ever be removed.
        /// </remarks>
        /// <param name="visualiser">The <see cref="TreeNode"/> to add.</param>
        void AddVisualiser(TreeNode visualiser);

        /// <summary>
        /// Removes a <see cref="TreeNode"/> from this <see cref="Drawable"/>.
        /// </summary>
        /// <param name="visualiser">The <see cref="TreeNode"/> to remove.</param>
        void RemoveVisualiser(TreeNode visualiser);
    }
}
