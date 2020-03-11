// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using osu.Framework.Lists;
using osuTK.Graphics.ES30;

namespace osu.Framework.Graphics.OpenGL.Textures
{
    /// <summary>
    /// A TextureGL which is acting as the backing for an atlas.
    /// </summary>
    internal class TextureGLAtlas : TextureGLSingle
    {
        /// <summary>
        /// Contains all currently-active <see cref="TextureGLAtlas"/>es.
        /// </summary>
        private static readonly LockedWeakList<TextureGLAtlas> all_atlases = new LockedWeakList<TextureGLAtlas>();

        /// <summary>
        /// Invoked when a new <see cref="TextureGLAtlas"/> is created.
        /// </summary>
        /// <remarks>
        /// Invocation from the draw or update thread cannot be assumed.
        /// </remarks>
        public static event Action<TextureGLAtlas> TextureCreated;

        public TextureGLAtlas(int width, int height, bool manualMipmaps, All filteringMode = All.Linear)
            : base(width, height, manualMipmaps, filteringMode)
        {
            all_atlases.Add(this);

            TextureCreated?.Invoke(this);
        }

        /// <summary>
        /// Retrieves all currently-active <see cref="TextureGLAtlas"/>es.
        /// </summary>
        public static TextureGLAtlas[] GetAllAtlases() => all_atlases.ToArray();

        protected override void Dispose(bool isDisposing)
        {
            base.Dispose(isDisposing);

            all_atlases.Remove(this);
        }
    }
}
