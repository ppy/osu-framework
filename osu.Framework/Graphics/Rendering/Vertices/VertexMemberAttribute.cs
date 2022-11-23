// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osuTK.Graphics.ES30;

namespace osu.Framework.Graphics.Rendering.Vertices
{
    [AttributeUsage(AttributeTargets.Field)]
    public class VertexMemberAttribute : Attribute
    {
        /// <summary>
        /// The number of components of <see cref="Type"/> represented by this vertex attribute member.
        /// E.g. a <see cref="osuTK.Vector2"/> is represented by **2** <see cref="VertexAttribPointerType.Float"/> components.
        /// </summary>
        public int Count { get; private set; }

        /// <summary>
        /// The type of each component of this vertex attribute member.
        /// E.g. a <see cref="osuTK.Vector2"/> is represented by 2 **<see cref="VertexAttribPointerType.Float"/>** components.
        /// </summary>
        public VertexAttribPointerType Type { get; private set; }

        /// <summary>
        /// Whether this vertex attribute member is normalized. If this is set to true, the member will be mapped to
        /// a range of [-1, 1] (signed) or [0, 1] (unsigned) when it is passed to the shader.
        /// </summary>
        public bool Normalized { get; private set; }

        /// <summary>
        /// The offset of this attribute member in the struct. This is computed internally by the framework.
        /// </summary>
        internal IntPtr Offset;

        public VertexMemberAttribute(int count, VertexAttribPointerType type)
        {
            Count = count;
            Type = type;
            Normalized = false;
        }

        public VertexMemberAttribute(int count, VertexAttribPointerType type, bool normalized)
        {
            Count = count;
            Type = type;
            Normalized = normalized;
        }
    }
}
