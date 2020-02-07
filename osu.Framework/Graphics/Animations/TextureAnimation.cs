// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osuTK;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Textures;

namespace osu.Framework.Graphics.Animations
{
    /// <summary>
    /// An animation that switches the displayed texture when a new frame is displayed.
    /// </summary>
    public class TextureAnimation : Animation<Texture>
    {
        private readonly Sprite textureHolder;

        public TextureAnimation()
        {
            InternalChild = textureHolder = new Sprite
            {
                RelativeSizeAxes = Axes.Both,
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
            };
        }

        protected override void DisplayFrame(Texture content)
        {
            textureHolder.Texture = content;
        }

        protected override Vector2 GetFrameSize(Texture content) => new Vector2(content?.DisplayWidth ?? 0, content?.DisplayHeight ?? 0);
    }
}
