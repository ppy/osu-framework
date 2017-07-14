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
    public abstract class Transformable : ITransformable
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
        public double TransformStartTime => (Clock?.CurrentTime ?? 0) + TransformDelay;

        /// <summary>
        /// Delay until the transformations are started, in milliseconds.
        /// </summary>
        protected double TransformDelay { get; private set; }

        private SortedList<ITransform> transformsLazy;

        private SortedList<ITransform> transforms => transformsLazy ?? (transformsLazy = new SortedList<ITransform>(new TransformTimeComparer()));

        /// <summary>
        /// A lazily-initialized list of <see cref="ITransform{T}"/>s applied to this class.
        /// </summary>
        public IReadOnlyList<ITransform> Transforms => transforms;

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
            if (transformsLazy == null)
                return;

            for (int i = 0; i < transformsLazy.Count; ++i)
            {
                var t = transformsLazy[i];

                if (t.StartTime > Time.Current)
                    break;

                if (!t.Time.HasValue)
                {
                    // this is the first time we are updating this transform with a valid time.
                    t.ReadIntoStartValue();

                    var ourType = t.GetType();

                    for (int j = 0; j < i; j++)
                    {
                        var otherTransform = transformsLazy[j];
                        if (otherTransform.GetType() == ourType)
                        {
                            transformsLazy.RemoveAt(j--);
                            i--;

                            // Trigger the abort event with the time we are behind when the abort
                            // should have happened.
                            otherTransform.OnAbort?.Invoke(Time.Current - t.StartTime);
                        }
                    }
                }

                t.UpdateTime(Time);
                t.Apply();

                if (t.EndTime <= Time.Current)
                {
                    transformsLazy.RemoveAt(i--);

                    // Trigger the completion event with the offset to the time when we the transform
                    // actually completed.
                    t.OnComplete?.Invoke(Time.Current - t.EndTime);
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
            transformsLazy?.Clear();
        }

        /// <summary>
        /// Add a delay duration to <see cref="TransformDelay"/>, in milliseconds.
        /// </summary>
        /// <param name="duration">The delay duration to add.</param>
        /// <param name="propagateChildren">Whether we also delay down the child tree.</param>
        /// <returns>This</returns>
        public virtual Transformable Delay(double duration, bool propagateChildren = false)
        {
            TransformDelay += duration;
            return this;
        }

        /// <summary>
        /// Reset <see cref="TransformDelay"/>.
        /// </summary>
        /// <returns>This</returns>
        public virtual Transformable DelayReset()
        {
            Delay(-TransformDelay);
            return this;
        }

        /// <summary>
        /// Flush specified transforms, using the last available values (ignoring current clock time).
        /// </summary>
        /// <param name="propagateChildren">Whether we also flush down the child tree.</param>
        /// <param name="flushType">An optional type of transform to flush. Null for all types.</param>
        public virtual void Flush(bool propagateChildren = false, Type flushType = null)
        {
            var operateTransforms = flushType == null ? Transforms : Transforms.Where(t => t.GetType() == flushType);

            foreach (ITransform t in operateTransforms)
            {
                t.UpdateTime(new FrameTimeInfo { Current = t.EndTime });
                t.Apply();
            }

            if (flushType == null)
                ClearTransforms();
            else
                transformsLazy.RemoveAll(t => t.GetType() == flushType);
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

        /// <summary>
        /// Warp the time for all transformations contained in <see cref="Transforms"/>.
        /// </summary>
        /// <param name="timeSpan">The time span to warp, in milliseconds.</param>
        public void TimeWarp(double timeSpan)
        {
            if (timeSpan == 0)
                return;

            foreach (ITransform t in Transforms)
            {
                t.StartTime += timeSpan;
                t.EndTime += timeSpan;
            }
        }

        /// <summary>
        /// Used to assign a monotonically increasing ID to transforms as they are added. This member is
        /// incremented whenever a transform is added.
        /// </summary>
        private ulong currentTransformID;

        public void AddTransform<T>(Transform<T> transform)
        {
            if (transform == null)
                throw new ArgumentNullException(nameof(transform));

            if (Clock == null)
            {
                transform.UpdateTime(new FrameTimeInfo { Current = transform.EndTime });
                transform.Apply();
                transform.OnComplete?.Invoke(0);
                return;
            }

            transform.TransformID = ++currentTransformID;
            transforms.Add(transform);

            // If our newly added transform could have an immediate effect, then let's
            // make this effect happen immediately.
            if (transform.StartTime < Time.Current || transform.EndTime <= Time.Current)
                updateTransforms();
        }
    }
}
