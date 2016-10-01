// Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System.Diagnostics;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Input;
using OpenTK.Input;

namespace osu.Framework.GameModes
{
    public class GameMode : Container
    {
        private GameMode parentGameMode;
        private GameMode childGameMode;

        protected ContentContainer Content;

        protected override Container AddTarget => Content;

        public GameMode()
        {
            RelativeSizeAxes = Axis.Both;
        }

        /// <summary>
        /// Called when this GameMode is being entered.
        /// </summary>
        /// <param name="last">The next GameMode.</param>
        /// <returns>The time after which the transition has finished running.</returns>
        protected virtual double OnEntering(GameMode last) => 0;

        /// <summary>
        /// Called when this GameMode is exiting.
        /// </summary>
        /// <param name="next">The next GameMode.</param>
        /// <returns>The time after which the transition has finished running.</returns>
        protected virtual double OnExiting(GameMode next) => 0;

        /// <summary>
        /// Called when this GameMode is being returned to from a child exiting.
        /// </summary>
        /// <param name="last">The next GameMode.</param>
        /// <returns>The time after which the transition has finished running.</returns>
        protected virtual double OnResuming(GameMode last) => 0;

        /// <summary>
        /// Called when this GameMode is being left to a new child.
        /// </summary>
        /// <param name="next">The new GameMode</param>
        /// <returns>The time after which the transition has finished running.</returns>
        protected virtual double OnSuspending(GameMode next) => 0;

        protected override bool OnKeyDown(InputState state, KeyDownEventArgs args)
        {
            switch (args.Key)
            {
                case Key.Escape:
                    if (parentGameMode == null) return false;

                    Exit();
                    return true;
            }

            return base.OnKeyDown(state, args);
        }

        public override void Load()
        {
            base.Load();

            Anchor = Anchor.Centre;
            Origin = Anchor.Centre;

            AddTopLevel(Content = new ContentContainer()
            {
                Depth = float.MinValue,
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre
            });

            if (parentGameMode == null)
                OnEntering(null);
        }

        /// <summary>
        /// Changes to a new GameMode.
        /// </summary>
        /// <param name="mode">The new GameMode.</param>
        protected void Push(GameMode mode)
        {
            Debug.Assert(childGameMode == null);

            startSuspend(mode);

            AddTopLevel(mode);
            mode.OnEntering(this);
            
            Content.Expire();
        }

        /// <summary>
        /// Exits this GameMode.
        /// </summary>
        protected void Exit()
        {
            Debug.Assert(parentGameMode != null);

            OnExiting(parentGameMode);
            Content.Expire();
            LifetimeEnd = Content.LifetimeEnd;

            parentGameMode.startResume(this);
            parentGameMode = null;
        }

        private void startResume(GameMode last)
        {
            childGameMode = null;
            OnResuming(last);
            Content.LifetimeEnd = double.MaxValue;
        }

        private void startSuspend(GameMode next)
        {
            OnSuspending(next);
            Content.Expire();

            childGameMode = next;
            next.parentGameMode = this;
        }

        protected class ContentContainer : Container
        {
            public override bool HandleInput => LifetimeEnd == double.MaxValue;
            public override bool RemoveWhenNotAlive => false;

            public ContentContainer()
            {
                RelativeSizeAxes = Axis.Both;
            }
        }
    }
}
