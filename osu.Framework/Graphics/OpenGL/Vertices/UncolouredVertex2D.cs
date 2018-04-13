// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Runtime.InteropServices;
using OpenTK;
using OpenTK.Graphics.ES30;

namespace osu.Framework.Graphics.OpenGL.Vertices
{
    [StructLayout(LayoutKind.Sequential)]
    public struct UncolouredVertex2D : IEquatable<UncolouredVertex2D>, IVertex
    {
        [VertexMember(2, VertexAttribPointerType.Float)]
        public Vector2 Position;

        public bool Equals(UncolouredVertex2D other)
        {
            return Position.Equals(other.Position);
        }
    }
}
