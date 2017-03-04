﻿// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Input;
using OpenTK.Input;

namespace osu.Framework.Screens
{
    public class Screen : Container
    {
        protected Screen ParentScreen;
        public Screen ChildScreen;

        public bool IsCurrentScreen => !hasExited && ChildScreen == null;

        private Container content;
        private Container childModeContainer;

        protected Game Game;

        protected override Container<Drawable> Content => content;

        public event Action<Screen> ModePushed;

        public event Action<Screen> Exited;

        private bool hasExited;

        /// <summary>
        /// Make this Screen directly exited when resuming from a child.
        /// </summary>
        public bool ValidForResume = true;

        public Screen()
        {
            RelativeSizeAxes = Axes.Both;
            Anchor = Anchor.Centre;
            Origin = Anchor.Centre;

            AddInternal(new[]
            {
                content = new ContentContainer
                {
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre
                },
                childModeContainer = new Container
                {
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    RelativeSizeAxes = Axes.Both,
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

        public override bool HandleInput => !hasExited;

        /// <summary>
        /// Called when this Screen is being entered. Only happens once, ever.
        /// </summary>
        /// <param name="last">The next Screen.</param>
        protected virtual void OnEntering(Screen last) { }

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
        protected virtual void OnResuming(Screen last) { }

        /// <summary>
        /// Called when this Screen is being left to a new child.
        /// </summary>
        /// <param name="next">The new Screen</param>
        protected virtual void OnSuspending(Screen next) { }

        [BackgroundDependencyLoader]
        private void load(Game game)
        {
            Game = game;
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            //for the case where we are at the top of the mode stack, we still want to run our OnEntering method.
            if (ParentScreen == null)
                OnEntering(null);
        }

        protected override bool OnKeyDown(InputState state, KeyDownEventArgs args)
        {
            if (args.Repeat) return false;

            switch (args.Key)
            {
                case Key.Escape:
                    Exit();
                    return true;
            }

            return base.OnKeyDown(state, args);
        }

        /// <summary>
        /// Changes to a new Screen.
        /// </summary>
        /// <param name="screen">The new Screen.</param>
        public virtual bool Push(Screen screen)
        {
            if (ChildScreen != null)
                throw new InvalidOperationException("Can not push more than one child screen.");

            screen.ParentScreen = this;
            childModeContainer.Add(screen);

            if (screen.hasExited)
            {
                screen.Expire();
                return false;
            }

            startSuspend(screen);

            screen.OnEntering(this);

            ModePushed?.Invoke(screen);

            Content.Expire();

            return true;
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
        public void Exit() => ExitFrom(this);

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
            public override bool HandleInput => LifetimeEnd == double.MaxValue;
            public override bool RemoveWhenNotAlive => false;

            public ContentContainer()
            {
                RelativeSizeAxes = Axes.Both;
            }
        }
    }
}
