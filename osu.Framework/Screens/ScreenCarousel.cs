// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using osu.Framework.Extensions.IEnumerableExtensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;

namespace osu.Framework.Screens
{
    /// <summary>
    /// A component that enables exiting and entering from any <see cref="IScreen"/> within it to another.
    /// </summary>
    /// <typeparam name="TKey">A type used to reference <see cref="IScreen"/>s added to this carousel.</typeparam>
    public class ScreenCarousel<TKey> : CompositeDrawable
    {
        private readonly CarouselScreenStack screenStack;

        protected readonly Dictionary<TKey, IScreen> Screens = new Dictionary<TKey, IScreen>();

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
        public void AddScreen(TKey key, IScreen screen)
        {
            if (Screens.ContainsKey(key))
                throw new InvalidOperationException($"Cannot add an {nameof(IScreen)} that has already been added to the {nameof(ScreenCarousel<TKey>)}");

            Screens.Add(key, screen);
        }

        /// <summary>
        /// Exit the current <see cref="IScreen"/> and enter a new one.
        /// </summary>
        /// <param name="key">The key of the <see cref="IScreen"/> to switch to</param>
        public void SwitchTo(TKey key)
        {
            if (!Screens.TryGetValue(key, out var screen))
                throw new InvalidOperationException($"Key provided for {nameof(SwitchTo)} does not exist in the {nameof(ScreenCarousel<TKey>)}");

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

        protected override void Dispose(bool isDisposing)
        {
            Screens.ForEach(s => s.Value.AsDrawable().Dispose());
            base.Dispose(isDisposing);
        }

        private class CarouselScreenStack : ScreenStack
        {
            // As this component should only ever have one screen inside its stack at a time, don't allow for pushing to screens.
            protected override bool AllowPushViaScreen => false;

            // Since we might need to re-use screens that have been previously exited, do not dispose screens on removal.
            protected override void Cleanup(Drawable d)
            {
                d.UnbindAllBindables();
            }
        }
    }
}
