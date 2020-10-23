// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace osu.Framework.Graphics.Performance
{
    public enum LifetimeEntryState
    {
        /// Not yet loaded.
        New,

        /// Currently dead and becomes alive in the future: current time &lt; <see cref="Drawable.LifetimeStart"/>.
        Future,

        /// Currently alive.
        Current,

        /// Currently dead and becomes alive if the clock is rewound: <see cref="Drawable.LifetimeEnd"/> &lt;= current time.
        Past,
    }
}
