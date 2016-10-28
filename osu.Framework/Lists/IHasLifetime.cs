// Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

namespace osu.Framework.Lists
{
    public interface IHasLifetime
    {
        double LifetimeStart { get; }
        double LifetimeEnd { get; }

        bool IsAliveAt(double time);
        bool IsLoaded { get; }
        bool RemoveWhenNotAlive { get; }
    }
}
