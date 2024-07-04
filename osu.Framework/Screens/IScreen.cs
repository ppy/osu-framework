// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using osu.Framework.Graphics;

namespace osu.Framework.Screens
{
    public interface IScreen : IDrawable
    {
        /// <summary>
        /// Whether this <see cref="IScreen"/> can be resumed.
        /// </summary>
        bool ValidForResume { get; set; }

        /// <summary>
        /// Whether this <see cref="IScreen"/> can be pushed.
        /// </summary>
        bool ValidForPush { get; set; }

        /// <summary>
        /// Invoked when this <see cref="IScreen"/> is entering from a parent <see cref="IScreen"/>.
        /// </summary>
        /// <param name="e">The <see cref="ScreenTransitionEvent"/> containing information about the screen event.</param>
        void OnEntering(ScreenTransitionEvent e);

        /// <summary>
        /// Invoked when this <see cref="IScreen"/> is exiting to a parent <see cref="IScreen"/>.
        /// </summary>
        /// <param name="e">The <see cref="ScreenExitEvent"/> containing information about the screen event.</param>
        /// <returns>True to cancel the exit process.</returns>
        bool OnExiting(ScreenExitEvent e);

        /// <summary>
        /// Invoked when this <see cref="IScreen"/> is entered from a child <see cref="IScreen"/>.
        /// </summary>
        /// <param name="e">The <see cref="ScreenTransitionEvent"/> containing information about the screen event.</param>
        void OnResuming(ScreenTransitionEvent e);

        /// <summary>
        /// Invoked when this <see cref="IScreen"/> is exited to a child <see cref="IScreen"/>.
        /// </summary>
        /// <param name="e">The <see cref="ScreenTransitionEvent"/> containing information about the screen event.</param>
        void OnSuspending(ScreenTransitionEvent e);
    }

    public static class ScreenExtensions
    {
        /// <summary>
        /// Pushes an <see cref="IScreen"/> to another.
        /// </summary>
        /// <param name="screen">The <see cref="IScreen"/> to push to.</param>
        /// <param name="newScreen">The <see cref="IScreen"/> to push.</param>
        public static void Push(this IScreen screen, IScreen newScreen)
        {
            var stack = getStack(screen);

            if (stack == null)
                throw new InvalidOperationException($"Cannot {nameof(Push)} to a non-loaded {nameof(IScreen)} directly. Consider using {nameof(ScreenStack)}.{nameof(ScreenStack.Push)} instead.");

            stack.Push(screen, newScreen);
        }

        /// <summary>
        /// Exits from an <see cref="IScreen"/>.
        /// </summary>
        /// <param name="screen">The <see cref="IScreen"/> to exit from.</param>
        public static void Exit(this IScreen screen)
        {
            var stack = getStack(screen);

            if (stack == null)
                screen.ValidForPush = false;
            else
                stack.Exit(screen);
        }

        /// <summary>
        /// Makes an <see cref="IScreen"/> the current screen, exiting all child <see cref="IScreen"/>s along the way.
        /// </summary>
        /// <param name="screen">The <see cref="IScreen"/> to make current.</param>
        public static void MakeCurrent(this IScreen screen)
            => getStack(screen)?.MakeCurrent(screen);

        /// <summary>
        /// Retrieves whether an <see cref="IScreen"/> is the current screen.
        /// This will return false on all <see cref="IScreen"/>s while a child <see cref="IScreen"/> is loading.
        /// </summary>
        /// <param name="screen">The <see cref="IScreen"/> to check.</param>
        /// <returns>True if <paramref name="screen"/> is the current screen.</returns>
        public static bool IsCurrentScreen(this IScreen screen)
            => getStack(screen)?.IsCurrentScreen(screen) ?? false;

        /// <summary>
        /// Retrieves the child <see cref="IScreen"/> of an <see cref="IScreen"/>.
        /// </summary>
        /// <param name="screen">The <see cref="IScreen"/> to retrieve the child of.</param>
        /// <returns>The child <see cref="IScreen"/> of <paramref name="screen"/>, or null if <paramref name="screen"/> has no child.</returns>
        public static IScreen GetChildScreen(this IScreen screen)
            => getStack(screen)?.GetChildScreen(screen);

        /// <summary>
        /// Retrieves the parent <see cref="IScreen"/> of an <see cref="IScreen"/>.
        /// </summary>
        /// <param name="screen">The <see cref="IScreen"/> to retrieve the parent of.</param>
        /// <returns>The parent <see cref="IScreen"/> of <paramref name="screen"/>, or null if <paramref name="screen"/> has no parent.</returns>
        public static IScreen GetParentScreen(this IScreen screen)
            => getStack(screen)?.GetParentScreen(screen);

        internal static Drawable AsDrawable(this IScreen screen) => (Drawable)screen;

        private static ScreenStack getStack(IDrawable current)
        {
            while (current != null)
            {
                if (current is ScreenStack stack)
                    return stack;

                current = current.Parent;
            }

            return null;
        }
    }
}
