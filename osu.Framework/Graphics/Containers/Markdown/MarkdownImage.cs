// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Allocation;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Textures;
using OpenTK;
using OpenTK.Graphics;

namespace osu.Framework.Graphics.Containers.Markdown
{
    /// <summary>
    /// Load image from url
    /// </summary>
    public class MarkdownImage : CompositeDrawable
    {
        private readonly Box background;
        public MarkdownImage(string url)
        {
            InternalChildren = new Drawable[]
            {
                background = new Box
                {
                    RelativeSizeAxes = Axes.Both,
                    Colour = Color4.LightGray,
                    Alpha = 0.3f
                },
                new DelayedLoadWrapper(
                    new ImageContainer(url)
                    {
                        RelativeSizeAxes = Axes.Both,
                        OnLoadComplete = d =>
                        {
                            if(d is ImageContainer imageContainer)
                                EffectLoadImageComplete(imageContainer);
                        },
                    })
            };
        }

        protected virtual void EffectLoadImageComplete(ImageContainer imageContainer)
        {
            var rowImageSize = imageContainer.Image?.Texture?.Size ?? new Vector2();
            //Resize to image's row size
            this.ResizeWidthTo(rowImageSize.X, 700, Easing.OutQuint);
            this.ResizeHeightTo(rowImageSize.Y, 700, Easing.OutQuint);

            //Hide background image
            background.FadeTo(0, 300, Easing.OutQuint);
            imageContainer.FadeInFromZero(300, Easing.OutQuint);
        }

        protected class ImageContainer : CompositeDrawable
        {
            private readonly string imageUrl;
            private readonly Sprite image;

            public Sprite Image => image;

            public ImageContainer(string url)
            {
                imageUrl = url;
                InternalChildren = new Drawable[]
                {
                    image = new Sprite
                    {
                        RelativeSizeAxes = Axes.Both,
                        FillMode = FillMode.Fit,
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre
                    }
                };
            }

            [BackgroundDependencyLoader]
            private void load(TextureStore textures)
            {
                Texture texture = null;
                if (!string.IsNullOrEmpty(imageUrl))
                    texture = textures.Get(imageUrl);

                //TODO : get default texture
                //if (texture == null)
                //    texture = textures.Get(@"Markdown/default-image");

                image.Texture = texture;
            }
        }
    }
}
