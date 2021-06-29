// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Textures;

namespace osu.Framework.Graphics.Containers.Markdown
{
    /// <summary>
    /// Visualises an image.
    /// </summary>
    /// <code>
    /// ![alt text](url)
    /// </code>
    public class MarkdownImage : CompositeDrawable
    {
        private readonly string url;

        public MarkdownImage(string url)
        {
            this.url = url;

            AutoSizeAxes = Axes.Both;
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            InternalChild = CreateContent(url);
        }

        protected virtual Drawable CreateContent(string url) => new DelayedLoadWrapper(CreateImageContainer(url));

        protected virtual ImageContainer CreateImageContainer(string url)
        {
            var converter = new ImageContainer(url);
            converter.OnLoadComplete += d => d.FadeInFromZero(300, Easing.OutQuint);
            return converter;
        }

        protected class ImageContainer : CompositeDrawable
        {
            private readonly string url;
            private readonly Sprite image;

            public ImageContainer(string url)
            {
                this.url = url;

                AutoSizeAxes = Axes.Both;

                InternalChild = image = CreateImageSprite();
            }

            [BackgroundDependencyLoader]
            private void load(TextureStore textures)
            {
                image.Texture = GetImageTexture(textures, url);
            }

            protected virtual Sprite CreateImageSprite() => new Sprite();

            protected virtual Texture GetImageTexture(TextureStore textures, string url)
            {
                Texture texture = null;
                if (!string.IsNullOrEmpty(url))
                    texture = textures.Get(url);

                // Use a default texture
                texture ??= GetNotFoundTexture(textures);
                return texture;
            }

            protected virtual Texture GetNotFoundTexture(TextureStore textures) => null;
        }
    }
}
