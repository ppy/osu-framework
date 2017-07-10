// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using osu.Framework.Lists;
using osu.Framework.Timing;
using System.Linq;
using osu.Framework.Allocation;
using System.Collections.Generic;

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

        private SortedList<ITransform<T>> transforms;
        /// <summary>
        /// A lazily-initialized list of <see cref="ITransform{T}"/>s applied to this class.
        /// </summary>
        public SortedList<ITransform<T>> Transforms => transforms ?? (transforms = new SortedList<ITransform<T>>(new TransformTimeComparer<T>()));

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

            for (int i = 0; i < transforms.Count; ++i)
            {
                var t = transforms[i];

                if (t.StartTime > Time.Current)
                    break;

                if (!t.Time.HasValue)
                {
                    // this is the first time we are updating this transform with a valid time.
                    t.ReadIntoStartValue(derivedThis);

                    var ourType = t.GetType();

                    for (int j = 0; j < i; j++)
                    {
                        if (transforms[j].GetType() == ourType)
                        {
                            transforms.RemoveAt(j--);
                            i--;
                        }
                    }
                }

                t.UpdateTime(Time);
                t.Apply(derivedThis);

                if (t.EndTime <= Time.Current)
                {
                    transforms.RemoveAt(i--);
                    if (t.HasNextIteration)
                    {
                        t.NextIteration();

                        // this could be added back at a lower index than where we are currently iterating, but
                        // running the same transform twice isn't a huge deal.
                        transforms.Add(t);
                    }
                }
            }
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
            var operateTransforms = flushType == null ? Transforms : Transforms.Where(t => t.GetType() == flushType);

            foreach (ITransform<T> t in operateTransforms)
            {
                t.UpdateTime(new FrameTimeInfo { Current = t.EndTime });
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
        /// <returns>A <see cref="InvokeOnDisposal"/> to be used in a using() statement.</returns>
        public InvokeOnDisposal BeginDelayedSequence(double delay, bool recursive = false)
        {
            Delay(delay, recursive);

            return new InvokeOnDisposal(() => Delay(-delay, recursive));
        }

        /// <summary>
        /// Start a sequence of transforms from an absolute time value.
        /// </summary>
        /// <param name="startOffset">The offset in milliseconds from absolute zero.</param>
        /// <param name="recursive">Whether this should be applied to all children.</param>
        /// <returns>A <see cref="InvokeOnDisposal"/> to be used in a using() statement.</returns>
        /// <exception cref="InvalidOperationException">Absolute sequences should never be nested inside another existing sequence.</exception>
        public InvokeOnDisposal BeginAbsoluteSequence(double startOffset = 0, bool recursive = false)
        {
            if (TransformDelay != 0) throw new InvalidOperationException($"Cannot use {nameof(BeginAbsoluteSequence)} with a non-zero transform delay already present");
            return BeginDelayedSequence(-(Clock?.CurrentTime ?? 0) + startOffset, recursive);
        }

        private bool isInLoopedSequence;

        /// <summary>
        /// Loop all transforms created within a using block of this sequence.
        /// </summary>
        /// <param name="pause">The time to pause between loop iterations. 0 by default.</param>
        /// <param name="numIterations">The amount of loop iterations to perform. A negative value results in looping indefinitely. -1 by default.</param>
        public InvokeOnDisposal BeginLoopedSequence(double pause = 0, int numIterations = -1)
        {
            if (isInLoopedSequence)
                throw new InvalidOperationException($"May not nest multiple {nameof(BeginLoopedSequence)}s.");
            isInLoopedSequence = true;

            if (pause < 0)
                throw new InvalidOperationException($"May not call {nameof(BeginLoopedSequence)} with a negative {nameof(pause)}, but was {pause}.");

            // We do not want to loop those
            HashSet<ITransform<T>> existingTransforms = new HashSet<ITransform<T>>(Transforms);

            return new InvokeOnDisposal(delegate
            {
                var newTransforms = Transforms.Except(existingTransforms).ToArray();
                isInLoopedSequence = false;

                if (newTransforms.Length == 0)
                    return;

                double duration = newTransforms.Max(t => t.EndTime) - newTransforms.Min(t => t.StartTime);
                foreach (var t in Transforms.Except(existingTransforms))
                    t.Loop(pause + duration - t.Duration, numIterations);
            });
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
        /// <param name="newValue">The value to transform to.</param>
        /// <param name="duration">The transform duration.</param>
        /// <param name="easing">The transform easing.</param>
        /// <param name="transform">The transform to use.</param>
        public void TransformTo<TValue>(TValue newValue, double duration, EasingTypes easing, Transform<TValue, T> transform) where TValue : struct, IEquatable<TValue>
        {
            //if (duration == 0 && TransformDelay == 0)
            //{
            //    // we can apply transforms instantly under certain conditions.
            //    transform.UpdateTime(new FrameTimeInfo { Current = transform.EndTime });
            //    transform.Apply(derivedThis);
            //    return;
            //}

            double startTime = TransformStartTime;

            transform.StartTime = startTime;
            transform.EndTime = startTime + duration;
            transform.EndValue = newValue;
            transform.Easing = easing;

            addTransform(transform);
        }

        private void addTransform(ITransform<T> transform)
        {
            if (transform == null)
                throw new ArgumentNullException(nameof(transform));

            if (Clock == null)
            {
                transform.UpdateTime(new FrameTimeInfo { Current = transform.EndTime });
                transform.Apply(derivedThis);
                return;
            }

            Transforms.Add(transform);

            // If our newly added transform could have an immediate effect, then let's
            // make this effect happen immediately.
            if (transform.StartTime < Time.Current || transform.EndTime <= Time.Current)
                updateTransforms();
        }
    }
}
