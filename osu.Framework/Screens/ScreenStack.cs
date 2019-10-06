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
            if (source != null && source != stack.Peek()) throw new ScreenNotCurrentException(nameof(Push));

            if (suspendImmediately)
                suspend(source, newScreen);

            stack.Push(newScreen);
            ScreenPushed?.Invoke(source, newScreen);

            var newScreenDrawable = newScreen.AsDrawable();

            if (source == null)
            {
                // this is the first screen to be loaded.
                if (LoadState >= LoadState.Ready)
                    LoadScreen(this, newScreenDrawable, () => finishPush(null, newScreen));
                else
                    Schedule(() => finishPush(null, newScreen));
            }
            else
                LoadScreen((CompositeDrawable)source, newScreenDrawable, () => finishPush(source, newScreen));
        }

        /// <summary>
        /// Complete push of a loaded screen.
        /// </summary>
        /// <param name="parent">The screen to push to.</param>
        /// <param name="child">The new screen being pushed.</param>
        private void finishPush(IScreen parent, IScreen child)
        {
            if (!child.ValidForPush)
            {
                if (child == CurrentScreen)
                    exitFrom(null, shouldFireExitEvent: false, shouldFireResumeEvent: suspendImmediately);

                return;
            }

            if (!suspendImmediately)
                suspend(parent, child);

            AddInternal(child.AsDrawable());
            child.OnEntering(parent);
        }

        /// <summary>
        /// Complete suspend of a screen in the stack.
        /// </summary>
        /// <param name="from">The screen being suspended.</param>
        /// <param name="to">The screen being entered.</param>
        private void suspend(IScreen from, IScreen to)
        {
            var sourceDrawable = from?.AsDrawable();
            if (sourceDrawable == null)
                return;

            if (sourceDrawable.IsLoaded)
                performSuspend();
            else
            {
                // Screens only receive OnEntering() upon load completion, so OnSuspending() should be delayed until after that
                sourceDrawable.OnLoadComplete += _ => performSuspend();
            }

            void performSuspend()
            {
                from.OnSuspending(to);
                sourceDrawable.Expire();
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
            // If the previous screen has already been exited, do not attempt to load the new one.
            if ((loader as IScreen)?.ValidForPush == false)
                return;

            if (toLoad.LoadState >= LoadState.Ready)
                continuation?.Invoke();
            else
            {
                if (loader.LoadState >= LoadState.Ready)
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

        internal void MakeCurrent(IScreen target)
        {
            if (CurrentScreen == target)
                return;

            if (!stack.Contains(target))
                throw new ScreenNotInStackException(nameof(MakeCurrent));

            // while a parent still exists and exiting is not blocked, continue to iterate upwards.
            IScreen firstScreen = CurrentScreen;
            IScreen exitSource = null;

            while (CurrentScreen != null)
            {
                if (exitFrom(exitSource, shouldFireResumeEvent: false) || CurrentScreen == target)
                {
                    // don't fire the resume event if the first screen blocked the exit.
                    if (CurrentScreen != firstScreen)
                        resumeFrom(exitSource);
                    return;
                }

                exitSource = CurrentScreen;
            }
        }

        internal bool IsCurrentScreen(IScreen source) => source == CurrentScreen;

        internal IScreen GetChildScreen(IScreen source)
            => stack.TakeWhile(s => s != source).LastOrDefault();

        /// <summary>
        /// Exits the current <see cref="IScreen"/>.
        /// </summary>
        /// <param name="source">The <see cref="IScreen"/> which last exited.</param>
        /// <param name="shouldFireExitEvent">Whether <see cref="IScreen.OnExiting"/> should be fired on the exiting screen.</param>
        /// <param name="shouldFireResumeEvent">Whether <see cref="IScreen.OnResuming"/> should be fired on the resuming screen.</param>
        /// <returns>Whether the exit was blocked.</returns>
        private bool exitFrom([CanBeNull] IScreen source, bool shouldFireExitEvent = true, bool shouldFireResumeEvent = true)
        {
            if (stack.Count == 0)
                return false;

            // The current screen is at the top of the stack, it will be the one that is exited
            var toExit = stack.Pop();

            // The next current screen will be resumed
            if (shouldFireExitEvent && toExit.AsDrawable().IsLoaded)
            {
                // if a screen is !ValidForResume, it should not be allowed to block unless it is the current screen (source == null)
                // OnExiting should still be called regardless.
                bool blockRequested = toExit.OnExiting(CurrentScreen);

                if ((source == null || toExit.ValidForResume) && blockRequested)
                {
                    // If the exit event gets cancelled, add the screen back on the stack.
                    stack.Push(toExit);
                    return true;
                }
            }

            // we will probably want to change this logic when we support returning to a screen after exiting.
            toExit.ValidForResume = false;
            toExit.ValidForPush = false;

            if (source == null)
            {
                // This is the first screen that exited
                toExit.AsDrawable().Expire();
            }

            exited.Add(toExit.AsDrawable());

            ScreenExited?.Invoke(toExit, CurrentScreen);

            // Resume the next current screen from the exited one
            if (shouldFireResumeEvent)
                resumeFrom(toExit);

            return false;
        }

        /// <summary>
        /// Unbind and return leases for all <see cref="Bindable{T}"/>s managed by the exiting screen.
        /// </summary>
        /// <remarks>
        /// While all bindables will eventually be cleaned up by disposal logic, this is too late as
        /// leases could potentially be in a leased state during exiting transitions.
        /// This method should be called after exiting is confirmed to ensure a correct leased state before <see cref="IScreen.OnResuming"/>.
        /// </remarks>
        private void onExited(IScreen prev, IScreen next) => (prev as CompositeDrawable)?.UnbindAllBindablesSubTree();

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

        protected override bool ShouldBeConsideredForInput(Drawable child) => !(child is IScreen screen) || screen.IsCurrentScreen();

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

        internal override void UnbindAllBindablesSubTree()
        {
            // Suspended screens that are not part of our children won't receive unbind invocations until their disposal, which happens too late.
            // To get around this, we unbind them ourselves in the correct order (reverse-push)
            // Exited screens don't need to be unbound here due to being unbound when exiting

            foreach (var s in stack)
                s.AsDrawable().UnbindAllBindablesSubTree();

            base.UnbindAllBindablesSubTree();
        }

        public class ScreenNotCurrentException : InvalidOperationException
        {
            public ScreenNotCurrentException(string action)
                : base($"Cannot perform {action} on a non-current screen.")
            {
            }
        }

        protected override void Dispose(bool isDisposing)
        {
            // Correct disposal order must be enforced manually before the base disposal.

            foreach (var s in exited)
                s.Dispose();
            exited.Clear();

            foreach (var s in stack)
                s.AsDrawable().Dispose();
            stack.Clear();

            base.Dispose(isDisposing);
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
