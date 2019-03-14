// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using osu.Framework.Bindables;
using osu.Framework.Extensions.TypeExtensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;

namespace osu.Framework.Screens
{
    /// <summary>
    /// A component which provides functionality for displaying and handling transitions between multiple <see cref="IScreen"/>s.
    /// </summary>
    public class ScreenStack : CompositeDrawable
    {
        /// <summary>
        /// Invoked when <see cref="ScreenExtensions.Push"/> is called on a <see cref="IScreen"/>.
        /// </summary>
        public event ScreenChangedDelegate ScreenPushed;

        /// <summary>
        /// Invoked when <see cref="ScreenExtensions.Exit"/> is called on a <see cref="IScreen"/>.
        /// </summary>
        public event ScreenChangedDelegate ScreenExited;

        /// <summary>
        /// The currently-active <see cref="IScreen"/>.
        /// </summary>
        public IScreen CurrentScreen => stack.FirstOrDefault();

        private readonly Stack<IScreen> stack = new Stack<IScreen>();

        /// <summary>
        /// Screens which are exited and require manual cleanup.
        /// </summary>
        private readonly List<Drawable> exited = new List<Drawable>();

        private readonly bool suspendImmediately;

        /// <summary>
        /// Creates a new <see cref="ScreenStack"/> with no active <see cref="IScreen"/>.
        /// </summary>
        /// <param name="suspendImmediately">Whether <see cref="IScreen.OnSuspending"/> should be called immediately, or wait for the next screen to be loaded first.</param>
        public ScreenStack(bool suspendImmediately = true)
        {
            this.suspendImmediately = suspendImmediately;
            ScreenExited += onExited;
        }

        /// <summary>
        /// Creates a new <see cref="ScreenStack"/>, and immediately pushes a <see cref="IScreen"/>.
        /// </summary>
        /// <param name="baseScreen">The initial <see cref="IScreen"/> to be loaded</param>
        /// <param name="suspendImmediately">Whether <see cref="IScreen.OnSuspending"/> should be called immediately, or wait for the next screen to be loaded first.</param>
        public ScreenStack(IScreen baseScreen, bool suspendImmediately = true)
            : this(suspendImmediately)
        {
            Push(baseScreen);
        }

        /// <summary>
        /// Pushes a <see cref="IScreen"/> to this <see cref="ScreenStack"/>.
        /// </summary>
        /// <param name="screen">The <see cref="IScreen"/> to push.</param>
        public void Push(IScreen screen)
        {
            Push(CurrentScreen, screen);
        }

        /// <summary>
        /// Exits the current <see cref="IScreen"/>.
        /// </summary>
        public void Exit()
        {
            Exit(CurrentScreen);
        }

        internal void Push(IScreen source, IScreen newScreen)
        {
            if (stack.Contains(newScreen))
                throw new ScreenAlreadyEnteredException();

            if (source == null && stack.Count > 0)
                throw new InvalidOperationException($"A source must be provided when pushing to a non-empty {nameof(ScreenStack)}");

            if (newScreen.AsDrawable().RemoveWhenNotAlive)
                throw new ScreenWillBeRemovedOnPushException(newScreen.GetType());

            // Suspend the current screen, if there is one
            if (source != null)
            {
                if (source != stack.Peek())
                    throw new ScreenNotCurrentException(nameof(Push));

                if (suspendImmediately)
                    suspend();
            }

            // Push the new screen
            stack.Push(newScreen);
            ScreenPushed?.Invoke(source, newScreen);

            if (source != null)
                LoadScreen((CompositeDrawable)source, newScreen.AsDrawable(), finishLoad);
            else if (LoadState >= LoadState.Ready)
                LoadScreen(this, newScreen.AsDrawable(), finishLoad);
            else
                Schedule(finishLoad);

            void finishLoad()
            {
                if (!suspendImmediately)
                    suspend();

                if (!newScreen.ValidForPush)
                {
                    exitFrom(null);
                    return;
                }

                AddInternal(newScreen.AsDrawable());
                newScreen.OnEntering(source);
            }

            void suspend()
            {
                source?.OnSuspending(newScreen);
                source?.AsDrawable().Expire();
            }
        }

        /// <summary>
        /// Loads a <see cref="IScreen"/> through a <see cref="CompositeDrawable"/>.
        /// </summary>
        /// <param name="loader">The <see cref="CompositeDrawable"/> to load <paramref name="toLoad"/> with.</param>
        /// <param name="toLoad">The <see cref="IScreen"/> to load.</param>
        /// <param name="continuation">The <see cref="Action"/> to invoke after <paramref name="toLoad"/> has finished loading.</param>
        protected virtual void LoadScreen(CompositeDrawable loader, Drawable toLoad, Action continuation)
        {
            if (toLoad.LoadState >= LoadState.Ready)
                continuation?.Invoke();
            else
            {
                if (loader.LoadState >= LoadState.Loaded)
                    loader.LoadComponentAsync(toLoad, _ => continuation?.Invoke(), scheduler: Scheduler);
                else
                    Schedule(() => LoadScreen(loader, toLoad, continuation));
            }
        }

        internal void Exit(IScreen source)
        {
            if (!stack.Contains(source))
                throw new ScreenNotCurrentException(nameof(Exit));

            if (CurrentScreen != source)
                throw new ScreenHasChildException(nameof(Exit), $"Use {nameof(ScreenExtensions.MakeCurrent)} instead.");

            exitFrom(null);
        }

        internal void MakeCurrent(IScreen source)
        {
            if (CurrentScreen == source)
                return;

            if (!stack.Contains(source))
                throw new ScreenNotInStackException(nameof(MakeCurrent));

            exitFrom(null, () =>
            {
                foreach (var child in stack)
                {
                    if (child == source)
                        break;
                    child.ValidForResume = false;
                }
            });
        }

        internal bool IsCurrentScreen(IScreen source) => source == CurrentScreen;

        internal IScreen GetChildScreen(IScreen source)
            => stack.TakeWhile(s => s != source).LastOrDefault();

        /// <summary>
        /// Exits the current <see cref="IScreen"/>.
        /// </summary>
        /// <param name="source">The <see cref="IScreen"/> which last exited.</param>
        /// <param name="onExiting">An action that is invoked when the current screen allows the exit to continue.</param>
        private void exitFrom([CanBeNull] IScreen source, Action onExiting = null)
        {
            if (stack.Count == 0)
                return;

            // The current screen is at the top of the stack, it will be the one that is exited
            var toExit = stack.Pop();

            // The next current screen will be resumed
            if (toExit.OnExiting(CurrentScreen))
            {
                stack.Push(toExit);
                return;
            }

            // we will probably want to change this logic when we support returning to a screen after exiting.
            toExit.ValidForResume = false;
            toExit.ValidForPush = false;

            onExiting?.Invoke();

            if (source == null)
            {
                // This is the first screen that exited
                toExit.AsDrawable().Expire();
            }

            exited.Add(toExit.AsDrawable());

            ScreenExited?.Invoke(toExit, CurrentScreen);

            // Resume the next current screen from the exited one
            resumeFrom(toExit);
        }

        /// <summary>
        /// Unbind and return leases for all <see cref="Bindable{T}"/>s managed by the exiting screen.
        /// </summary>
        /// <remarks>
        /// While all bindables will eventually be cleaned up by disposal logic, this is too late as
        /// leases could potentially be in a leased state during exiting transitions.
        /// This method should be called after exiting is confirmed to ensure a correct leased state before <see cref="IScreen.OnResuming"/>.
        /// </remarks>
        private void onExited(IScreen prev, IScreen next) => (prev as CompositeDrawable)?.UnbindAllBindables();

        /// <summary>
        /// Resumes the current <see cref="IScreen"/>.
        /// </summary>
        /// <param name="source">The <see cref="IScreen"/> which exited.</param>
        private void resumeFrom([NotNull] IScreen source)
        {
            if (CurrentScreen == null)
                return;

            if (CurrentScreen.ValidForResume)
            {
                CurrentScreen.OnResuming(source);

                // Screens are expired when they are suspended - lifetime needs to be reset when resumed
                CurrentScreen.AsDrawable().LifetimeEnd = double.MaxValue;
            }
            else
                exitFrom(source);
        }

        protected override bool UpdateChildrenLife()
        {
            if (!base.UpdateChildrenLife()) return false;

            // In order to provide custom suspend/resume logic, screens always have RemoveWhenNotAlive set to false.
            // We need to manually handle removal here (in the opposite order to how the screens were pushed to ensure bindable sanity).
            if (exited.FirstOrDefault()?.IsAlive == false)
            {
                foreach (var s in exited)
                {
                    RemoveInternal(s);
                    DisposeChildAsync(s);
                }

                exited.Clear();
            }

            return true;
        }

        public class ScreenNotCurrentException : InvalidOperationException
        {
            public ScreenNotCurrentException(string action)
                : base($"Cannot perform {action} on a non-current screen.")
            {
            }
        }

        public class ScreenHasChildException : InvalidOperationException
        {
            public ScreenHasChildException(string action, string description)
                : base($"Cannot perform {action} when a child is already present. {description}")
            {
            }
        }

        public class ScreenAlreadyEnteredException : InvalidOperationException
        {
            public ScreenAlreadyEnteredException()
                : base("Cannot push a screen in an entered state.")
            {
            }
        }

        public class ScreenWillBeRemovedOnPushException : InvalidOperationException
        {
            public ScreenWillBeRemovedOnPushException(Type type)
                : base($"The pushed ({type.ReadableName()}) has {nameof(RemoveWhenNotAlive)} = true and will be removed when a child screen is pushed. "
                       + $"Screens must set {nameof(RemoveWhenNotAlive)} to false.")
            {
            }
        }

        public class ScreenNotInStackException : InvalidOperationException
        {
            public ScreenNotInStackException(string action)
                : base($"Cannot perform {action} on a screen not in a {nameof(ScreenStack)}.")
            {
            }
        }
    }

    /// <param name="lastScreen">The <see cref="IScreen"/> that was exited or suspended.</param>
    /// <param name="newScreen">The <see cref="IScreen"/> that was pushed or resumed.</param>
    public delegate void ScreenChangedDelegate(IScreen lastScreen, IScreen newScreen);
}
