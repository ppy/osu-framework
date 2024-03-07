// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace osu.Framework.Graphics.Rendering.Deferred.Events
{
    internal readonly record struct SetBlendMaskEvent(BlendingMask Mask)
    {
        public static RenderEvent Create(BlendingMask mask)
            => new RenderEvent(new SetBlendMaskEvent(mask));
    }
}
