// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using osu.Framework.Graphics.OpenGL.Buffers;
using osu.Framework.Graphics.OpenGL.Textures;
using osu.Framework.Graphics.Rendering;
using osu.Framework.Graphics.Shaders;
using osu.Framework.Graphics.Textures;
using osu.Framework.Graphics.Video;
using osu.Framework.Lists;
using osu.Framework.Statistics;
using osuTK;
using osuTK.Graphics.ES30;
using SixLabors.ImageSharp.PixelFormats;

namespace osu.Framework.Graphics.OpenGL
{
    public class OpenGLRenderer : IRenderer
    {
        public WrapMode CurrentWrapModeS { get; private set; }
        public WrapMode CurrentWrapModeT { get; private set; }
        public Texture WhitePixel => whitePixel.Value;

        // in case no other textures are used in the project, create a new atlas as a fallback source for the white pixel area (used to draw boxes etc.)
        private readonly Lazy<TextureWhitePixel> whitePixel;
        private readonly LockedWeakList<Texture> allTextures = new LockedWeakList<Texture>();

        private readonly bool[] lastBoundTextureIsAtlas = new bool[16];
        private readonly int[] lastBoundTexture = new int[16];
        private int lastActiveTextureUnit;

        public OpenGLRenderer()
        {
            whitePixel = new Lazy<TextureWhitePixel>(() =>
                new TextureAtlas(this, TextureAtlas.WHITE_PIXEL_SIZE + TextureAtlas.PADDING, TextureAtlas.WHITE_PIXEL_SIZE + TextureAtlas.PADDING, true).WhitePixel);
        }

        void IRenderer.BeginFrame(Vector2 windowSize)
        {
            lastBoundTexture.AsSpan().Clear();
            lastBoundTextureIsAtlas.AsSpan().Clear();
        }

        public bool BindTexture(Texture texture, int unit = 0, WrapMode? wrapModeS = null, WrapMode? wrapModeT = null)
        {
            if (texture is TextureWhitePixel && lastBoundTextureIsAtlas[unit])
            {
                // We can use the special white space from any atlas texture.
                return true;
            }

            bool didBind = texture.Bind(unit, wrapModeS ?? texture.WrapModeS, wrapModeT ?? texture.WrapModeT);
            lastBoundTextureIsAtlas[unit] = texture is TextureAtlasRegion;

            return didBind;
        }

        internal bool BindTexture(int textureId, int unit = 0, WrapMode wrapModeS = WrapMode.None, WrapMode wrapModeT = WrapMode.None)
        {
            if (wrapModeS != CurrentWrapModeS)
            {
                // Will flush the current batch internally.
                GlobalPropertyManager.Set(GlobalProperty.WrapModeS, (int)wrapModeS);
                CurrentWrapModeS = wrapModeS;
            }

            if (wrapModeT != CurrentWrapModeT)
            {
                // Will flush the current batch internally.
                GlobalPropertyManager.Set(GlobalProperty.WrapModeT, (int)wrapModeT);
                CurrentWrapModeT = wrapModeT;
            }

            if (lastActiveTextureUnit == unit && lastBoundTexture[unit] == textureId)
                return false;

            GLWrapper.FlushCurrentBatch();

            GL.ActiveTexture(TextureUnit.Texture0 + unit);
            GL.BindTexture(TextureTarget.Texture2D, textureId);

            lastBoundTexture[unit] = textureId;
            lastBoundTextureIsAtlas[unit] = false;
            lastActiveTextureUnit = unit;

            FrameStatistics.Increment(StatisticsCounterType.TextureBinds);
            return true;
        }

        public IFrameBuffer CreateFrameBuffer(RenderBufferFormat[]? renderBufferFormats = null, TextureFilteringMode filteringMode = TextureFilteringMode.Linear)
        {
            All glFilteringMode;
            RenderbufferInternalFormat[]? glFormats = null;

            switch (filteringMode)
            {
                case TextureFilteringMode.Linear:
                    glFilteringMode = All.Linear;
                    break;

                case TextureFilteringMode.Nearest:
                    glFilteringMode = All.Nearest;
                    break;

                default:
                    throw new ArgumentException($"Unsupported filtering mode: {filteringMode}", nameof(filteringMode));
            }

            if (renderBufferFormats != null)
            {
                glFormats = new RenderbufferInternalFormat[renderBufferFormats.Length];

                for (int i = 0; i < renderBufferFormats.Length; i++)
                {
                    switch (renderBufferFormats[i])
                    {
                        case RenderBufferFormat.D16:
                            glFormats[i] = RenderbufferInternalFormat.DepthComponent16;
                            break;

                        default:
                            throw new ArgumentException($"Unsupported render buffer format: {renderBufferFormats[i]}", nameof(renderBufferFormats));
                    }
                }
            }

            return new FrameBuffer(this, glFormats, glFilteringMode);
        }

        public Texture CreateTexture(int width, int height, bool manualMipmaps = false, TextureFilteringMode filteringMode = TextureFilteringMode.Linear, WrapMode wrapModeS = WrapMode.None,
                                     WrapMode wrapModeT = WrapMode.None, Rgba32 initialisationColour = default)
        {
            All glFilteringMode;

            switch (filteringMode)
            {
                case TextureFilteringMode.Linear:
                    glFilteringMode = All.Linear;
                    break;

                case TextureFilteringMode.Nearest:
                    glFilteringMode = All.Nearest;
                    break;

                default:
                    throw new ArgumentException($"Unsupported filtering mode: {filteringMode}", nameof(filteringMode));
            }

            return CreateTexture(new TextureGL(this, width, height, manualMipmaps, glFilteringMode, initialisationColour), wrapModeS, wrapModeT);
        }

        public Texture CreateVideoTexture(int width, int height)
            => CreateTexture(new VideoTexture(this, width, height), WrapMode.None, WrapMode.None);

        internal Texture CreateTexture(INativeTexture nativeTexture, WrapMode wrapModeS, WrapMode wrapModeT)
        {
            var tex = new Texture(nativeTexture, wrapModeS, wrapModeT);

            allTextures.Add(tex);
            TextureCreated?.Invoke(tex);

            return tex;
        }

        internal event Action<Texture>? TextureCreated;

        event Action<Texture>? IRenderer.TextureCreated
        {
            add => throw new NotImplementedException();
            remove => throw new NotImplementedException();
        }

        Texture[] IRenderer.GetAllTextures() => allTextures.ToArray();
    }
}
