// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics.Primitives;

namespace osu.Framework.Graphics.Rendering.Deferred.Events
{
    internal readonly record struct SetScissorEvent(RectangleI Scissor)
    {
        public static RenderEvent Create(RectangleI scissor)
            => RenderEvent.Create(new SetScissorEvent(scissor));
    }
}
