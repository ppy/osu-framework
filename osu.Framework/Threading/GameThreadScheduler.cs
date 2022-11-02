// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace osu.Framework.Threading
{
    internal class GameThreadScheduler : Scheduler
    {
        public GameThreadScheduler(GameThread thread)
            : base(() => thread.IsCurrent, thread.Clock)
        {
        }
    }
}
