// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

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
        private Sprite textureHolder;

        public TextureAnimation(bool startAtCurrentTime = true)
            : base(startAtCurrentTime)
        {
        }

        public override Drawable CreateContent() => textureHolder = new Sprite
        {
            RelativeSizeAxes = Axes.Both,
            Anchor = Anchor.Centre,
            Origin = Anchor.Centre,
        };

        protected override void DisplayFrame(Texture content) => textureHolder.Texture = content;

        protected override void ClearDisplay() => textureHolder.Texture = null;

        protected override float GetFillAspectRatio() => textureHolder.FillAspectRatio;

        protected override Vector2 GetCurrentDisplaySize() =>
            new Vector2(textureHolder.Texture?.DisplayWidth ?? 0, textureHolder.Texture?.DisplayHeight ?? 0);
    }
}
