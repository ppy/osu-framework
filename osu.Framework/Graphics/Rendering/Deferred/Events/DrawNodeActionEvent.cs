// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics.Rendering.Deferred.Allocation;

namespace osu.Framework.Graphics.Rendering.Deferred.Events
{
    internal readonly record struct DrawNodeActionEvent(RenderEventType Type, ResourceReference DrawNode, DrawNodeActionType Action) : IRenderEvent
    {
        public static DrawNodeActionEvent Create(DeferredRenderer renderer, DrawNode drawNode, DrawNodeActionType action)
            => new DrawNodeActionEvent(RenderEventType.DrawNodeAction, renderer.Context.Reference(drawNode), action);
    }

    internal enum DrawNodeActionType : byte
    {
        Enter,
        Exit
    }
}
