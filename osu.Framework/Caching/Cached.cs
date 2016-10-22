// Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;

namespace osu.Framework.Caching
{
    public static class StaticCached
    {
        internal static bool AlwaysStale = false;
    }

    public struct Cached<T>
    {
        public delegate T PropertyUpdater();

        private bool isValid;

        public bool IsValid => !StaticCached.AlwaysStale && isValid;

        private PropertyUpdater updateDelegate;

        /// <summary>
        /// Create a new cached property.
        /// </summary>
        /// <param name="updateDelegate">The delegate method which will perform future updates to this property.</param>
        public Cached(PropertyUpdater updateDelegate = null)
        {
            isValid = false;
            value = default(T);
            this.updateDelegate = updateDelegate;
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
            isValid = true;
            return true;
        }

        /// <summary>
        /// Invalidate the cache of this object.
        /// </summary>
        /// <returns>True if we invalidated from a valid state.</returns>
        public bool Invalidate()
        {
            if (IsValid)
            {
                isValid = false;
                return true;
            }

            return false;
        }

        private T value;

        public T Value
        {
            get
            {
                if (!IsValid)
                    MakeValidOrDefault();

                return value;
            }

            set { throw new NotSupportedException("Can't manually update value!"); }
        }
    }

    public struct Cached
    {
        public delegate void PropertyUpdater();

        private bool isValid;

        public bool IsValid => !StaticCached.AlwaysStale && isValid;

        private PropertyUpdater updateDelegate;

        /// <summary>
        /// Create a new cached property.
        /// </summary>
        /// <param name="updateDelegate">The delegate method which will perform future updates to this property.</param>
        public Cached(PropertyUpdater updateDelegate = null)
        {
            isValid = false;
            this.updateDelegate = updateDelegate;
        }

        /// <summary>
        /// Refresh this cached object with a custom delegate.
        /// </summary>
        /// <param name="providedDelegate"></param>
        public void Refresh(PropertyUpdater providedDelegate)
        {
            updateDelegate = updateDelegate ?? providedDelegate;
            EnsureValid();
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

            updateDelegate();
            isValid = true;
            return true;
        }

        /// <summary>
        /// Invalidate the cache of this object.
        /// </summary>
        /// <returns>True if we invalidated from a valid state.</returns>
        public bool Invalidate()
        {
            if (IsValid)
            {
                isValid = false;
                return true;
            }

            return false;
        }
    }
}
