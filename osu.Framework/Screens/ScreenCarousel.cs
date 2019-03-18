// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;

namespace osu.Framework.Screens
{
    /// <summary>
    /// A component that enables exiting and entering from any <see cref="IScreen"/> inside a <see cref="ScreenStack"/> to another.
    /// </summary>
    /// <typeparam name="T">The type of Key to identify <see cref="IScreen"/> added to this component</typeparam>
    public class ScreenCarousel<T> : CompositeDrawable
    {
        private readonly CarouselScreenStack screenStack;

        protected readonly Dictionary<T, IScreen> Screens = new Dictionary<T, IScreen>();

        public IScreen CurrentScreen => screenStack.CurrentScreen;

        public ScreenCarousel()
        {
            InternalChild = screenStack = new CarouselScreenStack { RelativeSizeAxes = Axes.Both };
        }

        /// <summary>
        /// Adds a new <see cref="IScreen"/> to this component.
        /// </summary>
        /// <param name="key"> The key used to identify this screen </param>
        /// <param name="screen"> The <see cref="IScreen"/> to add to the carousel </param>
        public void AddScreen(T key, IScreen screen)
        {
            if (Screens.ContainsValue(screen))
                throw new InvalidOperationException($"Cannot add an {nameof(IScreen)} that has already been added to the {nameof(ScreenCarousel<T>)}");

            Screens.Add(key, screen);
        }

        /// <summary>
        /// Exit the current <see cref="IScreen"/> and enter a new one.
        /// </summary>
        /// <param name="key">The key of the <see cref="IScreen"/> to switch to</param>
        public void SwitchTo(T key)
        {
            if (!Screens.TryGetValue(key, out var screen))
                throw new InvalidOperationException($"Key provided for {nameof(SwitchTo)} does not exist in the {nameof(ScreenCarousel<T>)}");

            // We don't need to do anything if we're already on the target screen.
            if (screen == screenStack.CurrentScreen)
                return;

            // If the current screen has not yet exited, keep trying to exit it until it is.
            if (screenStack.CurrentScreen != null)
            {
                screenStack.Exit();

                Schedule(() => { SwitchTo(key); });
                return;
            }

            // Once the screen stack is empty, we can safely push our screen.
            screenStack.Push(screen);
        }

        private class CarouselScreenStack : ScreenStack
        {
            // As this component should only ever have one screen inside its stack at a time, don't allow for pushing to screens.
            protected override bool AllowPushViaScreen => false;

            protected override bool InvalidateScreensOnExit => false;

            // Since we might need to re-use screens that have been previously exited, do not dispose screens on removal.
            protected override void Cleanup(Drawable d)
            {
                RemoveInternal(d);
            }
        }
    }
}
