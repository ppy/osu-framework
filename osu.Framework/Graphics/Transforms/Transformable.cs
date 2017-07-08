// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using osu.Framework.Lists;
using osu.Framework.Timing;

namespace osu.Framework.Graphics.Transforms
{
    /// <summary>
    /// A type of object which can have transforms attached to it.
    /// An implementer of this class must call <see cref="UpdateTransforms"/> to update the transforms.
    /// </summary>
    public abstract class Transformable<T>
        where T : Transformable<T>
    {
        /// <summary>
        /// The clock that is used to provide the timing for the transforms.
        /// </summary>
        public abstract IFrameBasedClock Clock { get; set; }

        /// <summary>
        /// The current frame's time as observed by this class's <see cref="Clock"/>.
        /// </summary>
        public FrameTimeInfo Time => Clock.TimeInfo;

        /// <summary>
        /// The time to use for starting transforms which support <see cref="Delay(double, bool)"/>
        /// </summary>
        protected double TransformStartTime => (Clock?.CurrentTime ?? 0) + TransformDelay;

        /// <summary>
        /// Delay until the transformations are started, in milliseconds.
        /// </summary>
        protected double TransformDelay { get; private set; }

        private LifetimeList<ITransform<T>> transforms;
        /// <summary>
        /// A lazily-initialized list of <see cref="ITransform{T}"/>s applied to this class.
        /// </summary>
        public LifetimeList<ITransform<T>> Transforms
        {
            get
            {
                if (transforms == null)
                {
                    transforms = new LifetimeList<ITransform<T>>(new TransformTimeComparer<T>());

                    // Apply transforms one last time when they're removed
                    transforms.Removed += t => t.Apply(derivedThis);
                }

                return transforms;
            }
        }

        /// <summary>
        /// We will need to pass in the derived version of ourselves in various methods below (including <see cref="ITransform{T}.Apply(T)"/>)
        /// however this is both messy and may potentially have a performance overhead. So a local casted reference is kept to avoid this.
        /// </summary>
        private readonly T derivedThis;

        protected Transformable()
        {
            derivedThis = (T)this;
        }

        /// <summary>
        /// Resets <see cref="TransformDelay"/> and processes updates to this class based on loaded transforms.
        /// </summary>
        protected void UpdateTransforms()
        {
            TransformDelay = 0;
            updateTransforms();
        }

        /// <summary>
        /// Process updates to this class based on loaded transforms. This does not reset <see cref="TransformDelay"/>.
        /// This is used for performing extra updates on transforms when new transforms are added.
        /// </summary>
        private void updateTransforms()
        {
            if (transforms == null || transforms.Count == 0)
                return;

            transforms.Update(Time);

            // We iterate by index to gain performance
            var aliveTransforms = transforms.AliveItems;
            // ReSharper disable once ForCanBeConvertedToForeach
            for (int i = 0; i < aliveTransforms.Count; ++i)
                aliveTransforms[i].Apply(derivedThis);
        }

        /// <summary>
        /// Clear all transformations and resets <see cref="TransformDelay"/>.
        /// </summary>
        /// <param name="propagateChildren">Whether we also clear down the child tree.</param>
        public virtual void ClearTransforms(bool propagateChildren = false)
        {
            DelayReset();
            transforms?.Clear();
        }

        /// <summary>
        /// Add a delay duration to <see cref="TransformDelay"/>, in milliseconds.
        /// </summary>
        /// <param name="duration">The delay duration to add.</param>
        /// <param name="propagateChildren">Whether we also delay down the child tree.</param>
        /// <returns>This</returns>
        public virtual T Delay(double duration, bool propagateChildren = false)
        {
            if (duration == 0) return derivedThis;

            TransformDelay += duration;
            return derivedThis;
        }

        /// <summary>
        /// Reset <see cref="TransformDelay"/>.
        /// </summary>
        /// <returns>This</returns>
        public virtual T DelayReset()
        {
            Delay(-TransformDelay);
            return derivedThis;
        }

        /// <summary>
        /// Flush specified transforms, using the last available values (ignoring current clock time).
        /// </summary>
        /// <param name="propagateChildren">Whether we also flush down the child tree.</param>
        /// <param name="flushType">An optional type of transform to flush. Null for all types.</param>
        public virtual void Flush(bool propagateChildren = false, Type flushType = null)
        {
            var operateTransforms = flushType == null ? Transforms : Transforms.FindAll(t => t.GetType() == flushType);

            double maxTime = double.MinValue;
            foreach (ITransform<T> t in operateTransforms)
                if (t.EndTime > maxTime)
                    maxTime = t.EndTime;

            FrameTimeInfo maxTimeInfo = new FrameTimeInfo { Current = maxTime };

            foreach (ITransform<T> t in operateTransforms)
            {
                t.UpdateTime(maxTimeInfo);
                t.Apply(derivedThis);
            }

            if (flushType == null)
                ClearTransforms();
            else
                Transforms.RemoveAll(t => t.GetType() == flushType);
        }

        /// <summary>
        /// Start a sequence of transforms with a (cumulative) relative delay applied.
        /// </summary>
        /// <param name="delay">The offset in milliseconds from current time. Note that this stacks with other nested sequences.</param>
        /// <param name="recursive">Whether this should be applied to all children.</param>
        /// <returns>A <see cref="TransformSequence" /> to be used in a using() statement.</returns>
        public TransformSequence BeginDelayedSequence(double delay, bool recursive = false) => new TransformSequence(this, delay, recursive);

        /// <summary>
        /// Start a sequence of transforms from an absolute time value.
        /// </summary>
        /// <param name="startOffset">The offset in milliseconds from absolute zero.</param>
        /// <param name="recursive">Whether this should be applied to all children.</param>
        /// <returns>A <see cref="TransformSequence" /> to be used in a using() statement.</returns>
        /// <exception cref="InvalidOperationException">Absolute sequences should never be nested inside another existing sequence.</exception>
        public TransformSequence BeginAbsoluteSequence(double startOffset = 0, bool recursive = false)
        {
            if (TransformDelay != 0) throw new InvalidOperationException($"Cannot use {nameof(BeginAbsoluteSequence)} with a non-zero transform delay already present");
            return new TransformSequence(this, -(Clock?.CurrentTime ?? 0) + startOffset, recursive);
        }

        /// <summary>
        /// Set the loop interval of all transformations contained in <see cref="Transforms"/>.
        /// </summary>
        /// <param name="delay">The loop interval to set, in milliseconds.</param>
        public void Loop(float delay = 0)
        {
            foreach (var t in Transforms)
                t.Loop(Math.Max(0, TransformDelay + delay - t.Duration));
        }

        /// <summary>
        /// Warp the time for all transformations contained in <see cref="Transforms"/>.
        /// </summary>
        /// <param name="timeSpan">The time span to warp, in milliseconds.</param>
        public void TimeWarp(double timeSpan)
        {
            if (timeSpan == 0)
                return;

            foreach (ITransform<T> t in Transforms)
            {
                t.StartTime += timeSpan;
                t.EndTime += timeSpan;
            }
        }

        /// <summary>
        /// Applies a transform to this object.
        /// </summary>
        /// <typeparam name="TValue">The value type upon which the transform acts.</typeparam>
        /// <param name="currentValue">A function to get the current value to transform from.</param>
        /// <param name="newValue">The value to transform to.</param>
        /// <param name="duration">The transform duration.</param>
        /// <param name="easing">The transform easing.</param>
        /// <param name="transform">The transform to use.</param>
        public void TransformTo<TValue>(Func<TValue> currentValue, TValue newValue, double duration, EasingTypes easing, Transform<TValue, T> transform) where TValue : struct, IEquatable<TValue>
        {
            Type type = transform.GetType();

            double startTime = TransformStartTime;

            //For simplicity let's just update *all* transforms.
            //The commented (more optimised code) below doesn't consider past "removed" transforms, which can cause discrepancies.
            updateTransforms();

            //foreach (ITransform t in Transforms.AliveItems)
            //    if (t.GetType() == type)
            //        t.Apply(this);

            TValue startValue = currentValue();

            if (TransformDelay == 0)
            {
                Transforms.RemoveAll(t => t.GetType() == type);

                if (startValue.Equals(newValue))
                    return;
            }
            else
            {
                var last = Transforms.FindLast(t => t.GetType() == type) as Transform<TValue, T>;
                if (last != null)
                {
                    //we may be in the middle of an existing transform, so let's update it to the start time of our new transform.
                    last.UpdateTime(new FrameTimeInfo { Current = startTime });
                    startValue = last.CurrentValue;
                }
            }

            transform.StartTime = startTime;
            transform.EndTime = startTime + duration;
            transform.StartValue = startValue;
            transform.EndValue = newValue;
            transform.Easing = easing;

            addTransform(transform);
        }

        private void addTransform(ITransform<T> transform)
        {
            if (Clock == null)
            {
                transform.UpdateTime(new FrameTimeInfo { Current = transform.EndTime });
                transform.Apply(derivedThis);
                return;
            }

            //we have no duration and do not need to be delayed, so we can just apply ourselves and be gone.
            bool canApplyInstant = transform.Duration == 0 && TransformDelay == 0;

            //we should also immediately apply any transforms that have already started to avoid potentially applying them one frame too late.
            if (canApplyInstant || transform.StartTime < Time.Current)
            {
                transform.UpdateTime(Time);
                transform.Apply(derivedThis);
                if (canApplyInstant)
                    return;
            }

            Transforms.Add(transform);
        }

        /// <summary>
        /// A disposable-pattern object to handle isolated sequences of transforms. Should only be used in using blocks.
        /// </summary>
        public class TransformSequence : IDisposable
        {
            private readonly Transformable<T> us;
            private readonly bool recursive;
            private readonly double adjust;

            public TransformSequence(Transformable<T> us, double adjust, bool recursive = false)
            {
                this.recursive = recursive;
                this.us = us;
                this.adjust = adjust;

                us.Delay(adjust, recursive);
            }

            #region IDisposable Support
            private bool disposed;

            protected virtual void Dispose(bool disposing)
            {
                if (!disposed)
                {
                    us.Delay(-adjust, recursive);
                    disposed = true;
                }
            }

            ~TransformSequence()
            {
                Dispose(false);
            }

            public void Dispose()
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }
            #endregion
        }
    }
}
