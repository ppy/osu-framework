// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics.Rendering.Deferred.Allocation;
using osu.Framework.Graphics.Shaders;

namespace osu.Framework.Graphics.Rendering.Deferred.Events
{
    internal readonly record struct SetShaderEvent(ResourceReference Shader)
    {
        public static RenderEvent Create(DeferredRenderer renderer, IShader shader)
            => RenderEvent.Create(new SetShaderEvent(renderer.Context.Reference(shader)));
    }
}
