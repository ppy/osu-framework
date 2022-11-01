// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Diagnostics.CodeAnalysis;
using osu.Framework.Allocation;

namespace osu.Framework.Graphics.Rendering.Dummy
{
    public class DummyRendererQuery : IRendererQuery
    {
        public ValueInvokeOnDisposal<IRendererQuery> Begin() => new ValueInvokeOnDisposal<IRendererQuery>(this, q => { });

        public void Reset()
        {
        }

        public bool TryGetResult([NotNullWhen(true)] out int? result)
        {
            result = 0;
            return true;
        }
    }
}
