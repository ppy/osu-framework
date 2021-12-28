// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Android.Views;
using osu.Framework.Extensions.EnumExtensions;
using osu.Framework.Input.Handlers;
using osu.Framework.Platform;

#nullable enable

namespace osu.Framework.Android.Input
{
    /// <summary>
    /// Base input handler for handling events dispatched by <see cref="AndroidGameView"/>.
    /// Provides consistent and unified means for handling <see cref="InputEvent"/>s only from specific <see cref="InputSourceType"/>s.
    /// </summary>
    public abstract class AndroidInputHandler : InputHandler
    {
        /// <summary>
        /// The <see cref="InputSourceType"/>s that this <see cref="InputHandler"/> will handle.
        /// </summary>
        protected abstract IEnumerable<InputSourceType> HandledInputSources { get; }

        /// <summary>
        /// The view that this <see cref="InputHandler"/> is handling events from.
        /// </summary>
        protected readonly AndroidGameView View;

        /// <summary>
        /// Bitmask of all <see cref="HandledInputSources"/>.
        /// </summary>
        private InputSourceType inputSourceBitmask;

        protected AndroidInputHandler(AndroidGameView view)
        {
            View = view;
        }

        public override bool Initialize(GameHost host)
        {
            if (!base.Initialize(host))
                return false;

            // compute the bitmask for later use.
            foreach (var inputSource in HandledInputSources)
            {
                // ReSharper disable once BitwiseOperatorOnEnumWithoutFlags (InputSourceType is a flags enum, but is not properly marked as such)
                inputSourceBitmask |= inputSource;
            }

            return true;
        }

        /// <summary>
        /// Checks whether the <paramref name="inputEvent"/> should be handled by this <see cref="InputHandler"/>.
        /// </summary>
        /// <param name="inputEvent">The <see cref="InputEvent"/> to check.</param>
        /// <returns><c>true</c> if the <paramref name="inputEvent"/>'s <see cref="InputSourceType"/> matches <see cref="HandledInputSources"/>.</returns>
        /// <remarks>Should be checked before handling events.</remarks>
        protected bool ShouldHandleEvent([NotNullWhen(true)] InputEvent? inputEvent)
        {
            return inputEvent != null && inputSourceBitmask.HasFlagFast(inputEvent.Source);
        }
    }
}
