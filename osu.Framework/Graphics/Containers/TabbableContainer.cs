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

            // Yes, searching forwards has a step of -1 due to stack behavior
            nextTabStop(TabbableContentContainer, state.Keyboard.ShiftPressed ? 1 : -1)?.TriggerFocus();
            return true;
        }

        private Drawable nextTabStop(Container<Drawable> target, int step)
        {
            Stack<Container<Drawable>> stack = new Stack<Container<Drawable>>();
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
                int current = filtered.Count;
                if (!started)
                {
                    // Find self, to know starting point
                    current = filtered.IndexOf(this as TabbableContainer);
                    // Search own children
                    if (current != -1)
                    {
                        var self = filtered[current];
                        current += step + filtered.Count;
                        for (int i = 0; i < filtered.Count - 1; i++)
                        {
                            stack.Push(filtered[(current + step * i) % filtered.Count]);
                        }
                        stack.Push(self);
                        continue;
                    }
                    current = filtered.Count;
                }

                // Search other children
                for (int i = 0; i < filtered.Count; i++)
                {
                    stack.Push(filtered[(current + step * i) % filtered.Count]);
                }
            }

            return null;
        }
    }
}
