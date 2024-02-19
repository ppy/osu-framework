// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace osu.Framework.Graphics.Rendering.Deferred.Allocation
{
    internal readonly record struct ResourceReference(int Id)
    {
        public T Dereference<T>(DeferredRenderer renderer)
            => (T)renderer.Context.Dereference(this);
    }
}
