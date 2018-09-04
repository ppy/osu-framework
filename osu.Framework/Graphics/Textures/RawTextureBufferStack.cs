// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Allocation;
using SixLabors.ImageSharp.PixelFormats;

namespace osu.Framework.Graphics.Textures
{
    public class RawTextureBufferStack : RawTextureRgba32
    {
        private readonly BufferStack<Rgba32> bufferStack;

        /// <summary>
        /// Create an empty raw texture with an optional <see cref="BufferStack{T}"/>. backing.
        /// </summary>
        /// <param name="width">The width of the texture.</param>
        /// <param name="height">The height of the texture.</param>
        /// <param name="bufferStack">The buffer stack to retrieve the Rgba32[] from.</param>
        public RawTextureBufferStack(int width, int height, BufferStack<Rgba32> bufferStack)
            : base(width, height, bufferStack.ReserveBuffer(width * height))
        {
            this.bufferStack = bufferStack;
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            bufferStack?.FreeBuffer(Data);
        }
    }
}
