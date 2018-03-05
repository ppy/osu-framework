// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu/master/LICENCE

using OpenTK;
using OpenTK.Graphics;

namespace osu.Framework.Graphics.OpenGL.Vertices
{
    public interface ITexturedVertex2D : IVertex
    {
        Vector2 Position { get; set; }
        Color4 Colour { get; set; }
        Vector2 TexturePosition { get; set; }
        Vector4 TextureRect { get; set; }
        Vector2 BlendRange { get; set; }
    }
}
