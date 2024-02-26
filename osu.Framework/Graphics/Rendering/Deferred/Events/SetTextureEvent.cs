// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics.Rendering.Deferred.Allocation;

namespace osu.Framework.Graphics.Rendering.Deferred.Events
{
    internal readonly record struct SetTextureEvent(RenderEventType Type, ResourceReference Texture, int Unit) : IRenderEvent
    {
        public static SetTextureEvent Create(DeferredRenderer renderer, INativeTexture? texture, int unit)
            => new SetTextureEvent(RenderEventType.SetTexture, renderer.Context.Reference(texture), unit);
    }
}
