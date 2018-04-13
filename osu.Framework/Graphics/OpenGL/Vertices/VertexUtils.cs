// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

// ReSharper disable StaticMemberInGenericType

using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using OpenTK;
using OpenTK.Graphics.ES30;

namespace osu.Framework.Graphics.OpenGL.Vertices
{
    /// <summary>
    /// Helper method that provides functionality to enable and bind vertex attributes.
    /// </summary>
    internal static class VertexUtils<T>
        where T : IVertex
    {
        /// <summary>
        /// The stride of the vertex of type <see cref="T"/>.
        /// </summary>
        public static readonly int STRIDE = BlittableValueType.StrideOf(default(T));

        private static readonly List<VertexMemberAttribute> attributes = new List<VertexMemberAttribute>();
        private static int amountEnabledAttributes;

        static VertexUtils()
        {
            // Use reflection to retrieve the members attached with a VertexMemberAttribute
            foreach (FieldInfo field in typeof(T).GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Where(t => t.IsDefined(typeof(VertexMemberAttribute), true)))
            {
                var attrib = (VertexMemberAttribute)field.GetCustomAttribute(typeof(VertexMemberAttribute));

                // Because this is an un-seen vertex, the attribute locations are unknown, but they're needed for marshalling
                attrib.Offset = Marshal.OffsetOf(typeof(T), field.Name);

                attributes.Add(attrib);
            }
        }

        /// <summary>
        /// Enables and binds the vertex attributes/pointers for the vertex of type <see cref="T"/>.
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
