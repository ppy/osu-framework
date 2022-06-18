// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

namespace osu.Framework.Graphics.Performance
{
    /// <summary>
    /// The state of a <see cref="LifetimeEntry"/>.
    /// </summary>
    public enum LifetimeEntryState
    {
        /// <summary>
        /// The <see cref="LifetimeEntry"/> hasn't been processed within the <see cref="LifetimeEntryManager"/> yet.
        /// </summary>
        New,

        /// <summary>
        /// The <see cref="LifetimeEntry"/> is currently dead and becomes alive when current time &gt;= <see cref="LifetimeEntry.LifetimeStart"/>.
        /// </summary>
        Future,

        /// <summary>
        /// The <see cref="LifetimeEntry"/> is currently alive.
        /// </summary>
        Current,

        /// <summary>
        /// The <see cref="LifetimeEntry"/> is currently dead and becomes alive when current time &lt; <see cref="LifetimeEntry.LifetimeEnd"/>.
        /// </summary>
        Past,
    }
}
