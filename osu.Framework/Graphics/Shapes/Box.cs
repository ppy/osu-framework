// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using osu.Framework.Graphics.OpenGL;
using osu.Framework.Graphics.OpenGL.Vertices;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Shaders;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Textures;
using osuTK.Graphics.ES30;

namespace osu.Framework.Graphics.Shapes
{
    /// <summary>
    /// A simple rectangular box. Can be colored using the <see cref="Drawable.Colour"/> property.
    /// </summary>
    public class Box : Sprite
    {
        public Box()
        {
            Texture = Texture.WhitePixel;
        }

        protected override DrawNode CreateDrawNode() => new BoxDrawNode();

        protected class BoxDrawNode : SpriteDrawNode
        {
            public override void DrawHull(Action<TexturedVertex2D> vertexAction, ref float vertexDepth)
            {
                base.DrawHull(vertexAction, ref vertexDepth);

                if (Texture?.Available != true)
                    return;

                if (DrawColourInfo.Colour.MinAlpha != 1 || DrawColourInfo.Blending.RGBEquation != BlendEquationMode.FuncAdd)
                    return;

                // Todo: This can probably be optimised
                var skeleton = ScreenSpaceDrawQuad;
                if (GLWrapper.IsMaskingActive && GLWrapper.CurrentMaskingInfo.CornerRadius != 0)
                {
                    float offset = GLWrapper.CurrentMaskingInfo.CornerRadius / 3f;

                    var baseSkeleton = (ScreenSpaceDrawQuad * DrawInfo.MatrixInverse).AABBFloat;
                    baseSkeleton = baseSkeleton.Shrink(offset);

                    skeleton = Quad.FromRectangle(baseSkeleton) * DrawInfo.Matrix;
                }

                Shader shader = TextureShader;

                shader.Bind();

                Texture.TextureGL.WrapMode = WrapTexture ? TextureWrapMode.Repeat : TextureWrapMode.ClampToEdge;

                Blit(skeleton, vertexAction);

                shader.Unbind();

                vertexDepth -= 0.0001f;
            }

            public override void Draw(Action<TexturedVertex2D> vertexAction)
            {
                if (DrawColourInfo.Colour.MinAlpha == 1 && DrawColourInfo.Blending.RGBEquation == BlendEquationMode.FuncAdd
                                                        && (!GLWrapper.IsMaskingActive || GLWrapper.CurrentMaskingInfo.CornerRadius == 0))
                {
                    return;
                }

                base.Draw(vertexAction);
            }
        }
    }
}
