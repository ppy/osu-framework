// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics.Primitives;

namespace osu.Framework.Graphics.Rendering.Deferred.Events
{
    internal readonly record struct SetViewportEvent(RectangleI Viewport)
    {
        public static RenderEvent Create(RectangleI viewport)
            => RenderEvent.Create(new SetViewportEvent(viewport));
    }
}
