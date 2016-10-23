// Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using osu.Framework.DebugUtils;
using osu.Framework.Graphics.Batches;
using osu.Framework.Graphics.Primitives;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.ES20;
using RectangleF = osu.Framework.Graphics.Primitives.RectangleF;
using osu.Framework.Graphics.Shaders;

namespace osu.Framework.Graphics.OpenGL.Textures
{
    class TextureGLSingle : TextureGL
    {
        private static VertexBatch<TexturedVertex2D> spriteBatch;

        private ConcurrentQueue<TextureUpload> uploadQueue = new ConcurrentQueue<TextureUpload>();

        private int internalWidth;
        private int internalHeight;

        private TextureWrapMode internalWrapMode;

        public override bool Loaded => textureId > 0 || uploadQueue.Count > 0;

        public TextureGLSingle(int width, int height, bool manualMipmaps = false)
        {
            Width = width;
            Height = height;
            this.manualMipmaps = manualMipmaps;
        }

        #region Disposal

        protected override void Dispose(bool isDisposing)
        {
            base.Dispose(isDisposing);
            unload();
        }

        /// <summary>
        /// Removes texture from GL memory.
        /// </summary>
        private void unload()
        {
            TextureUpload u;
            while (uploadQueue.TryDequeue(out u))
                u.Dispose();

            int disposableId = textureId;

            if (disposableId <= 0)
                return;

            GLWrapper.DeleteTextures(disposableId);

            textureId = 0;
        }

        #endregion

        private int height;

        public override TextureGL Native => this;

        public override int Height
        {
            get
            {
                Debug.Assert(!isDisposed);
                return height;
            }

            set
            {
                Debug.Assert(!isDisposed);
                height = value;
            }
        }

        private int width;

        public override int Width
        {
            get
            {
                Debug.Assert(!isDisposed);
                return width;
            }

            set
            {
                Debug.Assert(!isDisposed);
                width = value;
            }
        }

        private int textureId;

        public override int TextureId
        {
            get
            {
                Debug.Assert(!isDisposed);
                return textureId;
            }
        }

        private static void rotateVector(ref Vector2 toRotate, float sin, float cos)
        {
            float oldX = toRotate.X;
            toRotate.X = toRotate.X * cos - toRotate.Y * sin;
            toRotate.Y = oldX * sin + toRotate.Y * cos;
        }

        public override RectangleF GetTextureRect(RectangleF? textureRect)
        {
            RectangleF texRect = textureRect != null
                ? new RectangleF(textureRect.Value.X, textureRect.Value.Y, textureRect.Value.Width, textureRect.Value.Height)
                : new RectangleF(0, 0, Width, Height);

            texRect.X /= width;
            texRect.Y /= height;
            texRect.Width /= width;
            texRect.Height /= height;

            return texRect;
        }

        /// <summary>
        /// Blits sprite to OpenGL display with specified parameters.
        /// </summary>
        public override void Draw(Quad vertexQuad, RectangleF? textureRect, Color4 drawColour, VertexBatch<TexturedVertex2D> spriteBatch = null)
        {
            Debug.Assert(!isDisposed);

            RectangleF texRect = GetTextureRect(textureRect);

            if (spriteBatch == null)
            {
                if (TextureGLSingle.spriteBatch == null)
                    TextureGLSingle.spriteBatch = new QuadBatch<TexturedVertex2D>(512, 64);
                spriteBatch = TextureGLSingle.spriteBatch;
            }

            spriteBatch.Add(new TexturedVertex2D
            {
                Position = vertexQuad.BottomLeft,
                TexturePosition = new Vector2(texRect.Left, texRect.Bottom),
                Colour = drawColour
            });
            spriteBatch.Add(new TexturedVertex2D
            {
                Position = vertexQuad.BottomRight,
                TexturePosition = new Vector2(texRect.Right, texRect.Bottom),
                Colour = drawColour
            });
            spriteBatch.Add(new TexturedVertex2D
            {
                Position = vertexQuad.TopRight,
                TexturePosition = new Vector2(texRect.Right, texRect.Top),
                Colour = drawColour
            });
            spriteBatch.Add(new TexturedVertex2D
            {
                Position = vertexQuad.TopLeft,
                TexturePosition = new Vector2(texRect.Left, texRect.Top),
                Colour = drawColour
            });
        }

        private void updateWrapMode()
        {
            Debug.Assert(!isDisposed);

            internalWrapMode = WrapMode;
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)internalWrapMode);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)internalWrapMode);
        }

        public override void SetData(TextureUpload upload)
        {
            Debug.Assert(!isDisposed);

            if (upload.Bounds == Rectangle.Empty)
                upload.Bounds = new Rectangle(0, 0, width, height);

            IsTransparent = false;

            bool requireUpload = uploadQueue.Count == 0;
            uploadQueue.Enqueue(upload);
            if (requireUpload)
                GLWrapper.EnqueueTextureUpload(this);
        }

        public override bool Bind()
        {
            Debug.Assert(!isDisposed);

            Upload();

            if (textureId <= 0)
                return false;

            if (IsTransparent)
                return false;

            GLWrapper.BindTexture(this);

            if (internalWrapMode != WrapMode)
                updateWrapMode();

            return true;
        }

        bool manualMipmaps;

        internal override bool Upload()
        {
            // We should never run raw OGL calls on another thread than the main thread due to race conditions.
            ThreadSafety.EnsureDrawThread();

            if (isDisposed)
                return false;

            IntPtr dataPointer;
            GCHandle? h0;
            TextureUpload upload;
            bool didUpload = false;

            while (uploadQueue.TryDequeue(out upload))
            {
                if (upload.Data.Length == 0)
                {
                    h0 = null;
                    dataPointer = IntPtr.Zero;
                }
                else
                {
                    h0 = GCHandle.Alloc(upload.Data, GCHandleType.Pinned);
                    dataPointer = h0.Value.AddrOfPinnedObject();
                    didUpload = true;
                }

                try
                {
                    // Do we need to generate a new texture?
                    if (textureId <= 0 || internalWidth < width || internalHeight < height)
                    {
                        internalWidth = width;
                        internalHeight = height;

                        // We only need to generate a new texture if we don't have one already. Otherwise just re-use the current one.
                        if (textureId <= 0)
                        {
                            int[] textures = new int[1];
                            GL.GenTextures(1, textures);

                            textureId = textures[0];

                            GLWrapper.BindTexture(this);
                            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)(manualMipmaps ? All.Linear :  All.LinearMipmapLinear));
                            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)All.Linear);

                            updateWrapMode();
                        }
                        else
                            GLWrapper.BindTexture(this);

                        if (width == upload.Bounds.Width && height == upload.Bounds.Height || dataPointer == IntPtr.Zero)
                            GL.TexImage2D(TextureTarget2d.Texture2D, upload.Level, TextureComponentCount.Rgba, width, height, 0, upload.Format, PixelType.UnsignedByte, dataPointer);
                        else
                        {
                            initializeLevel(upload.Level, width, height);

                            GL.TexSubImage2D(TextureTarget2d.Texture2D, upload.Level, upload.Bounds.X, upload.Bounds.Y, upload.Bounds.Width, upload.Bounds.Height, upload.Format, PixelType.UnsignedByte,
                                dataPointer);
                        }
                    }
                    // Just update content of the current texture
                    else if (dataPointer != IntPtr.Zero)
                    {
                        GLWrapper.BindTexture(this);

                        if (!manualMipmaps && upload.Level > 0)
                        {
                            //allocate mipmap levels
                            int level = 1;
                            int d = 2;

                            while (width / d > 0)
                            {
                                initializeLevel(level, width / d, height / d);
                                level++;
                                d *= 2;
                            }

                            manualMipmaps = true;
                        }

                        int div = (int)Math.Pow(2, upload.Level);

                        GL.TexSubImage2D(TextureTarget2d.Texture2D, upload.Level, upload.Bounds.X / div, upload.Bounds.Y / div, upload.Bounds.Width / div, upload.Bounds.Height / div, upload.Format,
                            PixelType.UnsignedByte, dataPointer);
                    }
                }
                finally
                {
                    h0?.Free();
                    upload.Dispose();
                }
            }

            if (didUpload && !manualMipmaps)
            {
                GL.Hint(HintTarget.GenerateMipmapHint, HintMode.Nicest);
                GL.GenerateMipmap(TextureTarget.Texture2D);
            }

            return didUpload;
        }

        //private static int clearFBO = -1;

        private void initializeLevel(int level, int width, int height)
        {
            byte[] transparentWhite = new byte[width * height * 4];
            int i = 0;
            while ((i += 4) < transparentWhite.Length)
            {
                transparentWhite[i] = 255;
                transparentWhite[i + 1] = 255;
                transparentWhite[i + 2] = 255;
            }

            GCHandle h0 = GCHandle.Alloc(transparentWhite, GCHandleType.Pinned);
            GL.TexImage2D(TextureTarget2d.Texture2D, level, TextureComponentCount.Rgba, width, height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, h0.AddrOfPinnedObject());
            h0.Free();

            //todo: figure why FBO clear method doesn't work.

            //if (clearFBO < 0)
            //    clearFBO = GL.GenFramebuffer();

            //int lastFramebuffer = GLWrapper.BindFrameBuffer(clearFBO);
            //GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, All.ColorAttachment0, TextureTarget2d.Texture2D, TextureId, 0);

            //GL.ClearColor(new Color4(255, 255, 255, 0));
            //GL.Clear(ClearBufferMask.ColorBufferBit);
            //GL.ClearColor(new Color4(0, 0, 0, 0));

            //GLWrapper.BindFrameBuffer(lastFramebuffer);
        }
    }
}
