using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Textures;
using osuTK;

namespace FlappyDon.Game.Elements
{
    /// <summary>
    /// A centered sprite used to display
    /// titles and text to the player.
    /// </summary>
    public class TitleSprite : Sprite
    {
        private readonly string textureName;

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
