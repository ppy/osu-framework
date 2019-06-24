// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

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
        /// <param name="aggregateFunction">The function to be used for aggregation, taking two input <see cref="T"/> values and returning one output.</param>
        /// <param name="resultBindable">An optional newly constructed bindable to use for <see cref="Result"/>. The initial value of this bindable is used as the initial value for the aggregate.</param>
        public AggregateBindable(Func<T, T, T> aggregateFunction, Bindable<T> resultBindable = null)
        {
            this.aggregateFunction = aggregateFunction;
            result = resultBindable ?? new Bindable<T>();
            initialValue = result.Value;
        }

        private readonly Dictionary<WeakReference, IBindable<T>> sourceMapping = new Dictionary<WeakReference, IBindable<T>>();

        /// <summary>
        /// Add a new source to be included in aggregation.
        /// </summary>
        /// <param name="bindable">The bindable to add.</param>
        public void AddSource(IBindable<T> bindable)
        {
            lock (sourceMapping)
            {
                if (findExistingWeak(bindable) != null)
                    return;

                var boundCopy = bindable.GetBoundCopy();
                sourceMapping.Add(new WeakReference(bindable), boundCopy);
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
                var weak = findExistingWeak(bindable);

                if (weak != null)
                {
                    sourceMapping[weak].UnbindAll();
                    sourceMapping.Remove(weak);
                }

                recalculateAggregate();
            }
        }

        private WeakReference findExistingWeak(IBindable<T> bindable) => sourceMapping.Keys.FirstOrDefault(k => k.Target == bindable);

        private void recalculateAggregate(ValueChangedEvent<T> obj = null)
        {
            T calculated = initialValue;

            lock (sourceMapping)
            {
                foreach (var dead in sourceMapping.Keys.Where(k => !k.IsAlive).ToArray())
                    sourceMapping.Remove(dead);

                foreach (var s in sourceMapping.Values)
                    calculated = aggregateFunction(calculated, s.Value);
            }

            result.Value = calculated;
        }
    }
}
