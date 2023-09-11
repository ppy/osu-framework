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
    public struct TexturedVertex3D : IEquatable<TexturedVertex3D>, IVertex
    {
        [VertexMember(3, VertexAttribPointerType.Float)]
        public Vector3 Position;

        [VertexMember(4, VertexAttribPointerType.Float)]
        public Color4 Colour;

        [VertexMember(2, VertexAttribPointerType.Float)]
        public Vector2 TexturePosition;

        [VertexMember(1, VertexAttribPointerType.Int)]
        private readonly int maskingIndex;

        [Obsolete("Initialise this type with an IRenderer instead", true)]
        public TexturedVertex3D()
        {
            this = default; // explicitly initialise all members to default values
        }

        public TexturedVertex3D(IRenderer renderer)
        {
            this = default; // explicitly initialise all members to default values
            maskingIndex = renderer.CurrentMaskingIndex;
        }

        public readonly bool Equals(TexturedVertex3D other)
            => Position.Equals(other.Position)
               && TexturePosition.Equals(other.TexturePosition)
               && Colour.Equals(other.Colour)
               && maskingIndex == other.maskingIndex;
    }
}
