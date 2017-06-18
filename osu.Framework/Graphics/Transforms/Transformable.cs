// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using osu.Framework.Lists;
using osu.Framework.Timing;

namespace osu.Framework.Graphics.Transforms
{
    public abstract class Transformable<T>
        where T : Transformable<T>
    {
        public abstract IFrameBasedClock Clock { get; set; }

        private LifetimeList<ITransform<T>> transforms;

        /// <summary>
        /// A lazily-initialized list of <see cref="ITransform"/>s applied to this class.
        /// <see cref="ITransform"/>s are applied right before the <see cref="Update"/> method is called.
        /// </summary>
        public LifetimeList<ITransform<T>> Transforms
        {
            get
            {
                if (transforms == null)
                {
                    transforms = new LifetimeList<ITransform<T>>(new TransformTimeComparer<T>());
                    transforms.Removed += transforms_OnRemoved;
                }

                return transforms;
            }
        }

        /// <summary>
        /// The current frame's time as observed by this class's <see cref="Clock"/>.
        /// </summary>
        public FrameTimeInfo Time => Clock.TimeInfo;

        /// <summary>
        /// The time to use for starting transforms which support <see cref="Delay(double, bool)"/>
        /// </summary>
        protected double TransformStartTime => (Clock?.CurrentTime ?? 0) + TransformDelay;

        /// <summary>
        /// Process updates to this class based on loaded transforms.
        /// </summary>
        protected void updateTransforms()
        {
            if (transforms == null || transforms.Count == 0) return;

            transforms.Update(Time);

            foreach (ITransform<T> t in transforms.AliveItems)
                t.Apply((T) this);
        }

        private void transforms_OnRemoved(ITransform<T> t)
        {
            t.Apply((T) this); //make sure we apply one last time.
        }


        internal double TransformDelay { get; private set; }

        public virtual void ClearTransforms(bool propagateChildren = false)
        {
            DelayReset();
            transforms?.Clear();
        }

        public virtual T Delay(double duration, bool propagateChildren = false)
        {
            if (duration == 0) return (T) this;

            TransformDelay += duration;
            return (T) this;
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
                t.Apply((T) this);
            }

            if (flushType == null)
                ClearTransforms();
            else
                Transforms.RemoveAll(t => t.GetType() == flushType);
        }

        public virtual T DelayReset()
        {
            Delay(-TransformDelay);
            return (T) this;
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

        public void Loop(float delay = 0)
        {
            foreach (var t in Transforms)
                t.Loop(Math.Max(0, TransformDelay + delay - t.Duration));
        }

        public void TimeWarp(double change)
        {
            if (change == 0)
                return;

            foreach (ITransform<T> t in Transforms)
            {
                t.StartTime += change;
                t.EndTime += change;
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
                transform.Apply((T) this);
                return;
            }

            //we have no duration and do not need to be delayed, so we can just apply ourselves and be gone.
            bool canApplyInstant = transform.Duration == 0 && TransformDelay == 0;

            //we should also immediately apply any transforms that have already started to avoid potentially applying them one frame too late.
            if (canApplyInstant || transform.StartTime < Time.Current)
            {
                transform.UpdateTime(Time);
                transform.Apply((T) this);
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
