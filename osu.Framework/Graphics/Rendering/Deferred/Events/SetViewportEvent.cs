// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics.Primitives;

namespace osu.Framework.Graphics.Rendering.Deferred.Events
{
    internal readonly record struct SetViewportEvent(RenderEventType Type, RectangleI Viewport) : IRenderEvent
    {
        public static SetViewportEvent Create(RectangleI viewport)
            => new SetViewportEvent(RenderEventType.SetViewport, viewport);
    }
}
