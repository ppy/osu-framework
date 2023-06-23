// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using osu.Framework.Graphics.Textures;
using osuTK;

namespace osu.Framework.Graphics.Rendering
{
    public interface IFrameBuffer : IDisposable
    {
        /// <summary>
        /// The framebuffer's backing texture.
        /// </summary>
        Texture Texture { get; }

        /// <summary>
        /// The list of other pixel formats for this framebuffer, or null if not defined.
        /// </summary>
        IReadOnlyList<RenderBufferFormat>? Formats { get; }

        /// <summary>
        /// The framebuffer's texture size.
        /// </summary>
        Vector2 Size { get; set; }

        /// <summary>
        /// Binds the framebuffer.
        /// <para>Does not clear the buffer or reset the viewport/ortho.</para>
        /// </summary>
        void Bind();

        /// <summary>
        /// Unbinds the framebuffer.
        /// </summary>
        void Unbind();
    }

    public static class RenderBufferFormatExtensions
    {
        public static bool HasStencilAttachment(this IFrameBuffer frameBuffer)
        {
            if (frameBuffer.Formats == null)
                return false;

            for (int i = 0; i < frameBuffer.Formats.Count; i++)
            {
                var format = frameBuffer.Formats[i];

                if (format == RenderBufferFormat.D24S8 || format == RenderBufferFormat.D32S8)
                    return true;
            }

            return false;
        }
    }
}
