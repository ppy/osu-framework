// Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Shaders;
using osu.Framework.Graphics.Textures;
using OpenTK.Graphics.ES30;
using osu.Framework.Graphics.Batches;
using osu.Framework.Graphics.OpenGL;
using OpenTK;
using System;

namespace osu.Framework.Graphics.Sprites
{
    public class SpriteDrawNode : DrawNode
    {
        public Texture Texture;
        public Quad ScreenSpaceDrawQuad;
        public RectangleF DrawRectangle;
        public float InflationAmount;
        public bool WrapTexture;

        public Shader TextureShader;
        public Shader RoundedTextureShader;

        public override void Draw(IVertexBatch vertexBatch)
        {
            base.Draw(vertexBatch);

            if (Texture == null || Texture.IsDisposed)
                return;

            Shader shader = GLWrapper.IsMaskingActive ? RoundedTextureShader : TextureShader;

            if (InflationAmount != 0)
            {
                shader.GetUniform<Vector4>(@"g_DrawingRect").Value = new Vector4(
                    DrawRectangle.Left,
                    DrawRectangle.Top,
                    DrawRectangle.Right,
                    DrawRectangle.Bottom);

                shader.GetUniform<Matrix3>(@"g_ToDrawingSpace").Value = DrawInfo.MatrixInverse;
                shader.GetUniform<float>(@"g_DrawingBlendRange").Value = InflationAmount;
            }

            shader.Bind();

            Texture.TextureGL.WrapMode = WrapTexture ? TextureWrapMode.Repeat : TextureWrapMode.ClampToEdge;
            Texture.Draw(ScreenSpaceDrawQuad, DrawInfo.Colour, null, vertexBatch as VertexBatch<TexturedVertex2D>,
                new Vector2(InflationAmount / DrawRectangle.Width, InflationAmount / DrawRectangle.Height));

            shader.Unbind();

            if (InflationAmount != 0)
                shader.GetUniform<float>(@"g_DrawingBlendRange").Value = 0f;
        }
    }
}
