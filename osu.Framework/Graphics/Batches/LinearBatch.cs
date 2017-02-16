﻿// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using osu.Framework.Graphics.OpenGL.Buffers;
using OpenTK.Graphics.ES30;

namespace osu.Framework.Graphics.Batches
{
    public class LinearBatch<T> : VertexBatch<T> where T : struct, IEquatable<T>
    {
        private readonly PrimitiveType type;

        public LinearBatch(int size, int maxBuffers, PrimitiveType type)
            : base(size, maxBuffers)
        {
            this.type = type;
        }

        protected override VertexBuffer<T> CreateVertexBuffer() => new LinearVertexBuffer<T>(Size, type, BufferUsageHint.DynamicDraw);
    }
}
