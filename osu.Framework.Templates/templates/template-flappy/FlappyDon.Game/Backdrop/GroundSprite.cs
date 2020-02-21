// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Textures;
using osuTK;

namespace FlappyDon.Game
{
    /// <summary>
    /// Manages the ground texture in the foreground
    /// of the game scene.
    /// </summary>
    public class GroundSprite : Sprite
    {
        [Resolved]
        private TextureStore textures { get; set; }

        public GroundSprite()
        {
            Anchor = Anchor.BottomLeft;
            Origin = Anchor.BottomLeft;
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            Texture = textures.Get("base");
            Scale = new Vector2(2.5f);
            Position = new Vector2(0.0f, 40.0f);
        }
    }
}
