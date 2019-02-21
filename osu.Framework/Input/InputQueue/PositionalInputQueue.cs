// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osuTK;

namespace osu.Framework.Input.InputQueue
{
    public class PositionalInputQueue : List<Drawable>, IPositionalInputVisitor
    {
        public bool Visit(Vector2 screenSpacePos, Drawable drawable)
        {
            if (!drawable.PropagatePositionalInputSubTree)
                return false;

            if (drawable.HandlePositionalInput && drawable.ReceivePositionalInputAt(screenSpacePos))
                Add(drawable);

            return true;
        }

        public bool Visit(Vector2 screenSpacePos, CompositeDrawable compositeDrawable)
        {
            if (!Visit(screenSpacePos, compositeDrawable as Drawable))
                return false;

            if (compositeDrawable.Masking && !compositeDrawable.ReceivePositionalInputAt(screenSpacePos))
                return false;

            for (int i = 0; i < compositeDrawable.AliveInternalChildren.Count; ++i)
                (compositeDrawable.AliveInternalChildren[i] as IInputQueueElement).Accept(this, screenSpacePos);

            return true;
        }

        public bool Visit(Vector2 screenSpacePos, OverlayContainer overlayContainer)
        {
            if (overlayContainer.PropagatePositionalInputSubTree && overlayContainer.HandlePositionalInput && overlayContainer.BlockPositionalInput && overlayContainer.ReceivePositionalInputAt(screenSpacePos))
            {
                // when blocking positional input behind us, we still want to make sure the global handlers receive events
                // but we don't want other drawables behind us handling them.
                RemoveAll(d => !(d is IHandleGlobalInput));
            }

            return Visit(screenSpacePos, overlayContainer as CompositeDrawable);
        }

        public bool Visit(Vector2 screenSpacePos, InputManager inputManager)
        {
            return false;
        }

        public bool Visit(Vector2 screenSpacePos, PassThroughInputManager passThroughInputManager)
        {
            if (!passThroughInputManager.PropagatePositionalInputSubTree) return false;

            if (passThroughInputManager.UseParentInput)
                Add(passThroughInputManager);
            return false;
        }
    }
}
