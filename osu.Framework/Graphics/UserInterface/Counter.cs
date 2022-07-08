// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Transforms;

namespace osu.Framework.Graphics.UserInterface
{
    /// <summary>
    /// A drawable object that supports counting to values.
    /// </summary>
    public class Counter : CompositeDrawable
    {
        private double count;

        /// <summary>
        /// The current count.
        /// </summary>
        protected double Count
        {
            get => count;
            private set
            {
                if (count == value)
                    return;

                count = value;

                OnCountChanged(count);
            }
        }

        /// <summary>
        /// Invoked when <see cref="Count"/> has changed.
        /// </summary>
        protected virtual void OnCountChanged(double count)
        {
        }

        public TransformSequence<Counter> CountTo(double endCount, double duration = 0, Easing easing = Easing.None)
            => this.TransformTo(nameof(Count), endCount, duration, easing);
    }

    public static class CounterTransformSequenceExtensions
    {
        public static TransformSequence<Counter> CountTo(this TransformSequence<Counter> t, double endCount, double duration = 0, Easing easing = Easing.None)
            => t.Append(o => o.CountTo(endCount, duration, easing));
    }
}
