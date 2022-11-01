// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Diagnostics.CodeAnalysis;
using osu.Framework.Allocation;

namespace osu.Framework.Graphics.Rendering
{
    public interface IRendererQuery
    {
        ValueInvokeOnDisposal<IRendererQuery> Begin();

        void Reset();

        bool TryGetResult([NotNullWhen(true)] out int? result);
    }
}
