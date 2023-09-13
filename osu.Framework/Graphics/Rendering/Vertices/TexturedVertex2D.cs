// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Runtime.InteropServices;
using osuTK;
using osuTK.Graphics;
using osuTK.Graphics.ES30;

namespace osu.Framework.Graphics.Rendering.Vertices
{
    [StructLayout(LayoutKind.Sequential)]
    public struct TexturedVertex2D : IEquatable<TexturedVertex2D>, IVertex
    {
        [VertexMember(2, VertexAttribPointerType.Float)]
        public Vector2 Position;

        [VertexMember(4, VertexAttribPointerType.Float)]
        public Color4 Colour;

        [VertexMember(2, VertexAttribPointerType.Float)]
        public Vector2 TexturePosition;

        [VertexMember(4, VertexAttribPointerType.Float)]
        public Vector4 TextureRect;

        [VertexMember(2, VertexAttribPointerType.Float)]
        public Vector2 BlendRange;

        [VertexMember(1, VertexAttribPointerType.Float)]
        private readonly float backbufferDrawDepth;

        [Obsolete("Initialise this type with an IRenderer instead", true)]
        public TexturedVertex2D()
        {
            this = default; // explicitly initialise all members to default values
        }

        public TexturedVertex2D(IRenderer renderer)
        {
            this = default; // explicitly initialise all members to default values
            backbufferDrawDepth = renderer.BackbufferDrawDepth;
        }

        public readonly bool Equals(TexturedVertex2D other) =>
            Position.Equals(other.Position)
            && TexturePosition.Equals(other.TexturePosition)
            && Colour.Equals(other.Colour)
            && TextureRect.Equals(other.TextureRect)
            && BlendRange.Equals(other.BlendRange)
            && backbufferDrawDepth == other.backbufferDrawDepth;
    }
}
