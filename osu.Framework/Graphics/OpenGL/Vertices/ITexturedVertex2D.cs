// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System.Runtime.CompilerServices;
using OpenTK;
using OpenTK.Graphics;

namespace osu.Framework.Graphics.OpenGL.Vertices
{
    public interface ITexturedVertex2D : IVertex
    {
        Vector2 Position { [MethodImpl(MethodImplOptions.AggressiveInlining)] set; }
        Color4 Colour { [MethodImpl(MethodImplOptions.AggressiveInlining)] set; }
        Vector2 TexturePosition { [MethodImpl(MethodImplOptions.AggressiveInlining)] set; }
        Vector4 TextureRect { [MethodImpl(MethodImplOptions.AggressiveInlining)] set; }
        Vector2 BlendRange { [MethodImpl(MethodImplOptions.AggressiveInlining)] set; }
    }
}
