using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Textures;
using osuTK;

namespace FlappyDon.Game.Elements
{
    /// <summary>
    /// A sprite that shows a single green pipe. There are two pipe sprites for every obstacle the
    /// player must overcome.
    /// </summary>
    public class Pipe : Sprite
    {
        public Pipe()
        {
            Anchor = Anchor.BottomCentre;
            Origin = Anchor.BottomCentre;
            Scale = new Vector2(4.1f);
        }

        [BackgroundDependencyLoader]
        private void load(TextureStore textures)
        {
            Texture = textures.Get("pipe-green");
        }
    }
}
