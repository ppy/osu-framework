// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Timing;

namespace osu.Framework.Lists
{
    public interface IHasLifetime
    {
        double LifetimeStart { get; }
        double LifetimeEnd { get; }

        bool IsAlive { get; }

        void UpdateTime(FrameTimeInfo time);

        bool IsLoaded { get; }
        bool RemoveWhenNotAlive { get; }
    }
}
