// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using osu.Framework.Graphics;
using osu.Framework.Input.Bindings;

namespace osu.Framework.Input
{
    public class InputQueue : ReadOnlyInputQueue
    {
        public List<Drawable> AllList => AllInner;
        public List<Drawable> RegularList => RegularInner;
        public List<KeyBindingContainer> KeyBingingContainersList => KeyBingingContainersInner;

        public InputQueue(Func<Drawable> getFocusedDrawable = null)
            : base(getFocusedDrawable)
        {
        }

        public void Add(Drawable drawable)
        {
            if (drawable is KeyBindingContainer kbc)
                KeyBingingContainersList.Add(kbc);
            else
                RegularList.Add(drawable);

            AllList.Add(drawable);
        }

        /// <summary>
        /// Clears underlying input queues.
        /// </summary>
        public void Clear()
        {
            AllList.Clear();
            RegularList.Clear();
            KeyBingingContainersList.Clear();
        }

        /// <summary>
        /// Reverses underlying input queues.
        /// </summary>
        public void Reverse()
        {
            AllList.Reverse();
            RegularList.Reverse();
            KeyBingingContainersList.Reverse();
        }

        public void RemoveAll(Predicate<Drawable> func)
        {
            RegularList.RemoveAll(func);
        }
    }
}
