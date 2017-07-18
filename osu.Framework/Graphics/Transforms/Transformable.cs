// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using osu.Framework.Lists;
using osu.Framework.Timing;
using System.Linq;
using osu.Framework.Allocation;
using System.Collections.Generic;
using System.Diagnostics;

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
        /// The starting time to use for new transforms.
        /// </summary>
        public double TransformStartTime => (Clock?.CurrentTime ?? 0) + TransformDelay;

        /// <summary>
        /// Delay until the transformations are started, in milliseconds.
        /// </summary>
        protected double TransformDelay { get; private set; }

        private SortedList<Transform> transformsLazy;

        private SortedList<Transform> transforms => transformsLazy ?? (transformsLazy = new SortedList<Transform>(Transform.COMPARER));

        /// <summary>
        /// A lazily-initialized list of <see cref="Transform"/>s applied to this class.
        /// </summary>
        public IReadOnlyList<Transform> Transforms => transforms;

        /// <summary>
        /// Resets <see cref="TransformDelay"/> and processes updates to this class based on loaded transforms.
        /// </summary>
        protected void UpdateTransforms()
        {
            TransformDelay = 0;
            updateTransforms();
        }

        private List<Action> removalActionsLazy;
        private List<Action> removalActions => removalActionsLazy ?? (removalActionsLazy = new List<Action>());

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

                if (!t.HasStartValue)
                {
                    t.ReadIntoStartValue();
                    t.HasStartValue = true;

                    // This is the first time we are updating this transform.
                    // We will find other still active transforms which act on the same target member and remove them.
                    // Since following transforms acting on the same target member are immediately removed when a
                    // new one is added, we can be sure that previous transforms were added before this one and can
                    // be safely removed.
                    for (int j = 0; j < i; ++j)
                    {
                        var otherTransform = transformsLazy[j];
                        if (otherTransform.TargetMember == t.TargetMember)
                        {
                            transformsLazy.RemoveAt(j--);
                            i--;

                            if (otherTransform.OnAbort != null)
                                removalActions.Add(otherTransform.OnAbort);
                        }
                    }
                }

                t.Apply(Time.Current);

                if (t.EndTime <= Time.Current)
                {
                    transformsLazy.RemoveAt(i--);
                    if (t.IsLooping)
                    {
                        t.StartTime += t.LoopDelay;
                        t.EndTime += t.LoopDelay;

                        // this could be added back at a lower index than where we are currently iterating, but
                        // running the same transform twice isn't a huge deal.
                        transformsLazy.Add(t);
                    }
                    else if (t.OnComplete != null)
                        removalActions.Add(t.OnComplete);
                }
            }

            invokePendingRemovalActions();
        }

        private void invokePendingRemovalActions()
        {
            if (removalActionsLazy?.Count > 0)
            {
                var toRemove = removalActionsLazy.ToArray();
                removalActionsLazy.Clear();

                foreach (var action in toRemove)
                    action();
            }
        }

        public void RemoveTransforms(IEnumerable<Transform> toRemove)
        {
            if (transformsLazy == null)
                return;

            foreach (var t in toRemove)
                transformsLazy.Remove(t);

            foreach (var t in toRemove)
                t.OnAbort?.Invoke();
        }

        /// <summary>
        /// Clear all transforms.
        /// </summary>
        /// <param name="propagateChildren">Whether we also clear down the child tree.</param>
        public virtual void ClearTransforms(bool propagateChildren = false)
        {
            if (transformsLazy == null)
                return;

            var toAbort = transformsLazy.ToArray();
            transformsLazy.Clear();

            foreach (var t in toAbort)
                t.OnAbort?.Invoke();
        }

        /// <summary>
        /// Add a delay duration to <see cref="TransformDelay"/>, in milliseconds.
        /// </summary>
        /// <param name="duration">The delay duration to add.</param>
        /// <param name="propagateChildren">Whether we also delay down the child tree.</param>
        /// <returns>This</returns>
        internal virtual void AddDelay(double duration, bool propagateChildren = false) => TransformDelay += duration;

        /// <summary>
        /// Flush specified transforms, using the last available values (ignoring current clock time).
        /// </summary>
        /// <param name="propagateChildren">Whether we also flush down the child tree.</param>
        /// <param name="flushMember">An optional property name of transforms to flush. Null for all transforms.</param>
        public virtual void Flush(bool propagateChildren = false, string flushMember = null)
        {
            if (transformsLazy == null)
                return;

            Func<Transform, bool> toFlushPredicate;
            if (flushMember == null)
                toFlushPredicate = t => !t.IsLooping;
            else
                toFlushPredicate = t => !t.IsLooping && t.TargetMember == flushMember;

            // Flush is undefined for endlessly looping transforms
            var toFlush = transformsLazy.Where(toFlushPredicate).ToArray();

            transformsLazy.RemoveAll(t => toFlushPredicate(t));

            foreach (Transform t in toFlush)
            {
                t.Apply(t.EndTime);
                t.OnComplete?.Invoke();
            }
        }

        /// <summary>
        /// Start a sequence of transforms with a (cumulative) relative delay applied.
        /// </summary>
        /// <param name="delay">The offset in milliseconds from current time. Note that this stacks with other nested sequences.</param>
        /// <param name="recursive">Whether this should be applied to all children.</param>
        /// <returns>A <see cref="InvokeOnDisposal"/> to be used in a using() statement.</returns>
        public InvokeOnDisposal BeginDelayedSequence(double delay, bool recursive = false)
        {
            if (delay == 0)
                return null;

            AddDelay(delay, recursive);
            double newTransformStartTime = TransformStartTime;

            return new InvokeOnDisposal(() =>
            {
                if (newTransformStartTime != TransformStartTime)
                    throw new InvalidOperationException(
                        $"{nameof(TransformStartTime)} at the end of delayed sequence is not the same as at the beginning, but should be. " +
                        $"(begin={newTransformStartTime} end={TransformStartTime})");

                AddDelay(-delay, recursive);
            });
        }

        /// <summary>
        /// Start a sequence of transforms from an absolute time value.
        /// </summary>
        /// <param name="startOffset">The offset in milliseconds from absolute zero.</param>
        /// <param name="recursive">Whether this should be applied to all children.</param>
        /// <returns>A <see cref="InvokeOnDisposal"/> to be used in a using() statement.</returns>
        /// <exception cref="InvalidOperationException">Absolute sequences should never be nested inside another existing sequence.</exception>
        public virtual InvokeOnDisposal BeginAbsoluteSequence(double newTransformStartTime, bool recursive = false)
        {
            double oldTransformDelay = TransformDelay;
            TransformDelay = newTransformStartTime - (Clock?.CurrentTime ?? 0);

            return new InvokeOnDisposal(() =>
            {
                if (newTransformStartTime != TransformStartTime)
                    throw new InvalidOperationException(
                        $"{nameof(TransformStartTime)} at the end of absolute sequence is not the same as at the beginning, but should be. " +
                        $"(begin={newTransformStartTime} end={TransformStartTime})");

                TransformDelay = oldTransformDelay;
            });
        }

        /// <summary>
        /// Used to assign a monotonically increasing ID to transforms as they are added. This member is
        /// incremented whenever a transform is added.
        /// </summary>
        private ulong currentTransformID;

        public void AddTransform(Transform transform)
        {
            if (transform == null)
                throw new ArgumentNullException(nameof(transform));

            if (Clock == null)
            {
                transform.Apply(transform.EndTime);
                transform.OnComplete?.Invoke();
                return;
            }

            Debug.Assert(!(transform.TransformID == 0 && transforms.Contains(transform)), $"Zero-id {nameof(Transform)}s should never be contained already.");

            // This contains check may be optimized away in the future, should it become a bottleneck
            if (transform.TransformID != 0 && transforms.Contains(transform))
                throw new InvalidOperationException($"{nameof(Transformable)} may not contain the same {nameof(Transform)} more than once.");

            transform.TransformID = ++currentTransformID;
            int insertionIndex = transforms.Add(transform);

            // Remove all existing following transforms touching the same property at this one.
            for (int i = insertionIndex + 1; i < transformsLazy.Count; ++i)
            {
                var t = transformsLazy[i];
                if (t.TargetMember == transform.TargetMember)
                {
                    transformsLazy.RemoveAt(i--);
                    if (t.OnAbort != null)
                        removalActions.Add(t.OnAbort);
                }
            }

            invokePendingRemovalActions();

            // If our newly added transform could have an immediate effect, then let's
            // make this effect happen immediately.
            if (transform.StartTime < Time.Current || transform.EndTime <= Time.Current)
                updateTransforms();
        }
    }
}
