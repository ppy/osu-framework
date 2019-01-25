// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using osu.Framework.Graphics.OpenGL;
using osu.Framework.Graphics.OpenGL.Vertices;
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

                if (GLWrapper.IsMaskingActive || DrawColourInfo.Colour.MinAlpha != 1 || DrawColourInfo.Blending.RGBEquation != BlendEquationMode.FuncAdd)
                    return;

                base.Draw(vertexAction);

                vertexDepth -= 0.0001f;
            }

            public override void Draw(Action<TexturedVertex2D> vertexAction)
            {
                if (!GLWrapper.IsMaskingActive && DrawColourInfo.Colour.MinAlpha == 1 && DrawColourInfo.Blending.RGBEquation == BlendEquationMode.FuncAdd)
                    return;

                base.Draw(vertexAction);
            }
        }
    }
}
