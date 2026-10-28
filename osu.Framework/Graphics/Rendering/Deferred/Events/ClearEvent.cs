// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace osu.Framework.Graphics.Rendering.Deferred.Events
{
    internal readonly record struct ClearEvent(ClearInfo Info)
    {
        public static RenderEvent Create(ClearInfo info)
            => RenderEvent.Create(new ClearEvent(info));
    }
}
