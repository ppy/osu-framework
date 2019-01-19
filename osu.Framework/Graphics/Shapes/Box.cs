// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Graphics.OpenGL;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Textures;
using osuTK;
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

        private class BoxDrawNode : SpriteDrawNode
        {
            protected internal override bool SupportsFrontRenderPass => DrawColourInfo.Colour.MinAlpha == 1 && DrawColourInfo.Blending.RGBEquation == BlendEquationMode.FuncAdd && !GLWrapper.IsMaskingActive && InflationAmount == Vector2.Zero;
        }
    }
}
