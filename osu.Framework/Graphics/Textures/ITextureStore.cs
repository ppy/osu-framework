// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Threading;
using System.Threading.Tasks;
using osu.Framework.IO.Stores;

namespace osu.Framework.Graphics.Textures
{
    /// <summary>
    /// Provides drawable-ready <see cref="Texture"/>s.
    /// </summary>
    public interface ITextureStore : IResourceStore<Texture>
    {
        /// <summary>
        /// Retrieves a texture from the store.
        /// </summary>
        /// <param name="name">The name of the texture.</param>
        /// <param name="wrapModeS">The texture wrap mode in horizontal direction.</param>
        /// <param name="wrapModeT">The texture wrap mode in vertical direction.</param>
        /// <returns>The texture.</returns>
        Texture? Get(string name, WrapMode wrapModeS, WrapMode wrapModeT);

        /// <summary>
        /// Retrieves a texture from the store asynchronously.
        /// </summary>
        /// <param name="name">The name of the texture.</param>
        /// <param name="wrapModeS">The texture wrap mode in horizontal direction.</param>
        /// <param name="wrapModeT">The texture wrap mode in vertical direction.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns>The texture.</returns>
        Task<Texture?> GetAsync(string name, WrapMode wrapModeS, WrapMode wrapModeT, CancellationToken cancellationToken = default);
    }
}
