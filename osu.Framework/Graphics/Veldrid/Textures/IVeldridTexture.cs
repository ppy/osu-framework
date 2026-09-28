// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics.Rendering;
using Veldrid;

namespace osu.Framework.Graphics.Veldrid.Textures
{
    internal interface IVeldridTexture : INativeTexture
    {
        /// <summary>
        /// The number of subsequent texture units this texture attaches to.
        /// </summary>
        int ResourceCount { get; }

        /// <summary>
        /// Retrieves the veldrid texture at a given index.
        /// </summary>
        /// <param name="index">The index of the texture.</param>
        Texture? GetVeldridTexture(int index);

        /// <summary>
        /// Gets the <see cref="ResourceSet"/> containing the texture attached to the given layout.
        /// </summary>
        /// <param name="index">The index of the texture to be attached.</param>
        /// <param name="layout">The <see cref="ResourceLayout"/> which the texture should be attached to.</param>
        ResourceSet? GetResourceSet(int index, ResourceLayout layout);
    }
}
