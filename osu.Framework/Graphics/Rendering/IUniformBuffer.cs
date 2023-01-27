// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;

namespace osu.Framework.Graphics.Rendering
{
    public interface IUniformBuffer : IDisposable
    {
    }

    public interface IUniformBuffer<TData> : IUniformBuffer
        where TData : unmanaged, IEquatable<TData>
    {
        TData Data { set; }
    }
}
