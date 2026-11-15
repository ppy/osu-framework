// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace osu.Framework.Graphics.Rendering.Deferred.Events
{
    internal readonly record struct SetScissorStateEvent(bool Enabled)
    {
        public static RenderEvent Create(bool enabled)
            => RenderEvent.Create(new SetScissorStateEvent(enabled));
    }
}
