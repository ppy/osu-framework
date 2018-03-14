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
    public struct TexturedVertex2D : IEquatable<TexturedVertex2D>, ITexturedVertex2D
    {
        [VertexMember(2, VertexAttribPointerType.Float)]
        private Vector2 position;
        [VertexMember(4, VertexAttribPointerType.Float)]
        private Color4 colour;
        [VertexMember(2, VertexAttribPointerType.Float)]
        private Vector2 texturePosition;
        [VertexMember(4, VertexAttribPointerType.Float)]
        private Vector4 textureRect;
        [VertexMember(2, VertexAttribPointerType.Float)]
        private Vector2 blendRange;

        public Vector2 Position { set => position = value; }
        public Color4 Colour { set => colour = value; }
        public Vector2 TexturePosition { set => texturePosition = value; }
        public Vector4 TextureRect { set => textureRect = value; }
        public Vector2 BlendRange { set => blendRange = value; }

        public bool Equals(TexturedVertex2D other)
        {
            return position.Equals(other.position)
                   && texturePosition.Equals(other.texturePosition)
                   && colour.Equals(other.colour)
                   && textureRect.Equals(other.textureRect)
                   && blendRange.Equals(other.blendRange);
        }
    }
}
