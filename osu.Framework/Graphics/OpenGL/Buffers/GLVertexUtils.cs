// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

// ReSharper disable StaticMemberInGenericType

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using osu.Framework.Graphics.Rendering.Vertices;
using osuTK.Graphics.ES30;

namespace osu.Framework.Graphics.OpenGL.Buffers
{
    /// <summary>
    /// Helper method that provides functionality to enable and bind GL vertex attributes.
    /// </summary>
    internal static class GLVertexUtils<T>
        where T : struct, IVertex
    {
        /// <summary>
        /// The stride of the vertex of type <typeparamref name="T"/>.
        /// </summary>
        public static readonly int STRIDE = Marshal.SizeOf(default(T));

        private static readonly List<VertexMemberAttribute> attributes = new List<VertexMemberAttribute>();
        private static int amountEnabledAttributes;

        static GLVertexUtils()
        {
            addAttributesRecursive(typeof(T), 0);
        }

        private static void addAttributesRecursive(Type type, int currentOffset)
        {
            foreach (FieldInfo field in type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                int fieldOffset = currentOffset + Marshal.OffsetOf(type, field.Name).ToInt32();

                if (typeof(IVertex).IsAssignableFrom(field.FieldType))
                {
                    // Vertices may contain others, but the attributes of contained vertices belong to the parent when marshalled, so they are recursively added for their parent
                    // Their field offsets must be adjusted to reflect the position of the child attribute in the parent vertex
                    addAttributesRecursive(field.FieldType, fieldOffset);
                }
                else if (field.IsDefined(typeof(VertexMemberAttribute), true))
                {
                    var attrib = (VertexMemberAttribute)field.GetCustomAttribute(typeof(VertexMemberAttribute));
                    Debug.Assert(attrib != null);

                    // Because this is an un-seen vertex, the attribute locations are unknown, but they're needed for marshalling
                    attrib.Offset = new IntPtr(fieldOffset);

                    attributes.Add(attrib);
                }
            }
        }

        /// <summary>
        /// Enables and binds the vertex attributes/pointers for the vertex of type <typeparamref name="T"/>.
        /// </summary>
        public static void Bind()
        {
            enableAttributes(attributes.Count);
            for (int i = 0; i < attributes.Count; i++)
                GL.VertexAttribPointer(i, attributes[i].Count, attributes[i].Type, attributes[i].Normalized, STRIDE, attributes[i].Offset);
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
}
