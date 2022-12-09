using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Textures;
using osuTK;

namespace FlappyDon.Game.Elements
{
    /// <summary>
    /// A centered sprite used to display titles and text to the player.
    /// </summary>
    public partial class TitleSprite : Sprite
    {
        private readonly string textureName;

        [Resolved]
        private TextureStore textures { get; set; }

        /// <summary>
        /// Creates a new screen sprite that will display a message to the player.
        /// </summary>
        /// <param name="textureName">The filename of the texture to display.</param>
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
    }
}
