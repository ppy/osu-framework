// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Diagnostics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Layout;

namespace osu.Framework.Graphics.Pooling
{
    public class PoolableDrawable : CompositeDrawable
    {
        public override bool DisposeOnDeathRemoval => pool == null;

        /// <summary>
        /// Whether this pooled drawable is currently in use.
        /// </summary>
        public bool IsInUse { get; private set; }

        /// <summary>
        /// Whether this drawable is currently managed by a pool.
        /// </summary>
        public bool IsInPool => pool != null;

        private IDrawablePool pool;

        public void SetPool(IDrawablePool pool)
        {
            this.pool = pool;
        }

        internal void Assign()
        {
            if (IsInUse)
                throw new InvalidOperationException($"This {nameof(PoolableDrawable)} is already in use");

            Debug.Assert(pool != null);
            IsInUse = true;

            LifetimeStart = double.MinValue;
            LifetimeEnd = double.MaxValue;

            Schedule(PrepareForUse);
        }

        /// <summary>
        /// Perform any initialisation on new usage of this drawable.
        /// </summary>
        protected virtual void PrepareForUse()
        {
        }

        /// <summary>
        /// Perform any clean-up required before returning this drawable to a pool.
        /// </summary>
        protected virtual void FreeAfterUse()
        {
        }

        protected override bool OnInvalidate(Invalidation invalidation, InvalidationSource source)
        {
            if (invalidation.HasFlag(Invalidation.Parent))
            {
                if (IsInUse && Parent == null)
                {
                    FreeAfterUse();
                    pool?.Return(this);
                    IsInUse = false;
                }
            }

            return base.OnInvalidate(invalidation, source);
        }
    }
}
