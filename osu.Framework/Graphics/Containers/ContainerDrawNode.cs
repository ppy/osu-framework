// Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System.Collections.Generic;
using osu.Framework.Graphics.OpenGL;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Shaders;
using osu.Framework.Graphics.Batches;
using OpenTK.Graphics;
using OpenTK.Graphics.ES20;
using osu.Framework.Extensions.MatrixExtensions;
using OpenTK;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Textures;

namespace osu.Framework.Graphics.Containers
{
    public class ContainerDrawNode : ShadedDrawNode
    {
        public List<DrawNode> Children;
        public MaskingInfo? MaskingInfo;
        public Quad? ScreenSpaceMaskingQuad = null;
        public float GlowRadius;
        public Color4 GlowColour;

        private void drawGlow()
        {
            if (MaskingInfo == null || GlowRadius <= 0.0f || GlowColour.A <= 0.0f)
                return;

            RectangleF glowRect = MaskingInfo.Value.MaskingRect;

            glowRect.Inflate(GlowRadius, GlowRadius);
            if (!ScreenSpaceMaskingQuad.HasValue)
                ScreenSpaceMaskingQuad = new Quad(glowRect.X, glowRect.Y, glowRect.Width, glowRect.Height) * DrawInfo.Matrix;

            Shader.GetUniform<Vector4>(@"g_MaskingRect").Value = new Vector4(
                glowRect.Left,
                glowRect.Top,
                glowRect.Right,
                glowRect.Bottom);

            Shader.GetUniform<Matrix3>(@"g_ToMaskingSpace").Value = DrawInfo.MatrixInverse;
            Shader.GetUniform<float>(@"g_CornerRadius").Value = MaskingInfo.Value.CornerRadius + GlowRadius;

            Shader.GetUniform<float>(@"g_BorderThickness").Value = 0;

            // Here we are generating the glow gradient by setting the blend range to the glow radius.
            // Note, that unlike for masking, we are using the blend range for a visual phenomenon here,
            // and not to achieve correct sampling of the border.
            Shader.GetUniform<float>(@"g_LinearBlendRange").Value = GlowRadius;

            GLWrapper.SetBlend(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.One);

            Shader.Bind();

            Color4 glowColour = GlowColour;
            glowColour.A *= DrawInfo.Colour.A;

            Texture.WhitePixel.Draw(ScreenSpaceMaskingQuad.Value, glowColour);

            Shader.Unbind();
        }

        protected override void Draw()
        {
            drawGlow();

            if (MaskingInfo != null)
                GLWrapper.PushScissor(MaskingInfo.Value);

            base.Draw();

            if (Children != null)
                foreach (DrawNode child in Children)
                    child.DrawSubTree();

            if (MaskingInfo != null)
                GLWrapper.PopScissor();
        }
    }
}
