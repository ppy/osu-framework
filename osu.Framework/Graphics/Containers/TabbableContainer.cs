// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
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

    public class TabbableContainer<T> : Container<T>
        where T : Drawable
    {
        /// <summary>
        /// Allows for tabbing between multiple levels within the TabbableContentContainer.
        /// </summary>
        public Container<Drawable> TabbableContentContainer { private get; set; }

        protected override bool OnKeyDown(InputState state, KeyDownEventArgs args)
        {
            if (args.Key != Key.Tab)
                return false;

            nextTabStop(TabbableContentContainer, state.Keyboard.ShiftPressed)?.TriggerFocus();
            return true;
        }

        private Drawable nextTabStop(Container<Drawable> target, bool reverse)
        {
            Stack<Container<Drawable>> stack = new Stack<Container<Drawable>>();
            stack.Push(target); // Extra push for circular tabbing
            stack.Push(target);

            bool started = false;
            while (stack.Count > 0)
            {
                var container = stack.Pop();
                if (container == null)
                    continue;

                if (!started)
                    started = ReferenceEquals(container, this);
                else if (container is TabbableContainer)
                    return container;

                var filtered = container.Children.OfType<Container<Drawable>>().ToList();
                int bound = reverse ? filtered.Count : 0;
                if (!started)
                {
                    // Find self, to know starting point
                    int index = filtered.IndexOf(this as TabbableContainer);
                    if (index != -1)
                        bound = reverse ? index + 1 : index;
                }

                if (reverse)
                {
                    for (int i = 0; i < bound; i++)
                        stack.Push(filtered[i]);
                }
                else
                {
                    for (int i = filtered.Count - 1; i >= bound; i--)
                        stack.Push(filtered[i]);
                }
            }

            return null;
        }
    }
}
