// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

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

        protected override bool OnKeyDown(InputState state, KeyDownEventArgs args) => BlockPassThroughKeyboard;

        protected override bool OnKeyUp(InputState state, KeyUpEventArgs args) => BlockPassThroughKeyboard;
    }

    public enum Visibility
    {
        Hidden,
        Visible
    }
}
