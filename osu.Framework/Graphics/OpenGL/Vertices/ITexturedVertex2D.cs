// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using OpenTK;
using OpenTK.Graphics;

namespace osu.Framework.Graphics.OpenGL.Vertices
{
    public interface ITexturedVertex2D : IVertex
    {
        Vector2 Position { set; }
        Color4 Colour { set; }
        Vector2 TexturePosition { set; }
        Vector4 TextureRect { set; }
        Vector2 BlendRange { set; }
    }
}
