// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using osu.Framework.Input;
using osu.Framework.Input.Events;
using osuTK.Input;

namespace osu.Framework.Graphics.Containers
{
    /// <summary>
    /// A container that handles tabbing to and from elements contained in it.
    /// </summary>
    public class TabbableContentContainer : TabbableContentContainer<Drawable>
    {
    }

    /// <summary>
    /// An interface that allows an element to be tabbed-to if its contained inside a <see cref="TabbableContentContainer"/>
    /// </summary>
    internal interface ITabTarget
    {
        /// <summary>
        /// Whether this element can be tabbed to.
        /// </summary>
        bool CanBeTabbedTo { get; }
    }

    public class TabbableContentContainer<T> : Container<T>
        where T : Drawable
    {
        private InputManager inputManager;

        protected override void LoadComplete()
        {
            inputManager = GetContainingInputManager();
            base.LoadComplete();
        }

        protected override bool OnKeyDown(KeyDownEvent e)
        {
            if (e.Key != Key.Tab)
                return false;

            var nextTab = nextTabStop(e.ShiftPressed);

            if (nextTab != null)
                inputManager.ChangeFocus(nextTab);

            return nextTab != null;
        }

        /// <summary>
        /// Perform a depth-first search for the next drawable that is able to be tabbed to.
        /// </summary>
        /// <param name="reverse"> The direction to perform the search. </param>
        /// <returns> The next tab target. </returns>
        private Drawable nextTabStop(bool reverse)
        {
            Stack<Drawable> stack = new Stack<Drawable>();
            stack.Push(this); // Extra push for circular tabbing
            stack.Push(this);

            // This gets set to true when the current focused drawable has been traversed by the search
            // Once found, the next traversed drawable that can be tabbed to will be selected.
            bool focusedDrawableFound = false;

            while (stack.Count > 0)
            {
                var drawable = stack.Pop();

                if (!focusedDrawableFound)
                    focusedDrawableFound = drawable.HasFocus;
                else if (drawable != this && drawable is ITabTarget tabbable && tabbable.CanBeTabbedTo)
                    return drawable;

                if (drawable is CompositeDrawable composite)
                {
                    var newChildren = composite.InternalChildren.ToList();
                    int bound = reverse ? newChildren.Count : 0;

                    if (!focusedDrawableFound)
                    {
                        // Find tbe current focused drawable to use as a pivot to find the next target
                        int index = newChildren.IndexOf(inputManager.FocusedDrawable);

                        // Limit the search to the direction which contains the next target
                        if (index != -1)
                            bound = reverse ? index + 1 : index;
                    }

                    if (reverse)
                    {
                        for (int i = 0; i < bound; i++)
                            stack.Push(newChildren[i]);
                    }
                    else
                    {
                        for (int i = newChildren.Count - 1; i >= bound; i--)
                            stack.Push(newChildren[i]);
                    }
                }
            }

            return null;
        }
    }
}
