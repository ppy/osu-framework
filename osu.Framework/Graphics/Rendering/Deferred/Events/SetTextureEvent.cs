// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics.Rendering.Deferred.Allocation;

namespace osu.Framework.Graphics.Rendering.Deferred.Events
{
    internal readonly record struct SetTextureEvent(ResourceReference Texture, int Unit)
    {
        public static RenderEvent Create(DeferredRenderer renderer, INativeTexture? texture, int unit)
            => RenderEvent.Create(new SetTextureEvent(renderer.Context.Reference(texture), unit));
    }
}
