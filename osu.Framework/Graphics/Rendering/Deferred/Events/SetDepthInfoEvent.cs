// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace osu.Framework.Graphics.Rendering.Deferred.Events
{
    internal readonly record struct SetDepthInfoEvent(DepthInfo Info) : IRenderEvent
    {
        public RenderEventType Type => RenderEventType.SetDepthInfo;
    }
}
