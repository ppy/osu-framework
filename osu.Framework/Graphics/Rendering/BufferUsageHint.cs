// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace osu.Framework.Graphics.Rendering
{
    /// <summary>
    /// A hint which helps the GPU optimize a buffer.
    /// </summary>
    public enum BufferUsageHint
    {
        /// <summary>
        /// This buffer will be used to draw close to every frame.
        /// </summary>
        StreamDraw,
        /// <summary>
        /// This buffer will be read from close to every frame.
        /// </summary>
        StreamRead,
        /// <summary>
        /// This buffer will be copied to other buffers close to every frame.
        /// </summary>
        StreamCopy,

        /// <summary>
        /// This buffer will be used to draw very often.
        /// </summary>
        DynamicDraw,
        /// <summary>
        /// This buffer will be read very often.
        /// </summary>
        DynamicRead,
        /// <summary>
        /// This buffer will be copied to other buffers very often.
        /// </summary>
        DynamicCopy,

        /// <summary>
        /// This buffer will be used to draw rarely.
        /// </summary>
        StaticDraw,
        /// <summary>
        /// This buffer will be read rarely.
        /// </summary>
        StaticRead,
        /// <summary>
        /// This buffer will be copied to other buffers rarely.
        /// </summary>
        StaticCopy
    }
}
