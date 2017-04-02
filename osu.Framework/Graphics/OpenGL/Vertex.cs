// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Runtime.InteropServices;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.ES30;

namespace osu.Framework.Graphics.OpenGL
{
    public static class Vertex
    {
        private static int amountEnabledAttributes;

        public static void EnableAttributes(int amount)
        {
            if (amount == amountEnabledAttributes)
                return;
            if (amount > amountEnabledAttributes)
            {
                for (int i = amountEnabledAttributes; i < amount; ++i)
                {
                    GL.EnableVertexAttribArray(i);
                }
            }
            else
            {
                for (int i = amountEnabledAttributes - 1; i >= amount; --i)
                {
                    GL.DisableVertexAttribArray(i);
                }
            }

            amountEnabledAttributes = amount;
        }
    }


    [StructLayout(LayoutKind.Sequential)]
    public struct UncolouredVertex2D : IEquatable<UncolouredVertex2D>
    {
        public Vector2 Position;

        private static readonly IntPtr positionOffset = Marshal.OffsetOf(typeof(UncolouredVertex2D), "Position");

        public bool Equals(UncolouredVertex2D other)
        {
            return Position.Equals(other.Position);
        }

        public static void Bind()
        {
            Vertex.EnableAttributes(1);

            GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, Stride, positionOffset);
        }

        public static readonly int Stride = BlittableValueType.StrideOf(new UncolouredVertex2D());
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct Vertex2D : IEquatable<Vertex2D>
    {
        public Vector2 Position;
        public Color4 Colour;

        private static readonly IntPtr positionOffset = Marshal.OffsetOf(typeof(Vertex2D), "Position");
        private static readonly IntPtr colourOffset = Marshal.OffsetOf(typeof(Vertex2D), "Colour");

        public bool Equals(Vertex2D other)
        {
            return Position.Equals(other.Position) && Colour.Equals(other.Colour);
        }

        public static void Bind()
        {
            Vertex.EnableAttributes(2);

            GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, Stride, positionOffset);
            GL.VertexAttribPointer(1, 4, VertexAttribPointerType.Float, false, Stride, colourOffset);
        }

        public static readonly int Stride = BlittableValueType.StrideOf(new Vertex2D());
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct TexturedVertex2D : IEquatable<TexturedVertex2D>
    {
        public Vector2 Position;
        public Color4 Colour;
        public Vector2 TexturePosition;
        public Vector4 TextureRect;
        public Vector2 BlendRange;

        private static readonly IntPtr positionOffset = Marshal.OffsetOf(typeof(TexturedVertex2D), "Position");
        private static readonly IntPtr colourOffset = Marshal.OffsetOf(typeof(TexturedVertex2D), "Colour");
        private static readonly IntPtr texturePositionOffset = Marshal.OffsetOf(typeof(TexturedVertex2D), "TexturePosition");
        private static readonly IntPtr textureRectOffset = Marshal.OffsetOf(typeof(TexturedVertex2D), "TextureRect");
        private static readonly IntPtr blendRangeOffset = Marshal.OffsetOf(typeof(TexturedVertex2D), "BlendRange");

        public bool Equals(TexturedVertex2D other)
        {
            return Position.Equals(other.Position)
                   && TexturePosition.Equals(other.TexturePosition)
                   && Colour.Equals(other.Colour)
                   && TextureRect.Equals(other.TextureRect)
                   && BlendRange.Equals(other.BlendRange);
        }

        public static void Bind()
        {
            Vertex.EnableAttributes(5);

            GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, Stride, positionOffset);
            GL.VertexAttribPointer(1, 4, VertexAttribPointerType.Float, false, Stride, colourOffset);
            GL.VertexAttribPointer(2, 2, VertexAttribPointerType.Float, false, Stride, texturePositionOffset);
            GL.VertexAttribPointer(3, 4, VertexAttribPointerType.Float, false, Stride, textureRectOffset);
            GL.VertexAttribPointer(4, 2, VertexAttribPointerType.Float, false, Stride, blendRangeOffset);
        }

        public static readonly int Stride = BlittableValueType.StrideOf(new TexturedVertex2D());
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct TimedTexturedVertex2D : IEquatable<TimedTexturedVertex2D>
    {
        public Vector2 Position;
        public Color4 Colour;
        public Vector2 TexturePosition;
        public float Time;

        private static readonly IntPtr positionOffset = Marshal.OffsetOf(typeof(TimedTexturedVertex2D), "Position");
        private static readonly IntPtr colourOffset = Marshal.OffsetOf(typeof(TimedTexturedVertex2D), "Colour");
        private static readonly IntPtr texturePositionOffset = Marshal.OffsetOf(typeof(TimedTexturedVertex2D), "TexturePosition");
        private static readonly IntPtr timeOffset = Marshal.OffsetOf(typeof(TimedTexturedVertex2D), "Time");

        public bool Equals(TimedTexturedVertex2D other)
        {
            return Position.Equals(other.Position) && TexturePosition.Equals(other.TexturePosition) && Colour.Equals(other.Colour) && Time.Equals(other.Time);
        }

        public static void Bind()
        {
            Vertex.EnableAttributes(4);

            GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, Stride, positionOffset);
            GL.VertexAttribPointer(1, 4, VertexAttribPointerType.Float, false, Stride, colourOffset);
            GL.VertexAttribPointer(2, 2, VertexAttribPointerType.Float, false, Stride, texturePositionOffset);
            GL.VertexAttribPointer(3, 1, VertexAttribPointerType.Float, false, Stride, timeOffset);
        }

        public static readonly int Stride = BlittableValueType.StrideOf(new TimedTexturedVertex2D());
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct ParticleVertex2D : IEquatable<ParticleVertex2D>
    {
        public Vector2 Position;
        public Color4 Colour;
        public Vector2 TexturePosition;
        public float Time;
        public Vector2 Direction;

        private static readonly IntPtr positionOffset = Marshal.OffsetOf(typeof(ParticleVertex2D), "Position");
        private static readonly IntPtr colourOffset = Marshal.OffsetOf(typeof(ParticleVertex2D), "Colour");
        private static readonly IntPtr texturePositionOffset = Marshal.OffsetOf(typeof(ParticleVertex2D), "TexturePosition");
        private static readonly IntPtr timeOffset = Marshal.OffsetOf(typeof(ParticleVertex2D), "Time");
        private static readonly IntPtr directionOffset = Marshal.OffsetOf(typeof(ParticleVertex2D), "Direction");

        public bool Equals(ParticleVertex2D other)
        {
            return Position.Equals(other.Position) && TexturePosition.Equals(other.TexturePosition) && Colour.Equals(other.Colour) && Time.Equals(other.Time) && Direction.Equals(other.Direction);
        }

        public static void Bind()
        {
            Vertex.EnableAttributes(5);

            GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, Stride, positionOffset);
            GL.VertexAttribPointer(1, 4, VertexAttribPointerType.Float, false, Stride, colourOffset);
            GL.VertexAttribPointer(2, 2, VertexAttribPointerType.Float, false, Stride, texturePositionOffset);
            GL.VertexAttribPointer(3, 1, VertexAttribPointerType.Float, false, Stride, timeOffset);
            GL.VertexAttribPointer(4, 2, VertexAttribPointerType.Float, false, Stride, directionOffset);
        }

        public static readonly int Stride = BlittableValueType.StrideOf(new ParticleVertex2D());
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct TexturedVertex3D : IEquatable<TexturedVertex3D>
    {
        public Vector3 Position;
        public Color4 Colour;
        public Vector2 TexturePosition;

        private static readonly IntPtr positionOffset = Marshal.OffsetOf(typeof(TexturedVertex3D), "Position");
        private static readonly IntPtr colourOffset = Marshal.OffsetOf(typeof(TexturedVertex3D), "Colour");
        private static readonly IntPtr texturePositionOffset = Marshal.OffsetOf(typeof(TexturedVertex3D), "TexturePosition");

        public bool Equals(TexturedVertex3D other)
        {
            return Position.Equals(other.Position) && TexturePosition.Equals(other.TexturePosition) && Colour.Equals(other.Colour);
        }

        public static void Bind()
        {
            Vertex.EnableAttributes(3);

            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, Stride, positionOffset);
            GL.VertexAttribPointer(1, 4, VertexAttribPointerType.Float, false, Stride, colourOffset);
            GL.VertexAttribPointer(2, 2, VertexAttribPointerType.Float, false, Stride, texturePositionOffset);
        }

        public static readonly int Stride = BlittableValueType.StrideOf(new TexturedVertex3D());
    }
}
