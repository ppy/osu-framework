using System;
using System.Drawing;
using osuTK.Graphics;

namespace osu.Framework.OML.ValueParsers
{
    public class OmlColor4Parser : IOmlValueParser<Color4>
    {
        public Color4 Parse(string value)
        {
            return (Color4) Parse(typeof(Color4), value);
        }

        public object Parse(Type type, string value)
        {
            var colorConverter = new ColorConverter();

            if (!value.StartsWith("#"))
                return Color4.FromSrgb(Color.FromName(value));
            
            var convertedColorObj = colorConverter.ConvertFromString(value);
            return convertedColorObj != null ?
                Color4.FromSrgb((Color) convertedColorObj) :
                Color4.White;
        }
    }
}