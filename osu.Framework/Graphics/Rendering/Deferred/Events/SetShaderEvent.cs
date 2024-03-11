// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics.Rendering.Deferred.Allocation;
using osu.Framework.Graphics.Shaders;

namespace osu.Framework.Graphics.Rendering.Deferred.Events
{
    internal readonly record struct SetShaderEvent(RenderEventType Type, ResourceReference Shader) : IRenderEvent
    {
        public static SetShaderEvent Create(DeferredRenderer renderer, IShader shader)
            => new SetShaderEvent(RenderEventType.SetShader, renderer.Context.Reference(shader));
    }
}
