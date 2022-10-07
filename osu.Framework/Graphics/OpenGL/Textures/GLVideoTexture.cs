// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Diagnostics;
using osu.Framework.Graphics.Textures;
using osu.Framework.Graphics.Video;
using osu.Framework.Platform;
using osuTK.Graphics.ES30;

namespace osu.Framework.Graphics.OpenGL.Textures
{
    internal unsafe class GLVideoTexture : GLTexture
    {
        public int[]? TextureIds { get; private set; }

        public GLVideoTexture(GLRenderer renderer, int width, int height)
            : base(renderer, width, height, true)
        {
        }

        private NativeMemoryTracker.NativeMemoryLease? memoryLease;

        public override int TextureId => TextureIds?[0] ?? 0;

        private int textureSize;

        public override int GetByteSize() => textureSize;

        protected override void DoUpload(ITextureUpload upload, IntPtr dataPointer)
        {
            if (!(upload is VideoTextureUpload videoUpload))
                return;

            // Do we need to generate a new texture?
            if (TextureIds == null)
            {
                Debug.Assert(memoryLease == null);
                memoryLease = NativeMemoryTracker.AddMemory(this, Width * Height * 3 / 2);

                TextureIds = new int[3];
                GL.GenTextures(TextureIds.Length, TextureIds);

                Renderer.BindTexture(this);

                for (uint i = 0; i < TextureIds.Length; i++)
                {
                    int width = videoUpload.GetPlaneWidth(i);
                    int height = videoUpload.GetPlaneHeight(i);

                    textureSize += width * height;

                    GL.ActiveTexture(TextureUnit.Texture0 + (int)i);

                    GL.TexImage2D(TextureTarget2d.Texture2D, 0, TextureComponentCount.R8, width, height,
                        0, PixelFormat.Red, PixelType.UnsignedByte, IntPtr.Zero);

                    GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)All.Linear);
                    GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)All.Linear);

                    GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
                    GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
                }
            }

            Renderer.BindTexture(this);

            for (uint i = 0; i < TextureIds.Length; i++)
            {
                GL.ActiveTexture(TextureUnit.Texture0 + (int)i);

                GL.PixelStore(PixelStoreParameter.UnpackRowLength, videoUpload.Frame->linesize[i]);

                GL.TexSubImage2D(TextureTarget2d.Texture2D, 0, 0, 0, videoUpload.GetPlaneWidth(i), videoUpload.GetPlaneHeight(i),
                    PixelFormat.Red, PixelType.UnsignedByte, (IntPtr)videoUpload.Frame->data[i]);

                GL.PixelStore(PixelStoreParameter.UnpackRowLength, 0);
            }
        }

        #region Disposal

        protected override void Dispose(bool isDisposing)
        {
            base.Dispose(isDisposing);

            memoryLease?.Dispose();

            Renderer.ScheduleDisposal(v =>
            {
                int[]? ids = v.TextureIds;

                if (ids == null)
                    return;

                for (int i = 0; i < ids.Length; i++)
                {
                    if (ids[i] >= 0)
                        GL.DeleteTextures(1, new[] { ids[i] });
                }
            }, this);
        }

        #endregion
    }
}
