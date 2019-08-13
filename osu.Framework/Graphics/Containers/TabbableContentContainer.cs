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

    internal interface ICanBeTabbedTo
    {
        /// <summary>
        /// Whether this element can be tabbed to.
        /// </summary>
        bool CanBeTabbedTo { get; }
    }

    public class TabbableContentContainer<T> : Container<T>
        where T : Drawable
    {
        protected override bool OnKeyDown(KeyDownEvent e)
        {
            if (e.Key != Key.Tab)
                return false;

            var nextTab = nextTabStop(e.ShiftPressed);

            if (nextTab != null)
            {
                GetContainingInputManager().ChangeFocus(nextTab);
            }

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

            bool currentTargetFound = false;

            while (stack.Count > 0)
            {
                var drawable = stack.Pop();

                if (!currentTargetFound)
                    currentTargetFound = drawable.HasFocus;
                else if (drawable != this && drawable is ICanBeTabbedTo tabbable && tabbable.CanBeTabbedTo)
                    return drawable;

                if (drawable is CompositeDrawable composite)
                {
                    var newChildren = composite.InternalChildren.ToList();
                    int bound = reverse ? newChildren.Count : 0;

                    if (!currentTargetFound)
                    {
                        // Find self, to know starting point
                        int index = newChildren.FindIndex(d => d.HasFocus && d is ICanBeTabbedTo);
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
