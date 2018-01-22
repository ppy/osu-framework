// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System.Collections.Generic;
using osu.Framework.Input;

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

        protected override bool OnHover(InputState state) => BlockPassThroughMouse;

        protected override bool OnMouseDown(InputState state, MouseDownEventArgs args) => BlockPassThroughMouse;

        protected override bool OnClick(InputState state) => BlockPassThroughMouse;

        protected override bool OnDragStart(InputState state) => BlockPassThroughMouse;

        protected override bool OnWheel(InputState state) => BlockPassThroughMouse;

        internal override bool BuildKeyboardInputQueue(List<Drawable> queue)
        {
            if (CanReceiveInput && BlockPassThroughKeyboard)
            {
                // when blocking keyboard input behind us, we still want to make sure the global handlers receive events
                // but we don't want other drawables behind us handling them.
                queue.RemoveAll(d => !(d is IHandleGlobalInput));
            }

            return base.BuildKeyboardInputQueue(queue);
        }
    }

    public enum Visibility
    {
        Hidden,
        Visible
    }
}
