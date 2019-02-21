// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Input.Bindings;

namespace osu.Framework.Input.InputQueue
{
    public class NonPositionalInputQueue : List<Drawable>, INonPositionalInputVisitor
    {
        public bool Visit(Drawable drawable, bool allowBlocking = true)
        {
            if (!drawable.PropagateNonPositionalInputSubTree)
                return false;

            if (drawable.HandleNonPositionalInput)
                Add(drawable);

            return true;
        }

        public bool Visit(CompositeDrawable compositeDrawable, bool allowBlocking = true)
        {
            if (!Visit(compositeDrawable as Drawable, allowBlocking))
                return false;

            for (int i = 0; i < compositeDrawable.AliveInternalChildren.Count; ++i)
                (compositeDrawable.AliveInternalChildren[i] as IInputQueueElement).Accept(this, allowBlocking);

            return true;
        }

        public bool Visit(OverlayContainer overlayContainer, bool allowBlocking = true)
        {
            if (overlayContainer.PropagateNonPositionalInputSubTree && overlayContainer.HandleNonPositionalInput && overlayContainer.BlockNonPositionalInput)
            {
                // when blocking non-positional input behind us, we still want to make sure the global handlers receive events
                // but we don't want other drawables behind us handling them.
                RemoveAll(d => !(d is IHandleGlobalInput));
            }

            return Visit(overlayContainer as CompositeDrawable, allowBlocking);
        }

        public bool Visit(KeyBindingContainer keyBindingContainer, bool allowBlocking = true)
        {
            if (!Visit(keyBindingContainer as CompositeDrawable, allowBlocking))
                return false;

            if (keyBindingContainer.Prioritised)
            {
                Remove(keyBindingContainer);
                Add(keyBindingContainer);
            }

            return true;
        }

        public bool Visit(InputManager inputManager, bool allowBlocking = true)
        {
            if (!allowBlocking)
                Visit(inputManager as CompositeDrawable, false);

            return false;
        }

        public bool Visit(PassThroughInputManager passThroughInputManager, bool allowBlocking = true)
        {
            if (!passThroughInputManager.PropagateNonPositionalInputSubTree) return false;

            if (!allowBlocking)
            {
                Visit(passThroughInputManager as InputManager, false);
                return false;
            }

            if (passThroughInputManager.UseParentInput)
                Add(passThroughInputManager);
            return false;
        }
    }
}
