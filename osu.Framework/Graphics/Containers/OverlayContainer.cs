// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using osu.Framework.Input;
using osuTK;

namespace osu.Framework.Graphics.Containers
{
    /// <summary>
    /// An element which starts hidden and can be toggled to visible.
    /// </summary>
    public abstract class OverlayContainer : VisibilityContainer
    {
        /// <summary>
        /// Whether we should block any positional input from interacting with things behind us.
        /// </summary>
        protected virtual bool BlockPositionalInput => true;

        /// <summary>
        /// Whether we should block any non-positional input from interacting with things behind us.
        /// </summary>
        protected virtual bool BlockNonPositionalInput => false;

        internal override bool BuildNonPositionalInputQueue(List<Drawable> queue, bool allowBlocking = true)
        {
            if (PropagateNonPositionalInputSubTree && HandleNonPositionalInput && BlockNonPositionalInput)
            {
                // when blocking non-positional input behind us, we still want to make sure the global handlers receive events
                // but we don't want other drawables behind us handling them.
                queue.RemoveAll(d => !(d is IHandleGlobalInput));
            }

            return base.BuildNonPositionalInputQueue(queue, allowBlocking);
        }

        internal override bool BuildPositionalInputQueue(Vector2 screenSpacePos, List<Drawable> queue)
        {
            if (PropagatePositionalInputSubTree && HandlePositionalInput && BlockPositionalInput && ReceivePositionalInputAt(screenSpacePos))
            {
                // when blocking positional input behind us, we still want to make sure the global handlers receive events
                // but we don't want other drawables behind us handling them.
                queue.RemoveAll(d => !(d is IHandleGlobalInput));
            }

            return base.BuildPositionalInputQueue(screenSpacePos, queue);
        }
    }
}
