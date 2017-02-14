// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Diagnostics;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Input;
using OpenTK.Input;

namespace osu.Framework.GameModes
{
    public class GameMode : Container
    {
        protected GameMode ParentGameMode;
        public GameMode ChildGameMode;

        public bool IsCurrentGameMode => ChildGameMode == null;

        private Container content;
        private Container childModeContainer;

        protected BaseGame Game;

        protected override Container<Drawable> Content => content;

        public event Action<GameMode> ModePushed;

        public event Action<GameMode> Exited;

        private bool hasExited;

        /// <summary>
        /// Make this GameMode directly exited when resuming from a child.
        /// </summary>
        public bool ValidForResume = true;

        public GameMode()
        {
            RelativeSizeAxes = Axes.Both;
            Anchor = Anchor.Centre;
            Origin = Anchor.Centre;

            AddInternal(new[]
            {
                content = new ContentContainer()
                {
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre
                },
                childModeContainer = new Container()
                {
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    RelativeSizeAxes = Axes.Both,
                },
            });
        }

        public override bool DisposeOnDeathRemoval => true;

        public override bool HandleInput => !hasExited;

        /// <summary>
        /// Called when this GameMode is being entered. Only happens once, ever.
        /// </summary>
        /// <param name="last">The next GameMode.</param>
        protected virtual void OnEntering(GameMode last) { }

        /// <summary>
        /// Called when this GameMode is exiting. Only happens once, ever.
        /// </summary>
        /// <param name="next">The next GameMode.</param>
        /// <returns>Return true to cancel the exit process.</returns>
        protected virtual bool OnExiting(GameMode next) => false;

        /// <summary>
        /// Called when this GameMode is being returned to from a child exiting.
        /// </summary>
        /// <param name="last">The next GameMode.</param>
        protected virtual void OnResuming(GameMode last) { }

        /// <summary>
        /// Called when this GameMode is being left to a new child.
        /// </summary>
        /// <param name="next">The new GameMode</param>
        protected virtual void OnSuspending(GameMode next) { }

        protected internal override void PerformLoad(BaseGame game)
        {
            Game = game;
            base.PerformLoad(game);
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            //for the case where we are at the top of the mode stack, we still want to run our OnEntering method.
            if (ParentGameMode == null)
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
        /// Changes to a new GameMode.
        /// </summary>
        /// <param name="mode">The new GameMode.</param>
        public virtual bool Push(GameMode mode)
        {
            Debug.Assert(ChildGameMode == null);

            mode.ParentGameMode = this;
            childModeContainer.Add(mode);

            if (mode.hasExited)
            {
                mode.Expire();
                return false;
            }

            startSuspend(mode);

            mode.OnEntering(this);

            ModePushed?.Invoke(mode);

            Content.Expire();

            return true;
        }

        private void startSuspend(GameMode next)
        {
            OnSuspending(next);
            Content.Expire();

            ChildGameMode = next;
        }

        /// <summary>
        /// Exits this GameMode.
        /// </summary>
        public void Exit()
        {
            if (hasExited)
                return;

            if (OnExiting(ParentGameMode))
                return;

            hasExited = true;

            Content.Expire();
            LifetimeEnd = Content.LifetimeEnd;

            ParentGameMode?.startResume(this);
            Exited?.Invoke(ParentGameMode);
            if (ParentGameMode?.ValidForResume == false)
                ParentGameMode.Exit();
            ParentGameMode = null;

            Exited = null;
            ModePushed = null;
        }

        private void startResume(GameMode last)
        {
            ChildGameMode = null;

            if (ValidForResume)
            {
                OnResuming(last);
                Content.LifetimeEnd = double.MaxValue;
            }
        }


        public void MakeCurrent()
        {
            if (IsCurrentGameMode) return;

            GameMode c;
            for (c = ChildGameMode; c.ChildGameMode != null; c = c.ChildGameMode)
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
