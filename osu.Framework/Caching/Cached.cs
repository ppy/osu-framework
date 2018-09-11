// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

// Uncomment this line to enable checks of invalidation propagation
// #define CheckInvalidationPropagation

using osu.Framework.Statistics;
using System;
#if CheckInvalidationPropagation
using NUnit.Framework;
#endif

namespace osu.Framework.Caching
{
    public static class StaticCached
    {
        internal static bool BypassCache = false;
    }

    public struct Cached<T>
    {
        private T value;

        private bool isValid;

        public bool IsValid => !StaticCached.BypassCache && isValid;

        public T Value
        {
            get
            {
                if (!isValid)
                    throw new InvalidOperationException($"May not query {nameof(Value)} of an invalid {nameof(Cached<T>)}.");
                return value;
            }

            set
            {
                this.value = value;
                isValid = true;
                FrameStatistics.Increment(StatisticsCounterType.Refreshes);
            }
        }

        /// <summary>
        /// Invalidate the cache of this object.
        /// </summary>
        /// <returns>True if we invalidated from a valid state.</returns>
        public bool Invalidate()
        {
            if (isValid)
            {
                isValid = false;
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
        private bool isValid;

        public bool IsValid => !StaticCached.BypassCache && isValid;

        /// <summary>
        /// Invalidate the cache of this object.
        /// </summary>
        /// <returns>True if we invalidated from a valid state.</returns>
        public bool Invalidate()
        {
            if (isValid)
            {
                isValid = false;
                FrameStatistics.Increment(StatisticsCounterType.Invalidations);
                return true;
            }

            return false;
        }

        public void Validate()
        {
            isValid = true;
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
        public static T Compute<T>(this ref Cached<T> cache, Func<T> func) =>
            cache.IsValid ? cache.Value : (cache.Value = func());

        public static void Compute(this ref Cached cache, Action action)
        {
            if (!cache.IsValid)
            {
                action();
                cache.Validate();
            }
        }
#else
        private static string checking;

        public static T Compute<T>(this ref Cached<T> cache, Func<T> func)
        {
            if (cache.IsValid)
            {
                if (checking == null)
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
                Assert.IsNull(checking, $"{checking} is depends on {cache.GetDescription()} but not properly invalidated");
                Assert.IsFalse(cache.IsComputing, $"{cache.GetDescription()} has a circular dependency");
                cache.IsComputing = true;
                var result = func();
                cache.IsComputing = false;
                return cache.Value = result;
            }
        }

        public static void Compute(this ref Cached cache, Action action)
        {
            if (cache.IsValid)
            {
                if (checking == null)
                {
                    checking = cache.GetDescription();
                    action();
                    checking = null;
                }
            }
            else
            {
                Assert.IsNull(checking, $"{checking} is depends on {cache.GetDescription()} but not properly invalidated");
                Assert.IsFalse(cache.IsComputing, $"{cache.GetDescription()} has a circular dependency");
                cache.IsComputing = true;
                action();
                cache.IsComputing = false;
            }
            cache.Validate();
        }
#endif
    }
}
