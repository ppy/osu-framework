// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osuTK;
using System;
using osu.Framework.Graphics.OpenGL.Vertices;

namespace osu.Framework.Graphics.Sprites
{
    /// <summary>
    /// Draw node containing all necessary information to draw a <see cref="Sprite"/>.
    /// </summary>
    public class SpriteDrawNode : TexturedDrawNode
    {
        public SpriteDrawNode(Sprite source)
            : base(source)
        {
        }

        protected virtual void Blit(Action<TexturedVertex2D> vertexAction)
        {
            Texture.DrawQuad(ScreenSpaceDrawQuad, DrawColourInfo.Colour, null, vertexAction,
                new Vector2(InflationAmount.X / DrawRectangle.Width, InflationAmount.Y / DrawRectangle.Height));
        }

        public override void Draw(Action<TexturedVertex2D> vertexAction)
        {
            base.Draw(vertexAction);

            if (Texture?.Available != true)
                return;

            Shader.Bind();

            Blit(vertexAction);

            Shader.Unbind();
        }
    }
}
