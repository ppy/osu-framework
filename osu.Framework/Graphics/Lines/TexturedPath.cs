// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Graphics.Textures;

namespace osu.Framework.Graphics.Lines
{
    public class TexturedPath : Path
    {
        public new Texture Texture
        {
            get => base.Texture;
            set => base.Texture = value;
        }
    }
}
