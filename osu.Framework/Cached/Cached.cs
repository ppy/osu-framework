﻿// Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using osu.Framework.Timing;

namespace osu.Framework.Cached
{
    public class Cached<T>
    {
        public delegate T PropertyUpdater();

        /// <summary>
        /// How often this property is refreshed.
        /// </summary>
        public int RefreshInterval;

        /// <summary>
        /// Whether we allow reads of stale values. If this is set to false, there may be a potential blocking delay when accessing the property.
        /// </summary>
        public bool AllowStaleReads = true;

        private bool isStale => lastUpdateTime < 0 || (RefreshInterval >= 0 && clock?.CurrentTime > lastUpdateTime + RefreshInterval);
        public bool IsValid => !isStale;

        private PropertyUpdater updateDelegate;
        private readonly IClock clock;
        private double lastUpdateTime = -1;

        /// <summary>
        /// Create a new cached property.
        /// </summary>
        /// <param name="updateDelegate">The delegate method which will perform future updates to this property.</param>
        /// <param name="clock">The clock which will be used to decide whether we need a refresh.</param>
        /// <param name="refreshInterval">How often we should refresh this property. Set to -1 to never update. Set to 0 for once per frame.</param>
        public Cached(PropertyUpdater updateDelegate = null, IClock clock = null, int refreshInterval = -1)
        {
            RefreshInterval = refreshInterval;
            this.updateDelegate = updateDelegate;
            this.clock = clock;
        }

        public static implicit operator T(Cached<T> value)
        {
            return value.Value;
        }

        /// <summary>
        /// Refresh this cached object with a custom delegate.
        /// </summary>
        /// <param name="providedDelegate"></param>
        public T Refresh(PropertyUpdater providedDelegate)
        {
            updateDelegate = updateDelegate ?? providedDelegate;
            return MakeValidOrDefault();
        }

        /// <summary>
        /// Refresh this property.
        /// </summary>
        public T MakeValidOrDefault()
        {
            if (IsValid) return value;

            if (!EnsureValid())
                return default(T);

            return value;
        }

        /// <summary>
        /// Refresh using a cached delegate.
        /// </summary>
        /// <returns>Whether refreshing was possible.</returns>
        public bool EnsureValid()
        {
            if (IsValid) return true;

            if (updateDelegate == null)
                return false;

            value = updateDelegate();
            lastUpdateTime = clock?.CurrentTime ?? 0;
            return true;
        }

        public bool Invalidate()
        {
            if (lastUpdateTime < 0) return false;

            lastUpdateTime = -1;
            return true;
        }

        private T value;

        public T Value
        {
            get
            {
                if (!AllowStaleReads && isStale)
                    MakeValidOrDefault();

                return value;
            }

            set { throw new Exception("Can't manually update value!"); }
        }
    }
}
