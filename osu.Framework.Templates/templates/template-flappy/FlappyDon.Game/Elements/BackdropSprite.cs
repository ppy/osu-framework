using System;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Textures;
using osuTK;

namespace FlappyDon.Game.Elements
{
    /// <summary>
    /// Manages the buildings and sky texture in the far background of the game scene.
    /// The texture is aspect scaled up to the height of the game window.
    /// </summary>
    public partial class BackdropSprite : Sprite
    {
        public BackdropSprite()
        {
            Anchor = Anchor.TopLeft;
            Origin = Anchor.TopLeft;
        }

        [BackgroundDependencyLoader]
        private void load(TextureStore textures)
        {
            Texture = textures.Get("background-day");
            RelativeSizeAxes = Axes.Y;
            Height = 1.0f;
        }

        protected override void Update()
        {
            base.Update();

            Vector2 size = Texture.Size;
            double aspectRatio = size.X / size.Y;

            // Taking the relative height, calculates the appropriate width.
            // The "Fill" feature of Sprite should really be doing this for us.
            Width = (float)Math.Ceiling(DrawHeight * aspectRatio);
        }
    }
}
