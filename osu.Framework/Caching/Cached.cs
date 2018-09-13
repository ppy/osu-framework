// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

// Uncomment following line to enable checks of invalidation propagation
//#define CheckInvalidationPropagation

using osu.Framework.Statistics;
using System;
#if CheckInvalidationPropagation
using osu.Framework.Development;
using NUnit.Framework;
#endif

namespace osu.Framework.Caching
{
    public struct Cached<T>
    {
        private T value;

        public bool IsValid { get; private set; }

        public T Value
        {
            get
            {
                if (!IsValid)
                    throw new InvalidOperationException($"May not query {nameof(Value)} of an invalid {nameof(Cached<T>)}.");
                return value;
            }

            set
            {
                this.value = value;
                IsValid = true;
                FrameStatistics.Increment(StatisticsCounterType.Refreshes);
            }
        }

        /// <summary>
        /// Invalidate the cache of this object.
        /// </summary>
        /// <returns>True if we invalidated from a valid state.</returns>
        public bool Invalidate()
        {
            if (IsValid)
            {
                IsValid = false;
                FrameStatistics.Increment(StatisticsCounterType.Invalidations);
                return true;
            }

            return false;
        }

#if !CheckInvalidationPropagation
        // ReSharper disable once ValueParameterNotUsed
        public string Name { set {} }
#else
        public string Name { get; set; }
        public bool IsComputing { get; set; }
        public string GetDescription() => !string.IsNullOrEmpty(Name) ? Name : $"Cached<{typeof(T)}>";
#endif
    }

    public struct Cached
    {
        public bool IsValid { get; private set; }

        /// <summary>
        /// Invalidate the cache of this object.
        /// </summary>
        /// <returns>True if we invalidated from a valid state.</returns>
        public bool Invalidate()
        {
            if (IsValid)
            {
                IsValid = false;
                FrameStatistics.Increment(StatisticsCounterType.Invalidations);
                return true;
            }

            return false;
        }

        public void Validate()
        {
            IsValid = true;
            FrameStatistics.Increment(StatisticsCounterType.Refreshes);
        }

#if !CheckInvalidationPropagation
        // ReSharper disable once ValueParameterNotUsed
        public string Name { set {} }
#else
        public string Name { get; set; }
        public bool IsComputing { get; set; }
        public string GetDescription() => !string.IsNullOrEmpty(Name) ? Name : "Cached";
#endif
    }

    public static class CachedExtensions
    {
#if !CheckInvalidationPropagation
        /// <summary>
        /// Compute <paramref name="func"/> with caching <paramref name="cache"/>.
        /// </summary>
        public static T ComputeWith<T>(this ref Cached<T> cache, Func<T> func) =>
            cache.IsValid ? cache.Value : (cache.Value = func());

        /// <summary>
        /// Run <paramref name="action"/> if <paramref name="cache"/> is invalid.
        /// </summary>
        public static void ValidateWith(this ref Cached cache, Action action)
        {
            if (!cache.IsValid)
            {
                action();
                cache.Validate();
            }
        }
#else
        private static string checking;

        public static T ComputeWith<T>(this ref Cached<T> cache, Func<T> func)
        {
            if (cache.IsValid)
            {
                if (checking == null && ThreadSafety.IsUpdateThread)
                {
                    checking = cache.GetDescription();
                    var value = func();
                    if (cache.IsValid)
                        Assert.AreEqual(cache.Value, value, $"{checking} was not invalidated when necessary");
                    else
                        cache.Value = value;
                    checking = null;
                }

                return cache.Value;
            }
            else
            {
                if (ThreadSafety.IsUpdateThread)
                {
                    Assert.IsNull(checking, $"{checking} is depends on {cache.GetDescription()} but not properly invalidated");
                    Assert.IsFalse(cache.IsComputing, $"{cache.GetDescription()} has a circular dependency");
                }

                cache.IsComputing = true;
                var result = func();
                cache.IsComputing = false;
                return cache.Value = result;
            }
        }

        public static void ValidateWith(this ref Cached cache, Action action)
        {
            if (cache.IsValid)
            {
                if (checking == null && ThreadSafety.IsUpdateThread)
                {
                    checking = cache.GetDescription();
                    action();
                    checking = null;
                }
            }
            else
            {
                if (ThreadSafety.IsUpdateThread)
                {
                    Assert.IsNull(checking, $"{checking} is depends on {cache.GetDescription()} but not properly invalidated");
                    Assert.IsFalse(cache.IsComputing, $"{cache.GetDescription()} has a circular dependency");
                }

                cache.IsComputing = true;
                action();
                cache.IsComputing = false;
            }
            cache.Validate();
        }
#endif
    }
}
