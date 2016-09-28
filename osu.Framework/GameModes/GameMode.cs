// Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System.Diagnostics;
using osu.Framework.Graphics.Containers;

namespace osu.Framework.GameModes
{
    public class GameMode : LargeContainer
    {
        private GameMode parentGameMode;
        private GameMode childGameMode;

        protected ContentContainer Content = new ContentContainer();

        protected override Container AddTarget => Content;

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

        public override void Load()
        {
            base.Load();
            AddTopLevel(Content);
        }

        /// <summary>
        /// Changes to a new GameMode.
        /// </summary>
        /// <param name="mode">The new GameMode.</param>
        protected void Push(GameMode mode)
        {
            Debug.Assert(childGameMode == null);

            AddTopLevel(mode);

            Content.LifetimeEnd = Time + mode.OnEntering(this);

            startSuspend(mode);
        }

        /// <summary>
        /// Exits this GameMode.
        /// </summary>
        protected void Exit()
        {
            Debug.Assert(parentGameMode != null);

            LifetimeEnd = Time + OnExiting(parentGameMode);

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
            childGameMode = next;
            next.parentGameMode = this;
        }

        protected class ContentContainer : LargeContainer
        {
            public override bool HandleInput => LifetimeEnd == double.MaxValue;
            public override bool RemoveWhenNotAlive => false;
        }
    }
}
