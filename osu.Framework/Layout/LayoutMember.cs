// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using osu.Framework.Graphics;
using osu.Framework.Statistics;

namespace osu.Framework.Layout
{
    /// <summary>
    /// A member that represents a part of the layout of a <see cref="Drawable"/>.
    /// Can be invalidated according to state changes in a <see cref="Drawable"/> (via <see cref="Graphics.Invalidation"/> flags).
    /// </summary>
    public abstract class LayoutMember
    {
        /// <summary>
        /// The <see cref="Graphics.Invalidation"/> flags this <see cref="LayoutMember"/> responds to.
        /// </summary>
        public readonly Invalidation Invalidation;

        /// <summary>
        /// Any extra conditions that must be satisfied before this <see cref="LayoutMember"/> is invalidated.
        /// </summary>
        public readonly InvalidationConditionDelegate Conditions;

        /// <summary>
        /// The source of <see cref="Invalidation"/> this <see cref="LayoutMember"/> responds to.
        /// </summary>
        public readonly InvalidationSource Source;

        /// <summary>
        /// The <see cref="Drawable"/> containing this <see cref="LayoutMember"/>.
        /// </summary>
        internal Drawable Parent;

        /// <summary>
        /// Creates a new <see cref="LayoutMember"/>.
        /// </summary>
        /// <param name="invalidation">The <see cref="Graphics.Invalidation"/> flags that will invalidate this <see cref="LayoutMember"/>.</param>
        /// <param name="source">The source of the invalidation.</param>
        /// <param name="conditions">Any extra conditions that must be satisfied before this <see cref="LayoutMember"/> is invalidated.</param>
        protected LayoutMember(Invalidation invalidation, InvalidationSource source = InvalidationSource.Default, InvalidationConditionDelegate conditions = null)
        {
            Invalidation = invalidation;
            Conditions = conditions;
            Source = source;
        }

        /// <summary>
        /// Whether this <see cref="LayoutMember"/> is valid.
        /// </summary>
        public bool IsValid { get; private set; }

        /// <summary>
        /// Invalidates this <see cref="LayoutMember"/>.
        /// </summary>
        /// <returns>Whether any invalidation occurred.</returns>
        public bool Invalidate()
        {
            if (!IsValid)
                return false;

            IsValid = false;
            FrameStatistics.Increment(StatisticsCounterType.Invalidations);
            return true;
        }

        /// <summary>
        /// Validates this <see cref="LayoutMember"/>.
        /// </summary>
        protected void Validate()
        {
            if (IsValid)
                return;

            IsValid = true;
            Parent?.ValidateSuperTree(Invalidation);
            FrameStatistics.Increment(StatisticsCounterType.Refreshes);
        }
    }

    /// <summary>
    /// The delegate that provides extra conditions for an invalidation to occur.
    /// </summary>
    /// <param name="source">The <see cref="Drawable"/> to be invalidated.</param>
    /// <param name="invalidation">The <see cref="Invalidation"/> flags.</param>
    public delegate bool InvalidationConditionDelegate(Drawable source, Invalidation invalidation);
}
