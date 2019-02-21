// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Input.Bindings;

namespace osu.Framework.Input.InputQueue
{
    public interface INonPositionalInputVisitor
    {
        bool Visit(Drawable drawable, bool allowBlocking = true);
        bool Visit(CompositeDrawable compositeDrawable, bool allowBlocking = true);
        bool Visit(OverlayContainer overlayContainer, bool allowBlocking = true);
        bool Visit(KeyBindingContainer keyBindingContainer, bool allowBlocking = true);
        bool Visit(InputManager inputManager, bool allowBlocking = true);
        bool Visit(PassThroughInputManager passThroughInputManager, bool allowBlocking = true);
    }
}
