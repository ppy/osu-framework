﻿// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Runtime.InteropServices;
using osuTK;
using osuTK.Graphics;
using osuTK.Graphics.ES30;

namespace osu.Framework.Graphics.OpenGL.Vertices
{
    [StructLayout(LayoutKind.Sequential)]
    public struct TimedTexturedVertex2D : IEquatable<TimedTexturedVertex2D>, IVertex
    {
        [VertexMember(2, VertexAttribPointerType.Float)]
        public Vector2 Position;
        [VertexMember(4, VertexAttribPointerType.Float)]
        public Color4 Colour;
        [VertexMember(2, VertexAttribPointerType.Float)]
        public Vector2 TexturePosition;
        [VertexMember(1, VertexAttribPointerType.Float)]
        public float Time;

        public bool Equals(TimedTexturedVertex2D other)
        {
            return Position.Equals(other.Position) && TexturePosition.Equals(other.TexturePosition) && Colour.Equals(other.Colour) && Time.Equals(other.Time);
        }
    }
}
