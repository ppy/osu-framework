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
        bool ValidForResume { get; set; }

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

    public class ScreenStack : CompositeDrawable
    {
        public event Action<IScreen> ScreenPushed;
        public event Action<IScreen> ScreenExited;

        private readonly Stack<ScreenDescriptor> screens = new Stack<ScreenDescriptor>();

        public ScreenStack(IScreen baseScreen)
        {
            var descriptor = new ScreenDescriptor { Screen = baseScreen };

            screens.Push(descriptor);

            AddInternal(descriptor.ScreenDrawable);

            baseScreen.OnEntering(null);
        }

        internal void Push(IScreen source, IScreen newScreen)
        {
            if (source != screens.Peek().Screen)
                throw new Screen.ScreenNotCurrentException(nameof(Push));

            if (screens.Any(d => d.Screen == newScreen))
                throw new Screen.ScreenAlreadyEnteredException();

            var last = screens.Peek();
            var next = new ScreenDescriptor { Screen = newScreen };

            if (next.ScreenDrawable.RemoveWhenNotAlive)
                throw new Screen.ScreenWillBeRemovedOnPushException(newScreen.GetType());

            // Suspend the current screen
            last.Screen.OnSuspending(newScreen);
            last.ScreenDrawable.Expire();

            next.ScreenDrawable.LifetimeEnd = double.MaxValue;

            // Push the new screen
            screens.Push(next);
            ScreenPushed?.Invoke(newScreen);

            void finishLoad()
            {
                if (!screens.Contains(last) || !screens.Contains(next))
                    return;

                AddInternal(next.ScreenDrawable);
                next.Screen.OnEntering(last.Screen);
            }

            LoadScreen(last.ScreenDrawable, next.ScreenDrawable, finishLoad);
        }

        protected virtual void LoadScreen(CompositeDrawable last, CompositeDrawable next, Action continuation)
        {
            if (next.LoadState >= LoadState.Ready)
                continuation?.Invoke();
            else
                last.LoadComponentAsync(next, _ => continuation?.Invoke());
        }

        internal void Exit(IScreen source)
        {
            if (screens.All(d => d.Screen != source))
                throw new Screen.ScreenNotCurrentException(nameof(Exit));
            
            if (source != screens.First().Screen)
                throw new Screen.ScreenHasChildException(nameof(Exit), $"Use {nameof(ScreenExtensions.MakeCurrent)} instead.");

            exitFrom(source);
        }

        internal void MakeCurrent(IScreen source)
        {
            if (source == screens.Peek().Screen)
                return;

            // Todo: This should throw an exception instead?
            if (screens.All(d => d.Screen != source))
                return;

            foreach (var child in screens)
            {
                if (child.Screen == source)
                    break;
                child.Screen.ValidForResume = false;
            }

            Exit(screens.Peek().Screen);
        }

        internal bool IsCurrentScreen(IScreen source)
            => source == screens.Peek().Screen;

        internal IScreen GetChildScreen(IScreen source)
            => screens.TakeWhile(d => d.Screen != source).LastOrDefault()?.Screen;

        /// <summary>
        /// Exits the current <see cref="IScreen"/>.
        /// </summary>
        /// <param name="source">The <see cref="IScreen"/> which exited. May or may not be the current screen in the stack.</param>
        private void exitFrom(IScreen source)
        {
            var last = screens.Pop();
            var next = screens.Peek();

            if (last.Screen.OnExiting(next.Screen))
            {
                screens.Push(last);
                return;
            }

            // Propagate the lifetime end from the exiting screen
            last.ScreenDrawable.Expire();
            last.ScreenDrawable.LifetimeEnd = ((Drawable)source).LifetimeEnd;

            ScreenExited?.Invoke(last.Screen);

            resume(source);
        }

        /// <summary>
        /// Resumes the current <see cref="IScreen"/>.
        /// </summary>
        /// <param name="source">The <see cref="IScreen"/> which exited. May or may not be the current screen in the stack.</param>
        private void resume(IScreen source)
        {
            var next = screens.Peek();

            if (next.Screen.ValidForResume)
            {
                next.Screen.OnResuming(source);
                next.ScreenDrawable.LifetimeEnd = double.MaxValue;
            }
            else
                exitFrom(source);
        }

        private class ScreenDescriptor
        {
            public IScreen Screen;
            public CompositeDrawable ScreenDrawable => (CompositeDrawable)Screen;
        }
    }

    public static class ScreenExtensions
    {
        public static void Push(this IScreen screen, IScreen newScreen)
            => runOnRoot(screen, stack => stack.Push(screen, newScreen));

        public static void Exit(this IScreen screen)
            => runOnRoot(screen, stack => stack.Exit(screen));

        public static void MakeCurrent(this IScreen screen)
            => runOnRoot(screen, stack => stack.MakeCurrent(screen));

        public static bool IsCurrentScreen(this IScreen screen)
            => runOnRoot(screen, stack => stack.IsCurrentScreen(screen));

        internal static IScreen GetChildScreen(this IScreen screen)
            => runOnRoot(screen, stack => stack.GetChildScreen(screen));

        private static void runOnRoot(IDrawable current, Action<ScreenStack> action)
        {
            if (current is ScreenStack stack)
                action(stack);
            else
                runOnRoot(current.Parent, action);
        }

        private static T runOnRoot<T>(IDrawable current, Func<ScreenStack, T> action)
        {
            if (current is ScreenStack stack)
                return action(stack);
            return runOnRoot(current.Parent, action);
        }
    }
}
