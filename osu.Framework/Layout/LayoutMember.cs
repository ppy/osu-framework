// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics;
using osu.Framework.Statistics;

namespace osu.Framework.Layout
{
    public delegate bool InvalidationConditionDelegate(Drawable source, Invalidation invalidationType);

    public abstract class LayoutMember
    {
        /// <summary>
        /// The <see cref="Invalidation"/> flags this <see cref="LayoutMember"/> responds to.
        /// </summary>
        public readonly Invalidation InvalidationType;

        /// <summary>
        /// Any extra conditions that must be satisfied before this <see cref="LayoutMember"/> is invalidated.
        /// </summary>
        public readonly InvalidationConditionDelegate InvalidationCondition;

        public readonly InvalidationSource InvalidationSource;

        /// <summary>
        /// The <see cref="Drawable"/> containing this <see cref="LayoutMember"/>.
        /// </summary>
        internal Drawable Parent;

        /// <summary>
        /// Creates a new <see cref="LayoutMember"/>.
        /// </summary>
        /// <param name="invalidationType">The <see cref="Invalidation"/> flags that will invalidate this <see cref="LayoutMember"/>.</param>
        /// <param name="invalidationCondition">Any extra conditions that must be satisfied before this <see cref="LayoutMember"/> is invalidated.</param>
        /// <param name="invalidationSource">The source of the invalidation.</param>
        protected LayoutMember(Invalidation invalidationType, InvalidationConditionDelegate invalidationCondition = null, InvalidationSource invalidationSource = InvalidationSource.Default)
        {
            InvalidationType = invalidationType;
            InvalidationCondition = invalidationCondition;
            InvalidationSource = invalidationSource;
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
            Parent?.ValidateSuperTree(InvalidationType);
            FrameStatistics.Increment(StatisticsCounterType.Refreshes);
        }
    }
}
