// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.ES30;

namespace osu.Framework.Graphics.OpenGL
{
    public interface IVertex
    {
    }

    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class VertexMemberAttribute : Attribute
    {
        public int Size { get; private set; }
        public VertexAttribPointerType Type { get; private set; }
        public bool Normalized { get; private set; }

        public IntPtr Offset;

        public VertexMemberAttribute(int size, VertexAttribPointerType type)
        {
            Size = size;
            Type = type;
            Normalized = false;
        }

        public VertexMemberAttribute(int size, VertexAttribPointerType type, bool normalized)
        {
            Size = size;
            Type = type;
            Normalized = normalized;
        }
    }

    public static class VertexUtils
    {
        private static Dictionary<Type, int> strideCache = new Dictionary<Type, int>();
        private static Dictionary<Type, List<VertexMemberAttribute>> memberCache = new Dictionary<Type, List<VertexMemberAttribute>>();

        private static int amountEnabledAttributes;

        public static int Stride<T>()
            where T : IVertex
        {
            int cached;
            if (strideCache.TryGetValue(typeof(T), out cached))
                return cached;

            return cached = strideCache[typeof(T)] = BlittableValueType.StrideOf(default(T));
        }

        public static void Bind<T>()
            where T : IVertex
        {
            List<VertexMemberAttribute> members;
            if (!memberCache.TryGetValue(typeof(T), out members))
            {
                members = new List<VertexMemberAttribute>();

                foreach (FieldInfo field in typeof(T).GetFields(BindingFlags.Instance).Where(t => t.IsDefined(typeof(VertexMemberAttribute), true)))
                {
                    VertexMemberAttribute attrib = (VertexMemberAttribute)field.GetCustomAttribute(typeof(VertexMemberAttribute));
                    attrib.Offset = Marshal.OffsetOf(typeof(T), field.Name);

                    members.Add(attrib);
                }

                memberCache[typeof(T)] = members;
            }

            int stride = Stride<T>();

            enableAttributes(members.Count);
            for (int i = 0; i < members.Count; i++)
                GL.VertexAttribPointer(i, members[i].Size, members[i].Type, members[i].Normalized, stride, members[i].Offset);
        }

        private static void enableAttributes(int amount)
        {
            if (amount == amountEnabledAttributes)
                return;
            if (amount > amountEnabledAttributes)
            {
                for (int i = amountEnabledAttributes; i < amount; ++i)
                    GL.EnableVertexAttribArray(i);
            }
            else
            {
                for (int i = amountEnabledAttributes - 1; i >= amount; --i)
                    GL.DisableVertexAttribArray(i);
            }

            amountEnabledAttributes = amount;
        }
    }

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

    [StructLayout(LayoutKind.Sequential)]
    public struct Vertex2D : IEquatable<Vertex2D>, IVertex
    {
        [VertexMember(2, VertexAttribPointerType.Float)]
        public Vector2 Position;
        [VertexMember(4, VertexAttribPointerType.Float)]
        public Color4 Colour;

        public bool Equals(Vertex2D other)
        {
            return Position.Equals(other.Position) && Colour.Equals(other.Colour);
        }
    }

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

        public bool Equals(TexturedVertex2D other)
        {
            return Position.Equals(other.Position)
                   && TexturePosition.Equals(other.TexturePosition)
                   && Colour.Equals(other.Colour)
                   && TextureRect.Equals(other.TextureRect)
                   && BlendRange.Equals(other.BlendRange);
        }
    }

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

    [StructLayout(LayoutKind.Sequential)]
    public struct ParticleVertex2D : IEquatable<ParticleVertex2D>, IVertex
    {
        [VertexMember(2, VertexAttribPointerType.Float)]
        public Vector2 Position;
        [VertexMember(4, VertexAttribPointerType.Float)]
        public Color4 Colour;
        [VertexMember(2, VertexAttribPointerType.Float)]
        public Vector2 TexturePosition;
        [VertexMember(1, VertexAttribPointerType.Float)]
        public float Time;
        [VertexMember(2, VertexAttribPointerType.Float)]
        public Vector2 Direction;

        public bool Equals(ParticleVertex2D other)
        {
            return Position.Equals(other.Position) && TexturePosition.Equals(other.TexturePosition) && Colour.Equals(other.Colour) && Time.Equals(other.Time) && Direction.Equals(other.Direction);
        }
    }

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
