// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;

namespace osu.Framework.Screens
{
    public class Screen : Container
    {
        protected Screen ParentScreen;
        public Screen ChildScreen;

        public bool IsCurrentScreen => !hasExited && hasEntered && ChildScreen == null;

        private readonly Container content;
        private Container childModeContainer;

        [Resolved]
        protected Game Game { get; private set; }

        protected override Container<Drawable> Content => content;

        public event Action<Screen> ModePushed;

        public event Action<Screen> Exited;

        private bool hasExited;
        private bool hasEntered;

        /// <summary>
        /// Make this Screen directly exited when resuming from a child.
        /// </summary>
        public bool ValidForResume = true;

        public Screen()
        {
            RelativeSizeAxes = Axes.Both;
            Anchor = Anchor.Centre;
            Origin = Anchor.Centre;

            AddRangeInternal(new[]
            {
                content = new ContentContainer
                {
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre
                },
            });
        }

        public override void Add(Drawable drawable)
        {
            if (drawable is Screen)
                throw new InvalidOperationException("Use Push to add nested Screens.");
            base.Add(drawable);
        }

        public override bool DisposeOnDeathRemoval => true;

        // in the case we don't have a parent screen, we still want to handle input as we are also responsible for
        // children inside childScreenContainer.
        // this means the root screen always received input.
        public override bool HandleKeyboardInput => IsCurrentScreen || !hasExited && ParentScreen == null;
        public override bool HandleMouseInput => IsCurrentScreen || !hasExited && ParentScreen == null;

        /// <summary>
        /// Called when this Screen is being entered. Only happens once, ever.
        /// </summary>
        /// <param name="last">The next Screen.</param>
        protected virtual void OnEntering(Screen last)
        {
        }

        /// <summary>
        /// Called when this Screen is exiting. Only happens once, ever.
        /// </summary>
        /// <param name="next">The next Screen.</param>
        /// <returns>Return true to cancel the exit process.</returns>
        protected virtual bool OnExiting(Screen next) => false;

        /// <summary>
        /// Called when this Screen is being returned to from a child exiting.
        /// </summary>
        /// <param name="last">The next Screen.</param>
        protected virtual void OnResuming(Screen last)
        {
        }

        /// <summary>
        /// Called when this Screen is being left to a new child.
        /// </summary>
        /// <param name="next">The new Screen</param>
        protected virtual void OnSuspending(Screen next)
        {
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            //for the case where we are at the top of the mode stack, we still want to run our OnEntering method.
            if (ParentScreen == null)
            {
                enter(null);

                AddInternal(childModeContainer = new Container
                {
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    RelativeSizeAxes = Axes.Both,
                });
            }
            else
            {
                childModeContainer = ParentScreen.childModeContainer;
            }
        }

        /// <summary>
        /// Changes to a new Screen.
        /// This will trigger an async load if the screen is not already loaded, during which the current screen will no longer be current (or accept user input).
        /// </summary>
        /// <param name="screen">The new Screen.</param>
        public virtual void Push(Screen screen)
        {
            if (hasExited)
                throw new TargetAlreadyExitedException();

            if (!IsCurrentScreen)
                throw new ScreenNotCurrentException(nameof(Push));

            if (ChildScreen != null)
                throw new ScreenHasChildException(nameof(Push), "Exit the existing child screen first.");

            if (screen.hasExited)
                throw new ScreenAlreadyExitedException();

            if (screen.hasEntered)
                throw new ScreenAlreadyEnteredException();

            screen.ParentScreen = this;
            startSuspend(screen);
            ModePushed?.Invoke(screen);

            void finishLoad()
            {
                if (hasExited || screen.hasExited)
                    return;

                childModeContainer.Add(screen);

                screen.enter(this);

                Content.Expire();
            }

            if (screen.LoadState >= LoadState.Ready)
                finishLoad();
            else
                LoadComponentAsync(screen, _ => finishLoad());
        }

        private void startSuspend(Screen next)
        {
            OnSuspending(next);
            Content.Expire();

            ChildScreen = next;
        }

        /// <summary>
        /// Exits this Screen.
        /// </summary>
        public void Exit()
        {
            if (ChildScreen != null)
                throw new ScreenHasChildException(nameof(Exit), $"Use {nameof(MakeCurrent)} instead.");

            ExitFrom(this);
        }

        private void enter(Screen source)
        {
            hasEntered = true;
            OnEntering(source);
        }

        /// <summary>
        /// Exits this Screen.
        /// </summary>
        /// <param name="source">Provides an exit source (used when skipping no-longer-valid modes upwards in stack).</param>
        protected void ExitFrom(Screen source)
        {
            if (hasExited)
                return;

            if (OnExiting(ParentScreen))
                return;

            hasExited = true;

            if (ValidForResume || source == this)
                Content.Expire();

            //propagate down the LifetimeEnd from the exit source.
            LifetimeEnd = source.Content.LifetimeEnd;

            Exited?.Invoke(ParentScreen);
            ParentScreen?.startResume(source);
            ParentScreen = null;

            Exited = null;
            ModePushed = null;
        }

        private void startResume(Screen source)
        {
            ChildScreen = null;

            if (ValidForResume)
            {
                OnResuming(source);
                Content.LifetimeEnd = double.MaxValue;
            }
            else
            {
                ExitFrom(source);
            }
        }


        public void MakeCurrent()
        {
            if (IsCurrentScreen) return;

            Screen c;
            for (c = ChildScreen; c.ChildScreen != null; c = c.ChildScreen)
                c.ValidForResume = false;

            //all the expired ones will exit
            c.Exit();
        }

        protected class ContentContainer : Container
        {
            public override bool HandleKeyboardInput => LifetimeEnd == double.MaxValue;
            public override bool HandleMouseInput => LifetimeEnd == double.MaxValue;
            public override bool RemoveWhenNotAlive => false;

            public ContentContainer()
            {
                RelativeSizeAxes = Axes.Both;
            }
        }

        public class TargetAlreadyExitedException : InvalidOperationException
        {
            public TargetAlreadyExitedException()
                : base("Cannot push to an already exited screen.")
            {
            }
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

        public class ScreenAlreadyExitedException : InvalidOperationException
        {
            public ScreenAlreadyExitedException()
                : base("Cannot push a screen in an exited state.")
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
    }
}
