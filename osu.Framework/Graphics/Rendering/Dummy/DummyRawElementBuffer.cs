// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;

namespace osu.Framework.Graphics.Rendering.Dummy
{
    internal class DummyRawElementBuffer<TIndex> : IRawElementBuffer<TIndex> where TIndex : unmanaged, IConvertible
    {
        public void Draw (PrimitiveTopology topology, int count, int offset = 0)
        {
        }

        public void BufferData (ReadOnlySpan<TIndex> data, BufferUsageHint usageHint)
        {
        }

        public void UpdateRange (ReadOnlySpan<TIndex> data, int offset = 0)
        {
        }

        public void Bind ()
        {
        }

        public void Unbind ()
        {
        }

        public void Dispose ()
        {
        }
    }
}
