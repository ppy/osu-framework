// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics;
using osu.Framework.Statistics;

namespace osu.Framework.Layout
{
    public delegate bool InvalidationConditionDelegate(Drawable source, Invalidation invalidationType);

    public abstract class LayoutMember
    {
        public readonly Invalidation InvalidationType;
        public readonly InvalidationConditionDelegate InvalidationCondition;

        internal Drawable Parent;

        protected LayoutMember(Invalidation invalidationType, InvalidationConditionDelegate invalidationCondition = null)
        {
            InvalidationType = invalidationType;
            InvalidationCondition = invalidationCondition;
        }

        public bool IsValid { get; private set; }

        public bool Invalidate()
        {
            if (!IsValid)
                return false;

            IsValid = false;
            FrameStatistics.Increment(StatisticsCounterType.Invalidations);
            return true;
        }

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
