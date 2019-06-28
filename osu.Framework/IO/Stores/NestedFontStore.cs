// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics.Textures;

namespace osu.Framework.IO.Stores
{
    /// <summary>
    /// A font store which can be ntested within a parent font store to allow better management of font lookup precedence.
    /// Shares its texture atlas with a parent store.
    /// </summary>
    public class NestedFontStore : FontStore
    {
        public NestedFontStore(IResourceStore<TextureUpload> store = null, float scaleAdjust = 100)
            : base(store, scaleAdjust, useAtlas: false)
        {
        }
    }
}
