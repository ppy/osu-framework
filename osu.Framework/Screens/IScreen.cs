// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

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
        /// <param name="last">The <see cref="IScreen"/> which has suspended.</param>
        void OnEntering(IScreen last);

        /// <summary>
        /// Invoked when this <see cref="IScreen"/> is exiting to a parent <see cref="IScreen"/>.
        /// </summary>
        /// <param name="next">The <see cref="IScreen"/> that will be resumed next.</param>
        /// <returns>True to cancel the exit process.</returns>
        bool OnExiting(IScreen next);

        /// <summary>
        /// Invoked when this <see cref="IScreen"/> is entered from a child <see cref="IScreen"/>.
        /// </summary>
        /// <param name="last">The next Screen.</param>
        void OnResuming(IScreen last);

        /// <summary>
        /// Invoked when this <see cref="IScreen"/> is exited to a child <see cref="IScreen"/>.
        /// </summary>
        /// <param name="next">The new Screen</param>
        void OnSuspending(IScreen next);
    }

    public static class ScreenExtensions
    {
        /// <summary>
        /// Pushes an <see cref="IScreen"/> to another.
        /// </summary>
        /// <param name="screen">The <see cref="IScreen"/> to push to.</param>
        /// <param name="newScreen">The <see cref="IScreen"/> to push.</param>
        public static void Push(this IScreen screen, IScreen newScreen)
            => runOnRoot(screen, stack => stack.Push(screen, newScreen));

        /// <summary>
        /// Exits from an <see cref="IScreen"/>.
        /// </summary>
        /// <param name="screen">The <see cref="IScreen"/> to exit from.</param>
        public static void Exit(this IScreen screen)
            => runOnRoot(screen, stack => stack.Exit(screen), () => screen.ValidForPush = false);

        /// <summary>
        /// Makes an <see cref="IScreen"/> the current screen, exiting all child <see cref="IScreen"/>s along the way.
        /// </summary>
        /// <param name="screen">The <see cref="IScreen"/> to make current.</param>
        public static void MakeCurrent(this IScreen screen)
            => runOnRoot(screen, stack => stack.MakeCurrent(screen));

        /// <summary>
        /// Retrieves whether an <see cref="IScreen"/> is the current screen.
        /// This will return false on all <see cref="IScreen"/>s while a child <see cref="IScreen"/> is loading.
        /// </summary>
        /// <param name="screen">The <see cref="IScreen"/> to check.</param>
        /// <returns>True if <paramref name="screen"/> is the current screen.</returns>
        public static bool IsCurrentScreen(this IScreen screen)
            => runOnRoot(screen, stack => stack.IsCurrentScreen(screen), () => false);

        /// <summary>
        /// Retrieves the child <see cref="IScreen"/> of an <see cref="IScreen"/>.
        /// </summary>
        /// <param name="screen">The <see cref="IScreen"/> to retrieve the child of.</param>
        /// <returns>The child <see cref="IScreen"/> of <paramref name="screen"/>, or <paramref name="screen"/> has no child.</returns>
        public static IScreen GetChildScreen(this IScreen screen)
            => runOnRoot(screen, stack => stack.GetChildScreen(screen), () => null);

        internal static Drawable AsDrawable(this IScreen screen) => (Drawable)screen;

        private static void runOnRoot(IDrawable current, Action<ScreenStack> onRoot, Action onFail = null)
        {
            switch (current)
            {
                case null:
                    onFail?.Invoke();
                    return;
                case ScreenStack stack:
                    onRoot(stack);
                    break;
                default:
                    runOnRoot(current.Parent, onRoot, onFail);
                    break;
            }
        }

        private static T runOnRoot<T>(IDrawable current, Func<ScreenStack, T> onRoot, Func<T> onFail = null)
        {
            switch (current)
            {
                case null:
                    if (onFail != null)
                        return onFail.Invoke();
                    return default;
                case ScreenStack stack:
                    return onRoot(stack);
                default:
                    return runOnRoot(current.Parent, onRoot, onFail);
            }
        }
    }
}
