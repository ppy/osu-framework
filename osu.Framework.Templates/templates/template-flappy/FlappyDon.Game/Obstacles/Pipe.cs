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
    /// A sprite that shows a single green pipe.
    /// There are two pipe sprites for every obstacle the
    /// player must overcome.
    /// </summary>
    public class Pipe : Sprite
    {
        [Resolved]
        private TextureStore textures { get; set; }

        public Pipe()
        {
            Anchor = Anchor.BottomCentre;
            Origin = Anchor.BottomCentre;
            Scale = new Vector2(4.1f);
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            Texture = textures.Get("pipe-green");
        }
    }
}
