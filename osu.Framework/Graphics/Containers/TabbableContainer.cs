// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System.Collections.Generic;
using System.Linq;
using osu.Framework.Input;
using OpenTK.Input;

namespace osu.Framework.Graphics.Containers
{
    public class TabbableContainer : TabbableContainer<Drawable>
    {
    }

    /// <summary>
    /// This interface is used for recogonizing <see cref="TabbableContainer{T}"/> of any type without reflection.
    /// </summary>
    internal interface ITabbableContainer
    {
        /// <summary>
        /// Whether this <see cref="ITabbableContainer"/> can be tabbed to.
        /// </summary>
        bool CanBeTabbedTo { get; }
    }

    public class TabbableContainer<T> : Container<T>, ITabbableContainer
        where T : Drawable
    {
        /// <summary>
        /// Whether this <see cref="TabbableContainer{T}"/> can be tabbed to.
        /// </summary>
        public virtual bool CanBeTabbedTo => true;

        /// <summary>
        /// Allows for tabbing between multiple levels within the TabbableContentContainer.
        /// </summary>
        public Container<Drawable> TabbableContentContainer { private get; set; }

        protected override bool OnKeyDown(InputState state, KeyDownEventArgs args)
        {
            if (TabbableContentContainer == null || args.Key != Key.Tab)
                return false;

            var nextTab = nextTabStop(TabbableContentContainer, state.Keyboard.ShiftPressed);
            if (nextTab != null) GetContainingInputManager().ChangeFocus(nextTab);
            return true;
        }

        private Drawable nextTabStop(Container<Drawable> target, bool reverse)
        {
            Stack<Drawable> stack = new Stack<Drawable>();
            stack.Push(target); // Extra push for circular tabbing
            stack.Push(target);

            bool started = false;
            while (stack.Count > 0)
            {
                var drawable = stack.Pop();

                if (!started)
                    started = ReferenceEquals(drawable, this);
                else if (drawable is ITabbableContainer tabbable && tabbable.CanBeTabbedTo)
                    return drawable;

                if (drawable is CompositeDrawable composite)
                {
                    var newChildren = composite.InternalChildren.ToList();
                    int bound = reverse ? newChildren.Count : 0;
                    if (!started)
                    {
                        // Find self, to know starting point
                        int index = newChildren.IndexOf(this);
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
