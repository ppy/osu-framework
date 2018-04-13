// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System.Collections.Generic;
using osu.Framework.Input;
using OpenTK;

namespace osu.Framework.Graphics.Containers
{
    /// <summary>
    /// An element which starts hidden and can be toggled to visible.
    /// </summary>
    public abstract class OverlayContainer : VisibilityContainer
    {
        /// <summary>
        /// Whether we should block any mouse input from interacting with things behind us.
        /// </summary>
        protected virtual bool BlockPassThroughMouse => true;

        /// <summary>
        /// Whether we should block any keyboard input from interacting with things behind us.
        /// </summary>
        protected virtual bool BlockPassThroughKeyboard => false;

        internal override bool BuildKeyboardInputQueue(List<Drawable> queue)
        {
            if (CanReceiveKeyboardInput && BlockPassThroughKeyboard)
            {
                // when blocking keyboard input behind us, we still want to make sure the global handlers receive events
                // but we don't want other drawables behind us handling them.
                queue.RemoveAll(d => !(d is IHandleGlobalInput));
            }

            return base.BuildKeyboardInputQueue(queue);
        }

        internal override bool BuildMouseInputQueue(Vector2 screenSpaceMousePos, List<Drawable> queue)
        {
            if (CanReceiveMouseInput && BlockPassThroughMouse && ReceiveMouseInputAt(screenSpaceMousePos))
            {
                // when blocking mouse input behind us, we still want to make sure the global handlers receive events
                // but we don't want other drawables behind us handling them.
                queue.RemoveAll(d => !(d is IHandleGlobalInput));
            }

            return base.BuildMouseInputQueue(screenSpaceMousePos, queue);
        }
    }
}
