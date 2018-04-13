// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Runtime.InteropServices;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.ES30;

namespace osu.Framework.Graphics.OpenGL.Vertices
{
    [StructLayout(LayoutKind.Sequential)]
    public struct TexturedVertex3D : IEquatable<TexturedVertex3D>, IVertex
    {
        [VertexMember(3, VertexAttribPointerType.Float)]
        public Vector3 Position;
        [VertexMember(4, VertexAttribPointerType.Float)]
        public Color4 Colour;
        [VertexMember(2, VertexAttribPointerType.Float)]
        public Vector2 TexturePosition;

        public bool Equals(TexturedVertex3D other)
        {
            return Position.Equals(other.Position) && TexturePosition.Equals(other.TexturePosition) && Colour.Equals(other.Colour);
        }
    }
}
