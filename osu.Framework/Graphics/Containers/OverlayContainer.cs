// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Input;

namespace osu.Framework.Graphics.Containers
{
    /// <summary>
    /// An element which starts hidden and can be toggled to visible.
    /// </summary>
    public abstract class OverlayContainer : VisibilityContainer, IHandleOnHover, IHandleOnMouseDown, IHandleOnClick, IHandleOnDragStart, IHandleOnWheel, IHandleOnKeyDown, IHandleOnKeyUp
    {
        /// <summary>
        /// Whether we should block any mouse input from interacting with things behind us.
        /// </summary>
        protected virtual bool BlockPassThroughMouse => true;

        /// <summary>
        /// Whether we should block any keyboard input from interacting with things behind us.
        /// </summary>
        protected virtual bool BlockPassThroughKeyboard => false;

        public virtual bool OnHover(InputState state) => BlockPassThroughMouse;

        public virtual bool OnMouseDown(InputState state, MouseDownEventArgs args) => BlockPassThroughMouse;

        public virtual bool OnClick(InputState state) => BlockPassThroughMouse;

        public virtual bool OnDragStart(InputState state) => BlockPassThroughMouse;

        public virtual bool OnWheel(InputState state) => BlockPassThroughMouse;

        public virtual bool OnKeyDown(InputState state, KeyDownEventArgs args) => BlockPassThroughKeyboard;

        public virtual bool OnKeyUp(InputState state, KeyUpEventArgs args) => BlockPassThroughKeyboard;
    }

    public enum Visibility
    {
        Hidden,
        Visible
    }
}
