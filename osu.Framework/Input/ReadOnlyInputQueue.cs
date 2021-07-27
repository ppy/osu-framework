// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using osu.Framework.Extensions.ListExtensions;
using osu.Framework.Graphics;
using osu.Framework.Input.Bindings;
using osu.Framework.Lists;

namespace osu.Framework.Input
{
    /// <summary>
    /// Holds grouped drawables for input event propagation.
    /// </summary>
    public class ReadOnlyInputQueue
    {
        protected readonly List<Drawable> AllInner;
        protected readonly List<Drawable> RegularInner;
        protected readonly List<KeyBindingContainer> KeyBingingContainersInner;

        protected ReadOnlyInputQueue(Func<Drawable> getFocusedDrawable = null)
        {
            GetFocusedDrawable = getFocusedDrawable;

            AllInner = new List<Drawable>();
            RegularInner = new List<Drawable>();
            KeyBingingContainersInner = new List<KeyBindingContainer>();

            All = AllInner.AsSlimReadOnly();
            Regular = RegularInner.AsSlimReadOnly();
            KeyBingingContainers = KeyBingingContainersInner.AsSlimReadOnly();
        }

        /// <summary>
        /// The currently focused <see cref="Drawable"/>. Null if there is no current focus.
        /// </summary>
        public readonly Func<Drawable> GetFocusedDrawable;

        /// <summary>
        /// Holds drawables that handles regular input.
        /// </summary>
        public readonly SlimReadOnlyListWrapper<Drawable> Regular;

        /// <summary>
        /// Holds drawables that handles key binding input.
        /// </summary>
        public readonly SlimReadOnlyListWrapper<KeyBindingContainer> KeyBingingContainers;

        /// <summary>
        /// Holds all drawables that handles any input
        /// </summary>
        public readonly SlimReadOnlyListWrapper<Drawable> All;
    }
}
