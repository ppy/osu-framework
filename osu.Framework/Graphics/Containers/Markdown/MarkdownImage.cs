// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

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

        /// <summary>
        /// Creates the content of this <see cref="MarkdownImage"/>, including the <see cref="ImageContainer"/>.
        /// </summary>
        /// <param name="url">The image url.</param>
        protected virtual Drawable CreateContent(string url) => new DelayedLoadWrapper(CreateImageContainer(url));

        /// <summary>
        /// Creates an <see cref="ImageContainer"/> to display the image.
        /// </summary>
        /// <param name="url">The image URL.</param>
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

            /// <summary>
            /// Creates a <see cref="Sprite"/> to display the image.
            /// </summary>
            protected virtual Sprite CreateImageSprite() => new Sprite();

            /// <summary>
            /// Retrieves a <see cref="Texture"/> for the image.
            /// </summary>
            /// <param name="textures">The texture store.</param>
            /// <param name="url">The image URL.</param>
            /// <returns>The image's <see cref="Texture"/>.</returns>
            protected virtual Texture GetImageTexture(TextureStore textures, string url)
            {
                Texture texture = null;
                if (!string.IsNullOrEmpty(url))
                    texture = textures.Get(url);

                // Use a default texture
                texture ??= GetNotFoundTexture(textures);
                return texture;
            }

            /// <summary>
            /// Retrieves a default <see cref="Texture"/> to be displayed when the image can't be loaded.
            /// </summary>
            /// <param name="textures">The texture store.</param>
            /// <returns>The <see cref="Texture"/>.</returns>
            protected virtual Texture GetNotFoundTexture(TextureStore textures) => null;
        }
    }
}
