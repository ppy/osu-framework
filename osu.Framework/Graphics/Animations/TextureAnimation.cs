// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using OpenTK;
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
        private Vector2 maxTextureSize = Vector2.Zero;

        public TextureAnimation()
        {
            Child = textureHolder = new Sprite
            {
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
            };
        }

        protected override void OnFrameAdded(Texture content, double displayDuration)
        {
            base.OnFrameAdded(content, displayDuration);

            maxTextureSize = Vector2.ComponentMax(new Vector2(content?.DisplayWidth ?? 0, content?.DisplayHeight ?? 0), maxTextureSize);
        }

        protected override void DisplayFrame(Texture content)
        {
            textureHolder.Texture = content;
            updateTextureHolderSize();
        }

        protected override void OnSizingChanged()
        {
            base.OnSizingChanged();

            if (textureHolder == null)
                return;

            textureHolder.RelativeSizeAxes = ~AutoSizeAxes;
            updateTextureHolderSize();
        }

        private void updateTextureHolderSize()
        {
            var newSize = Vector2.Zero;
            var content = textureHolder.Texture;
            if (content == null)
                return;

            if ((AutoSizeAxes & Axes.X) != 0)
                newSize.X = content.DisplayWidth;
            else
                newSize.X = content.DisplayWidth / maxTextureSize.X;

            if ((AutoSizeAxes & Axes.Y) != 0)
                newSize.Y = content.DisplayHeight;
            else
                newSize.Y = content.DisplayHeight / maxTextureSize.Y;

            textureHolder.Size = newSize;
        }
    }
}
