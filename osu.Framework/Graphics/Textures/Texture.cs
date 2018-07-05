// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using osu.Framework.Graphics.OpenGL.Textures;
using osu.Framework.Graphics.Primitives;
using OpenTK;
using OpenTK.Graphics.ES30;
using osu.Framework.Graphics.Colour;
using osu.Framework.Graphics.OpenGL.Vertices;
using RectangleF = osu.Framework.Graphics.Primitives.RectangleF;

namespace osu.Framework.Graphics.Textures
{
    public class Texture : IDisposable
    {
        private static TextureWhitePixel whitePixel;

        public static Texture WhitePixel
        {
            get
            {
                if (whitePixel == null)
                {
                    TextureAtlas atlas = new TextureAtlas(3, 3, true);
                    whitePixel = atlas.GetWhitePixel();
                    whitePixel.SetData(new TextureUpload(new byte[] { 255, 255, 255, 255 }));
                }

                return whitePixel;
            }
        }

        public TextureGL TextureGL;
        public string Filename;
        public string AssetName;

        /// <summary>
        /// At what multiple of our expected resolution is our underlying texture?
        /// </summary>
        public float ScaleAdjust = 1;

        public bool Disposable = true;
        public bool IsDisposed { get; private set; }

        public float DisplayWidth => Width / ScaleAdjust;
        public float DisplayHeight => Height / ScaleAdjust;

        public Texture(TextureGL textureGl) => TextureGL = textureGl ?? throw new ArgumentNullException(nameof(textureGl));

        public Texture(int width, int height, bool manualMipmaps = false, All filteringMode = All.Linear)
            : this(new TextureGLSingle(width, height, manualMipmaps, filteringMode))
        {
        }

        #region Disposal

        ~Texture()
        {
            Dispose(false);
        }


        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool isDisposing)
        {
            if (IsDisposed)
                return;
            IsDisposed = true;
        }

        #endregion

        public int Width
        {
            get => TextureGL.Width;
            set => TextureGL.Width = value;
        }

        public int Height
        {
            get => TextureGL.Height;
            set => TextureGL.Height = value;
        }

        public Vector2 Size => new Vector2(Width, Height);

        public void SetData(TextureUpload data)
        {
            TextureGL?.SetData(data);
        }

        protected virtual RectangleF TextureBounds(RectangleF? textureRect = null)
        {
            RectangleF texRect = textureRect ?? new RectangleF(0, 0, DisplayWidth, DisplayHeight);

            if (ScaleAdjust != 1)
            {
                texRect.Width *= ScaleAdjust;
                texRect.Height *= ScaleAdjust;
                texRect.X *= ScaleAdjust;
                texRect.Y *= ScaleAdjust;
            }

            return texRect;
        }

        public RectangleF GetTextureRect(RectangleF? textureRect = null)
        {
            return TextureGL.GetTextureRect(TextureBounds(textureRect));
        }

        public void DrawTriangle(Triangle vertexTriangle, ColourInfo colour, RectangleF? textureRect = null, Action<TexturedVertex2D> vertexAction = null, Vector2? inflationPercentage = null)
        {
            if (TextureGL == null || !TextureGL.Bind()) return;

            TextureGL.DrawTriangle(vertexTriangle, TextureBounds(textureRect), colour, vertexAction, inflationPercentage);
        }

        public void DrawQuad(Quad vertexQuad, ColourInfo colour, RectangleF? textureRect = null, Action<TexturedVertex2D> vertexAction = null, Vector2? inflationPercentage = null, Vector2? blendRangeOverride = null)
        {
            if (TextureGL == null || !TextureGL.Bind()) return;

            TextureGL.DrawQuad(vertexQuad, TextureBounds(textureRect), colour, vertexAction, inflationPercentage, blendRangeOverride);
        }

        public override string ToString() => $@"{AssetName} ({Width}, {Height})";
    }
}
