// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Graphics.OpenGL.Vertices;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Textures;
using osuTK;
using osuTK.Graphics.ES30;

namespace osu.Framework.Graphics
{
    public abstract class TexturedDrawNode : TexturedShaderDrawNode
    {
        protected Texture Texture { get; private set; }
        protected Quad ScreenSpaceDrawQuad { get; private set; }

        protected RectangleF DrawRectangle { get; private set; }
        protected Vector2 InflationAmount { get; private set; }

        protected bool WrapTexture { get; private set; }

        protected new ITexturedDrawable Source => (ITexturedDrawable)base.Source;

        protected TexturedDrawNode(ITexturedDrawable source)
            : base(source)
        {
        }

        public override void ApplyState()
        {
            base.ApplyState();

            Texture = Source.Texture;
            ScreenSpaceDrawQuad = Source.ScreenSpaceDrawQuad;
            DrawRectangle = Source.DrawRectangle;
            InflationAmount = Source.InflationAmount;
            WrapTexture = Source.WrapTexture;
        }

        public override void Draw(Action<TexturedVertex2D> vertexAction)
        {
            base.Draw(vertexAction);

            if (Texture?.Available != true)
                return;

            Texture.TextureGL.WrapMode = WrapTexture ? TextureWrapMode.Repeat : TextureWrapMode.ClampToEdge;
        }

        protected override bool RequiresRoundedShader => base.RequiresRoundedShader || InflationAmount != Vector2.Zero;
    }
}
