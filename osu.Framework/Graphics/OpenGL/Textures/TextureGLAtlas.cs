// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
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
        private static readonly List<TextureGLAtlas> all_atlases = new List<TextureGLAtlas>();

        /// <summary>
        /// Invoked when a new <see cref="TextureGLAtlas"/> is created.
        /// </summary>
        /// <remarks>
        /// Invocation from the draw or update thread cannot be assumed.
        /// </remarks>
        public static event Action<TextureGLAtlas> TextureAdded;

        /// <summary>
        /// Invoked when a <see cref="TextureGLAtlas"/> is discarded.
        /// </summary>
        /// <remarks>
        /// Invocation from the draw or update thread cannot be assumed.
        /// </remarks>
        public static event Action<TextureGLAtlas> TextureRemoved;

        /// <summary>
        /// The total amount of times this <see cref="TextureGLAtlas"/> was bound.
        /// </summary>
        public ulong BindCount { get; private set; }

        public TextureGLAtlas(int width, int height, bool manualMipmaps, All filteringMode = All.Linear)
            : base(width, height, manualMipmaps, filteringMode)
        {
            lock (all_atlases)
                all_atlases.Add(this);

            TextureAdded?.Invoke(this);
        }

        public override bool Bind(TextureUnit unit = TextureUnit.Texture0)
        {
            if (base.Bind(unit))
            {
                // No lock since it doesn't matter if this is slightly out-of-date
                BindCount++;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Retrieves all currently-active <see cref="TextureGLAtlas"/>es.
        /// </summary>
        public static TextureGLAtlas[] GetAllAtlases()
        {
            lock (all_atlases)
                return all_atlases.ToArray();
        }

        protected override void Dispose(bool isDisposing)
        {
            base.Dispose(isDisposing);

            lock (all_atlases)
                all_atlases.Remove(this);

            TextureRemoved?.Invoke(this);
        }
    }
}
