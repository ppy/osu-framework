// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace osu.Framework.Graphics.Rendering.Deferred.Events
{
    internal readonly record struct SetScissorStateEvent(RenderEventType Type, bool Enabled) : IRenderEvent
    {
        public static SetScissorStateEvent Create(bool enabled)
            => new SetScissorStateEvent(RenderEventType.SetScissorState, enabled);
    }
}
