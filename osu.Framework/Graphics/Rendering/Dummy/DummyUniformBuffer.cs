// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;

namespace osu.Framework.Graphics.Rendering.Dummy
{
    internal class DummyUniformBuffer<TData> : IUniformBuffer<TData>
        where TData : unmanaged, IEquatable<TData>
    {
        public TData Data { get; set; }

        public void Dispose()
        {
        }
    }
}
