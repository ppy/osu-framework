// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Statistics;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using NUnit.Framework;

namespace osu.Framework.Caching
{
    public static class StaticCached
    {
        internal static bool BypassCache = false;

        internal static bool CheckInvalidationPropagation = true;
    }

    public struct Cached<T>
    {
        private T value;

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

        private bool isValid;

        public bool IsValid => !StaticCached.BypassCache && isValid;

        public static implicit operator T(Cached<T> value) => value.Value;

        public string Name { get; set; }

        public bool IsComputing;

        /// <summary>
        /// Invalidate the cache of this object.
        /// </summary>
        /// <returns>True if we invalidated from a valid state.</returns>
        public bool Invalidate()
        {
            //Assert.IsFalse(IsComputing, $"{GetDescription()} is invalidated while computing itself");

            if (isValid)
            {
                //Console.WriteLine($"{GetDescription()} invalidated");

                isValid = false;
                FrameStatistics.Increment(StatisticsCounterType.Invalidations);
                return true;
            }

            return false;
        }

        public string GetDescription()
        {
            if (!string.IsNullOrEmpty(Name))
                return Name;
            else
                return $"Cached value of type {typeof(T)}";
        }
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
    }

    public static class CachedExtensions
    {
        private static readonly ThreadLocal<string> checking = new ThreadLocal<string>(() => null);
        private static readonly ThreadLocal<int> computing_depth = new ThreadLocal<int>(() => 0);
        private static readonly ThreadLocal<StackTrace> checking_stack_trace = new ThreadLocal<StackTrace>(() => null);

        public static T Compute<T>(this ref Cached<T> cache, Func<T> func)
        {
            if (cache.IsValid)
            {
                if (StaticCached.CheckInvalidationPropagation && checking.Value == null)
                {
                    checking.Value = cache.GetDescription();
                    //checking_stack_trace.Value = new StackTrace();

                    var value = func();

                    //Assert.IsTrue(cache.IsValid, $"{checking.Value} is invalidated while computing itself");

                    if (cache.IsValid)
                        Assert.AreEqual(cache.Value, value, $"{checking.Value} is not invalidated when necessary");

                    checking.Value = null;

                    return value;
                }
                return cache.Value;
            }
            else
            {
                if (checking.Value != null)
                {
                    Assert.Fail($"{checking.Value} is depends on {cache.GetDescription()} but not properly invalidated");
                }

                Assert.IsFalse(cache.IsComputing, $"{cache.GetDescription()} has a circular dependency");

                //Console.WriteLine($"{string.Concat(Enumerable.Repeat(' ', computing_depth.Value))}{cache.GetDescription()} computing... (");
                cache.IsComputing = true;
                computing_depth.Value += 1;

                var value = cache.Value = func();

                computing_depth.Value -= 1;
                cache.IsComputing = false;
                //Console.WriteLine($"{string.Concat(Enumerable.Repeat(' ', computing_depth.Value))}){cache.GetDescription()} computed");

                return value;
            }
        }
    }
}
