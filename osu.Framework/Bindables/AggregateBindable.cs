// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Collections.Generic;
using System.Linq;

namespace osu.Framework.Bindables
{
    /// <summary>
    /// Combines multiple bindables into one aggregate bindable result.
    /// </summary>
    /// <typeparam name="T">The type of values.</typeparam>
    public class AggregateBindable<T>
    {
        private readonly Func<T, T, T> aggregateFunction;

        /// <summary>
        /// The final result after aggregating all added sources.
        /// </summary>
        public IBindable<T> Result => result;

        private readonly Bindable<T> result;

        private readonly T initialValue;

        /// <summary>
        /// Create a new aggregate bindable.
        /// </summary>
        /// <param name="aggregateFunction">The function to be used for aggregation, taking two input <typeparamref name="T"/> values and returning one output.</param>
        /// <param name="resultBindable">An optional newly constructed bindable to use for <see cref="Result"/>. The initial value of this bindable is used as the initial value for the aggregate.</param>
        public AggregateBindable(Func<T, T, T> aggregateFunction, Bindable<T> resultBindable = null)
        {
            this.aggregateFunction = aggregateFunction;
            result = resultBindable ?? new Bindable<T>();
            initialValue = result.Value;
        }

        private readonly List<WeakRefPair> sourceMapping = new List<WeakRefPair>();

        /// <summary>
        /// Add a new source to be included in aggregation.
        /// </summary>
        /// <param name="bindable">The bindable to add.</param>
        public void AddSource(IBindable<T> bindable)
        {
            lock (sourceMapping)
            {
                if (findExistingPair(bindable) != null)
                    return;

                var boundCopy = bindable.GetBoundCopy();
                sourceMapping.Add(new WeakRefPair(new WeakReference<IBindable<T>>(bindable), boundCopy));
                boundCopy.BindValueChanged(recalculateAggregate, true);
            }
        }

        /// <summary>
        /// Remove a source from being included in aggregation.
        /// </summary>
        /// <param name="bindable">The bindable to remove.</param>
        public void RemoveSource(IBindable<T> bindable)
        {
            lock (sourceMapping)
            {
                var weak = findExistingPair(bindable);

                if (weak != null)
                {
                    weak.BoundCopy.UnbindAll();
                    sourceMapping.Remove(weak);
                }

                recalculateAggregate();
            }
        }

        private WeakRefPair findExistingPair(IBindable<T> bindable) =>
            sourceMapping.FirstOrDefault(p => p.WeakReference.TryGetTarget(out var target) && target == bindable);

        private void recalculateAggregate(ValueChangedEvent<T> obj = null)
        {
            T calculated = initialValue;

            lock (sourceMapping)
            {
                for (int i = 0; i < sourceMapping.Count; i++)
                {
                    var pair = sourceMapping[i];

                    if (!pair.WeakReference.TryGetTarget(out _))
                        sourceMapping.RemoveAt(i--);
                    else
                        calculated = aggregateFunction(calculated, pair.BoundCopy.Value);
                }
            }

            result.Value = calculated;
        }

        public void RemoveAllSources()
        {
            lock (sourceMapping)
            {
                foreach (var mapping in sourceMapping.ToArray())
                {
                    if (mapping.WeakReference.TryGetTarget(out var b))
                        RemoveSource(b);
                }
            }
        }

        private class WeakRefPair
        {
            public readonly WeakReference<IBindable<T>> WeakReference;
            public readonly IBindable<T> BoundCopy;

            public WeakRefPair(WeakReference<IBindable<T>> weakReference, IBindable<T> boundCopy)
            {
                WeakReference = weakReference;
                BoundCopy = boundCopy;
            }
        }
    }
}
