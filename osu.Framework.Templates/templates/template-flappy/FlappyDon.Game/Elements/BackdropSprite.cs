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
    public class BackdropSprite : Sprite
    {
        public float AspectRatio
        {
            get
            {
                if (Texture == null) return 1.0f;

                var size = Texture.Size;
                return size.X / size.Y;
            }
        }

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
            Size = new Vector2(320.0f, 1.0f);
        }

        protected override void Update()
        {
            base.Update();

            // Taking the relative height, calculates the appropriate width.
            // The "Fill" feature of Sprite should really be doing this for us.
            Width = (float)Math.Ceiling(DrawHeight * AspectRatio);
        }
    }
}
