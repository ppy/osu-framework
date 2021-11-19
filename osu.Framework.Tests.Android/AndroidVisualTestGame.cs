// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Android;

namespace osu.Framework.Tests.Android
{
    internal class AndroidVisualTestGame : VisualTestGame
    {
        [Cached]
        // Allows dependency injection
        private readonly AndroidGameActivity gameActivity;

        public AndroidVisualTestGame(AndroidGameActivity gameActivity)
        {
            this.gameActivity = gameActivity;
        }
    }
}
