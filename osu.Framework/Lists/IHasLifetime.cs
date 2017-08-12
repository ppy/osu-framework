// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

namespace osu.Framework.Lists
{
    public interface IHasLifetime
    {
        /// <summary>
        /// Whether this life-timed object is currently loaded.
        /// </summary>
        bool IsLoaded { get; }

        /// <summary>
        /// The beginning of this object's lifetime.
        /// </summary>
        double LifetimeStart { get; }

        /// <summary>
        /// The end of this object's lifetime.
        /// </summary>
        double LifetimeEnd { get; }

        /// <summary>
        /// Whether this life-timed object is currently alive. Alive-status depends on
        /// <see cref="LifetimeStart"/>, <see cref="LifetimeEnd"/> and the current time.
        /// </summary>
        bool IsAlive { get; }

        /// <summary>
        /// Whether this life-timed object should be removed from its container if <see cref="IsAlive"/> is false.
        /// </summary>
        bool RemoveWhenNotAlive { get; }
    }
}
