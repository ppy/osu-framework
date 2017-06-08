// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
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
    internal static class VertexUtils
    {
        private static readonly Dictionary<Type, int> stride_cache = new Dictionary<Type, int>();
        private static readonly Dictionary<Type, List<VertexMemberAttribute>> member_cache = new Dictionary<Type, List<VertexMemberAttribute>>();

        private static int amountEnabledAttributes;

        /// <summary>
        /// Computes the stride of a vertex type.
        /// </summary>
        public static int Stride<T>()
            where T : IVertex
        {
            int cached;
            if (stride_cache.TryGetValue(typeof(T), out cached))
                return cached;

            return stride_cache[typeof(T)] = BlittableValueType.StrideOf(default(T));
        }

        /// <summary>
        /// Enables and binds the vertex attributes/pointers for a vertex type.
        /// </summary>
        public static void Bind<T>()
            where T : IVertex
        {
            List<VertexMemberAttribute> members;
            if (!member_cache.TryGetValue(typeof(T), out members))
            {
                members = new List<VertexMemberAttribute>();

                // Use reflection to retrieve the members attached with a VertexMemberAttribute
                foreach (FieldInfo field in typeof(T).GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Where(t => t.IsDefined(typeof(VertexMemberAttribute), true)))
                {
                    var attrib = (VertexMemberAttribute)field.GetCustomAttribute(typeof(VertexMemberAttribute));

                    // Because this is an un-seen vertex, the attribute locations are unknown, but they're needed for marshalling
                    attrib.Offset = Marshal.OffsetOf(typeof(T), field.Name);

                    members.Add(attrib);
                }

                member_cache[typeof(T)] = members;
            }

            int stride = Stride<T>();

            enableAttributes(members.Count);
            for (int i = 0; i < members.Count; i++)
                GL.VertexAttribPointer(i, members[i].Count, members[i].Type, members[i].Normalized, stride, members[i].Offset);
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