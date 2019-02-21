// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osuTK;

namespace osu.Framework.Input.InputQueue
{
    public interface IPositionalInputVisitor
    {
        bool Visit(Vector2 screenSpacePos, Drawable drawable);
        bool Visit(Vector2 screenSpacePos, CompositeDrawable compositeDrawable);
        bool Visit(Vector2 screenSpacePos, OverlayContainer overlayContainer);
        bool Visit(Vector2 screenSpacePos, InputManager inputManager);
        bool Visit(Vector2 screenSpacePos, PassThroughInputManager passThroughInputManager);
    }
}
