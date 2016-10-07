// Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
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

        public Container Content;

        protected override Container AddTarget => Content;

        public event Action<GameMode> ModePushed;

        public event Action<GameMode> Exited;

        private bool hasExited;

        public GameMode()
        {
            RelativeSizeAxes = Axes.Both;
            Anchor = Anchor.Centre;
            Origin = Anchor.Centre;
        }

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

        protected override bool OnKeyDown(InputState state, KeyDownEventArgs args)
        {
            switch (args.Key)
            {
                case Key.Escape:
                    Exit();
                    return true;
            }

            return base.OnKeyDown(state, args);
        }

        public override void Load()
        {
            base.Load();

            AddTopLevel(Content = new ContentContainer()
            {
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre
            });

            if (ParentGameMode == null)
                OnEntering(null);
        }

        /// <summary>
        /// Changes to a new GameMode.
        /// </summary>
        /// <param name="mode">The new GameMode.</param>
        public void Push(GameMode mode)
        {
            Debug.Assert(ChildGameMode == null);

            startSuspend(mode);

            AddTopLevel(mode);
            mode.OnEntering(this);

            ModePushed?.Invoke(mode);

            Content.Expire();
        }

        private void startSuspend(GameMode next)
        {
            OnSuspending(next);
            Content.Expire();

            ChildGameMode = next;
            next.ParentGameMode = this;
        }

        /// <summary>
        /// Exits this GameMode.
        /// </summary>
        public void Exit()
        {
            Debug.Assert(ParentGameMode != null);

            if (hasExited)
                return;

            if (OnExiting(ParentGameMode))
                return;

            hasExited = true;

            Content.Expire();
            LifetimeEnd = Content.LifetimeEnd;

            ParentGameMode?.startResume(this);
            Exited?.Invoke(ParentGameMode);
            ParentGameMode = null;

            Exited = null;
            ModePushed = null;
        }

        private void startResume(GameMode last)
        {
            ChildGameMode = null;
            OnResuming(last);
            Content.LifetimeEnd = double.MaxValue;
        }


        public void MakeCurrent()
        {
            if (IsCurrentGameMode) return;

            //find deepest child
            GameMode c = ChildGameMode;
            while (c.ChildGameMode != null)
                c = c.ChildGameMode;

            //set deepest child's parent to us
            c.ParentGameMode = this;

            //exit child, making us current
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
