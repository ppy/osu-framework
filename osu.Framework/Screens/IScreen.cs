// Copyright (c) 2007-2019 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;

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
        /// Called when this Screen is being entered. Only happens once, ever.
        /// </summary>
        /// <param name="last">The next Screen.</param>
        void OnEntering(IScreen last);

        /// <summary>
        /// Called when this Screen is exiting. Only happens once, ever.
        /// </summary>
        /// <param name="next">The next Screen.</param>
        /// <returns>Return true to cancel the exit process.</returns>
        bool OnExiting(IScreen next);

        /// <summary>
        /// Called when this Screen is being returned to from a child exiting.
        /// </summary>
        /// <param name="last">The next Screen.</param>
        void OnResuming(IScreen last);

        /// <summary>
        /// Called when this Screen is being left to a new child.
        /// </summary>
        /// <param name="next">The new Screen</param>
        void OnSuspending(IScreen next);
    }

    public delegate void ScreenChangedDelegate(IScreen lastScreen, IScreen newScreen);

    public class ScreenStack : CompositeDrawable
    {
        public event ScreenChangedDelegate ScreenPushed;
        public event ScreenChangedDelegate ScreenExited;

        public IScreen CurrentScreen => screens.FirstOrDefault();

        private readonly Stack<IScreen> screens = new Stack<IScreen>();

        public ScreenStack()
        {
        }

        public ScreenStack(IScreen baseScreen)
        {
            Push(baseScreen);
        }

        /// <summary>
        /// Pushes a <see cref="IScreen"/> to the current screen.
        /// </summary>
        /// <param name="screen"></param>
        public void Push(IScreen screen)
        {
            Push(null, screen);
        }

        /// <summary>
        /// Exits from the current <see cref="IScreen"/>.
        /// </summary>
        public void Exit()
        {
            Exit(CurrentScreen);
        }

        internal void Push(IScreen source, IScreen newScreen)
        {
            if (screens.Contains(newScreen))
                throw new Screen.ScreenAlreadyEnteredException();

            if (newScreen.AsDrawable().RemoveWhenNotAlive)
                throw new Screen.ScreenWillBeRemovedOnPushException(newScreen.GetType());

            // Suspend the current screen, if there is one
            if (source != null)
            {
                if (source != screens.Peek())
                    throw new Screen.ScreenNotCurrentException(nameof(Push));

                source.OnSuspending(newScreen);
                source.AsDrawable().Expire();
            }

            // Exited screens are expired, so the lifetime state needs to be reset to correctly display the re-pushed screen
            newScreen.AsDrawable().LifetimeEnd = double.MaxValue;

            // Push the new screen
            screens.Push(newScreen);
            ScreenPushed?.Invoke(source, newScreen);

            void finishLoad()
            {
                if (!newScreen.ValidForPush)
                {
                    Exit(newScreen);
                    return;
                }

                AddInternal(newScreen.AsDrawable());
                newScreen.OnEntering(source);
            }

            if (source != null)
                LoadScreen((CompositeDrawable)source, newScreen.AsDrawable(), finishLoad);
            else if (LoadState >= LoadState.Ready)
                LoadScreen(this, newScreen.AsDrawable(), finishLoad);
            else
                finishLoad();
        }

        protected virtual void LoadScreen(CompositeDrawable loader, Drawable toLoad, Action continuation)
        {
            if (toLoad.LoadState >= LoadState.Ready)
                continuation?.Invoke();
            else
                loader.LoadComponentAsync(toLoad, _ => continuation?.Invoke(), scheduler: Scheduler);
        }

        internal void Exit(IScreen source)
        {
            if (!screens.Contains(source))
                throw new Screen.ScreenNotCurrentException(nameof(Exit));

            if (CurrentScreen != source)
                throw new Screen.ScreenHasChildException(nameof(Exit), $"Use {nameof(ScreenExtensions.MakeCurrent)} instead.");

            exitFrom(source);
        }

        internal void MakeCurrent(IScreen source)
        {
            if (CurrentScreen == source)
                return;

            // Todo: This should throw an exception instead?
            if (!screens.Contains(source))
                return;

            foreach (var child in screens.Skip(1))
            {
                if (child == source)
                    break;
                child.ValidForResume = false;
            }

            Exit(CurrentScreen);
        }

        internal bool IsCurrentScreen(IScreen source) => source == CurrentScreen;

        internal IScreen GetChildScreen(IScreen source)
            => screens.TakeWhile(s => s != source).LastOrDefault();

        /// <summary>
        /// Exits the current <see cref="IScreen"/>.
        /// </summary>
        /// <param name="source">The <see cref="IScreen"/> which exited. May or may not be the current screen in the stack.</param>
        private void exitFrom(IScreen source)
        {
            // We're guaranteed that the top of the stack is the source
            screens.Pop();

            if (source.OnExiting(CurrentScreen))
            {
                screens.Push(source);
                return;
            }

            // Propagate the lifetime end from the exiting screen
            source.AsDrawable().Expire();
            source.AsDrawable().LifetimeEnd = ((Drawable)source).LifetimeEnd;

            ScreenExited?.Invoke(source, CurrentScreen);

            resume(source);
        }

        /// <summary>
        /// Resumes the current <see cref="IScreen"/>.
        /// </summary>
        /// <param name="source">The <see cref="IScreen"/> which exited. May or may not be the current screen in the stack.</param>
        private void resume(IScreen source)
        {
            if (CurrentScreen == null)
                return;

            if (CurrentScreen.ValidForResume)
            {
                CurrentScreen.OnResuming(source);

                // Suspended screens are expired, so the lifetime state needs to be reset to correctly display the resumed screen
                CurrentScreen.AsDrawable().LifetimeEnd = double.MaxValue;
            }
            else
                exitFrom(source);
        }

    }

    public static class ScreenExtensions
    {
        public static void Push(this IScreen screen, IScreen newScreen)
            => runOnRoot(screen, stack => stack.Push(screen, newScreen));

        public static void Exit(this IScreen screen)
            => runOnRoot(screen, stack => stack.Exit(screen), () => screen.ValidForPush = false);

        public static void MakeCurrent(this IScreen screen)
            => runOnRoot(screen, stack => stack.MakeCurrent(screen));

        public static bool IsCurrentScreen(this IScreen screen)
            => runOnRoot(screen, stack => stack.IsCurrentScreen(screen), () => false);

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
