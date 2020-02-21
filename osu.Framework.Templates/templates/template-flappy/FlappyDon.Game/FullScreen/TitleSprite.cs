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
    /// A centered sprite used to display
    /// titles and text to the player.
    /// </summary>
    public class TitleSprite : Sprite
    {
        private string textureName;

        [Resolved]
        private TextureStore textures { get; set; }

        public TitleSprite(string textureName)
        {
            this.textureName = textureName;

            Anchor = Anchor.Centre;
            Origin = Anchor.Centre;
            Scale = new Vector2(3.3f);
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            Texture = textures.Get(textureName);
        }

        public void Show(float delay = 0.0f)
        {
            this.Delay(delay).Then().FadeIn();
        }
    }
}
