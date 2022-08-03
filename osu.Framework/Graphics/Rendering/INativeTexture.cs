// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Graphics.Textures;
using osu.Framework.Graphics.Visualisation;

namespace osu.Framework.Graphics.Rendering
{
    internal interface INativeTexture : IDisposable
    {
        /// <summary>
        /// The renderer that created this texture.
        /// </summary>
        IRenderer Renderer { get; }

        /// <summary>
        /// An identifier for this texture, to show up in the <see cref="TextureVisualiser"/>.
        /// </summary>
        string Identifier { get; }

        /// <summary>
        /// Maximum texture size in any direction.
        /// </summary>
        int MaxSize { get; }

        /// <summary>
        /// The width of the texture.
        /// </summary>
        int Width { get; set; }

        /// <summary>
        /// The height of the texture.
        /// </summary>
        int Height { get; set; }

        /// <summary>
        /// Whether the texture is in a usable state.
        /// </summary>
        bool Available { get; }

        /// <summary>
        /// By default, texture uploads are queued for upload at the beginning of each frame, allowing loading them ahead of time.
        /// When this is true, this will be bypassed and textures will only be uploaded on use. Should be set for every-frame texture uploads
        /// to avoid overloading the global queue.
        /// </summary>
        bool BypassTextureUploadQueueing { get; set; }

        /// <summary>
        /// Whether the latest data has been uploaded.
        /// </summary>
        bool UploadComplete { get; }

        /// <summary>
        /// Flush any unprocessed uploads without actually uploading.
        /// </summary>
        void FlushUploads();

        /// <summary>
        /// Sets the pixel data of the texture.
        /// </summary>
        /// <param name="upload">The <see cref="ITextureUpload"/> containing the data.</param>
        void SetData(ITextureUpload upload);

        /// <summary>
        /// Uploads this texture.
        /// </summary>
        /// <returns>Whether any uploads occurred.</returns>
        bool Upload();

        /// <summary>
        /// Binds this texture for drawing.
        /// </summary>
        /// <param name="unit">The sampling unit in which the texture is to be bound.</param>
        /// <param name="wrapModeS">The horizontal wrap mode.</param>
        /// <param name="wrapModeT">The vertical wrap mode.</param>
        /// <returns>Whether the texture was successfully bound.</returns>
        bool Bind(int unit, WrapMode wrapModeS, WrapMode wrapModeT);

        /// <summary>
        /// The size of this texture in bytes.
        /// </summary>
        int GetByteSize();

        /// <summary>
        /// The total amount of times this <see cref="INativeTexture"/> has ever been bound.
        /// </summary>
        ulong BindCount { get; }
    }
}
