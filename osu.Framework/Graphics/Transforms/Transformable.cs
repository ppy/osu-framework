// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Timing;
using osu.Framework.Utils;

namespace osu.Framework.Graphics.Transforms
{
    /// <summary>
    /// A type of object which can have <see cref="Transform"/>s operating upon it.
    /// An implementer of this class must call <see cref="UpdateTransforms"/> to
    /// update and apply its <see cref="Transform"/>s.
    /// </summary>
    public abstract class Transformable : ITransformable
    {
        /// <summary>
        /// The clock that is used to provide the timing for this object's <see cref="Transform"/>s.
        /// </summary>
        public abstract IFrameBasedClock Clock { get; set; }

        /// <summary>
        /// The current frame's time as observed by this class's <see cref="Clock"/>.
        /// </summary>
        public FrameTimeInfo Time => Clock.TimeInfo;

        /// <summary>
        /// The starting time to use for new <see cref="Transform"/>s.
        /// </summary>
        public double TransformStartTime => (Clock?.CurrentTime ?? 0) + TransformDelay;

        /// <summary>
        /// Delay from the current time until new <see cref="Transform"/>s are started, in milliseconds.
        /// </summary>
        protected double TransformDelay { get; private set; }

        /// <summary>
        /// A lazily-initialized list of <see cref="Transform"/>s applied to this object.
        /// </summary>
        public IEnumerable<Transform> Transforms => targetGroupingTrackers?.SelectMany(t => t.Transforms) ?? Array.Empty<Transform>();

        /// <summary>
        /// Retrieves the <see cref="Transform"/>s for a given target member.
        /// </summary>
        /// <param name="targetMember">The target member to find the <see cref="Transform"/>s for.</param>
        /// <returns>An enumeration over the transforms for the target member.</returns>
        public IEnumerable<Transform> TransformsForTargetMember(string targetMember) =>
            getTrackerFor(targetMember)?.Transforms ?? Enumerable.Empty<Transform>();

        /// <summary>
        /// The end time in milliseconds of the latest transform enqueued for this <see cref="Transformable"/>.
        /// Will return the current time value if no transforms are present.
        /// </summary>
        public double LatestTransformEndTime
        {
            get
            {
                //expiry should happen either at the end of the last transform or using the current sequence delay (whichever is highest).
                double max = TransformStartTime;

                if (targetGroupingTrackers != null)
                {
                    foreach (var tracker in targetGroupingTrackers)
                    {
                        for (int i = 0; i < tracker.Transforms.Count; i++)
                        {
                            var t = tracker.Transforms[i];
                            if (t.EndTime > max)
                                max = t.EndTime + 1; //adding 1ms here ensures we can expire on the current frame without issue.
                        }
                    }
                }

                return max;
            }
        }

        /// <summary>
        /// Whether to remove completed transforms from the list of applicable transforms. Setting this to false allows for rewinding transforms.
        /// </summary>
        public virtual bool RemoveCompletedTransforms { get; internal set; } = true;

        /// <summary>
        /// Resets <see cref="TransformDelay"/> and processes updates to this class based on loaded <see cref="Transform"/>s.
        /// </summary>
        protected void UpdateTransforms()
        {
            TransformDelay = 0;

            if (targetGroupingTrackers == null)
                return;

            updateTransforms(Time.Current);
        }

        private double lastUpdateTransformsTime;

        private List<TargetGroupingTransformTracker> targetGroupingTrackers;

        private TargetGroupingTransformTracker getTrackerFor(string targetMember)
        {
            if (targetGroupingTrackers != null)
            {
                foreach (var t in targetGroupingTrackers)
                {
                    if (t.TargetMembers.Contains(targetMember))
                        return t;
                }
            }

            return null;
        }

        private TargetGroupingTransformTracker getTrackerForGrouping(string targetGrouping, bool createIfNotExisting)
        {
            if (targetGroupingTrackers != null)
            {
                foreach (var t in targetGroupingTrackers)
                {
                    if (t.TargetGrouping == targetGrouping)
                        return t;
                }
            }

            if (!createIfNotExisting)
                return null;

            var tracker = new TargetGroupingTransformTracker(this, targetGrouping);

            targetGroupingTrackers ??= new List<TargetGroupingTransformTracker>();
            targetGroupingTrackers.Add(tracker);

            return tracker;
        }

        /// <summary>
        /// Process updates to this class based on loaded <see cref="Transform"/>s. This does not reset <see cref="TransformDelay"/>.
        /// This is used for performing extra updates on <see cref="Transform"/>s when new <see cref="Transform"/>s are added.
        /// </summary>
        /// <param name="time">The point in time to update transforms to.</param>
        /// <param name="forceRewindReprocess">Whether prior transforms should be reprocessed even if a rewind was not detected.</param>
        private void updateTransforms(double time, bool forceRewindReprocess = false)
        {
            if (targetGroupingTrackers == null)
                return;

            bool rewinding = lastUpdateTransformsTime > time || forceRewindReprocess;
            lastUpdateTransformsTime = time;

            // collection may grow due to abort / completion events.
            for (int i = 0; i < targetGroupingTrackers.Count; i++)
                targetGroupingTrackers[i].UpdateTransforms(time, rewinding);
        }

        /// <summary>
        /// Removes a <see cref="Transform"/>.
        /// </summary>
        /// <param name="toRemove">The <see cref="Transform"/> to remove.</param>
        public void RemoveTransform(Transform toRemove)
        {
            EnsureTransformMutationAllowed();

            getTrackerForGrouping(toRemove.TargetGrouping, false)?.RemoveTransform(toRemove);

            toRemove.TriggerAbort();
        }

        /// <summary>
        /// Clears <see cref="Transform"/>s.
        /// </summary>
        /// <param name="propagateChildren">Whether we also clear the <see cref="Transform"/>s of children.</param>
        /// <param name="targetMember">
        /// An optional <see cref="Transform.TargetMember"/> name of <see cref="Transform"/>s to clear.
        /// Null for clearing all <see cref="Transform"/>s.
        /// </param>
        public virtual void ClearTransforms(bool propagateChildren = false, string targetMember = null)
        {
            EnsureTransformMutationAllowed();

            ClearTransformsAfter(double.NegativeInfinity, propagateChildren, targetMember);
        }

        /// <summary>
        /// Removes <see cref="Transform"/>s that start after <paramref name="time"/>.
        /// </summary>
        /// <param name="time">The time to clear <see cref="Transform"/>s after.</param>
        /// <param name="propagateChildren">Whether to also clear such <see cref="Transform"/>s of children.</param>
        /// <param name="targetMember">
        /// An optional <see cref="Transform.TargetMember"/> name of <see cref="Transform"/>s to clear.
        /// Null for clearing all <see cref="Transform"/>s.
        /// </param>
        public virtual void ClearTransformsAfter(double time, bool propagateChildren = false, string targetMember = null)
        {
            if (targetGroupingTrackers == null)
                return;

            EnsureTransformMutationAllowed();

            if (targetMember != null)
            {
                getTrackerFor(targetMember)?.ClearTransformsAfter(time, targetMember);
            }
            else
            {
                // collection may grow due to abort / completion events.
                for (int i = 0; i < targetGroupingTrackers.Count; i++)
                    targetGroupingTrackers[i].ClearTransformsAfter(time);
            }
        }

        /// <summary>
        /// Applies <see cref="Transform"/>s at a point in time. This may only be called if <see cref="RemoveCompletedTransforms"/> is set to false.
        /// <para>
        /// This does not change the clock time.
        /// </para>
        /// </summary>
        /// <param name="time">The time to apply <see cref="Transform"/>s at.</param>
        /// <param name="propagateChildren">Whether to also apply children's <see cref="Transform"/>s at <paramref name="time"/>.</param>
        public virtual void ApplyTransformsAt(double time, bool propagateChildren = false)
        {
            EnsureTransformMutationAllowed();

            if (RemoveCompletedTransforms) throw new InvalidOperationException($"Cannot arbitrarily apply transforms with {nameof(RemoveCompletedTransforms)} active.");

            updateTransforms(time);
        }

        /// <summary>
        /// Finishes specified <see cref="Transform"/>s, using their <see cref="Transform{TValue}.EndValue"/>.
        /// </summary>
        /// <param name="propagateChildren">Whether we also finish the <see cref="Transform"/>s of children.</param>
        /// <param name="targetMember">
        /// An optional <see cref="Transform.TargetMember"/> name of <see cref="Transform"/>s to finish.
        /// Null for finishing all <see cref="Transform"/>s.
        /// </param>
        public virtual void FinishTransforms(bool propagateChildren = false, string targetMember = null)
        {
            if (targetGroupingTrackers == null)
                return;

            EnsureTransformMutationAllowed();

            if (targetMember != null)
            {
                getTrackerFor(targetMember)?.FinishTransforms(targetMember);
            }
            else
            {
                // Use for over foreach as collection may grow due to abort / completion events.
                // Note that this may mean that in the addition of elements being removed,
                // `FinishTransforms` may not be called on all items.
                for (int i = 0; i < targetGroupingTrackers.Count; i++)
                    targetGroupingTrackers[i].FinishTransforms();
            }
        }

        /// <summary>
        /// Add a delay duration to <see cref="TransformDelay"/>, in milliseconds.
        /// </summary>
        /// <param name="duration">The delay duration to add.</param>
        /// <param name="propagateChildren">Whether we also delay down the child tree.</param>
        /// <returns>This</returns>
        internal virtual void AddDelay(double duration, bool propagateChildren = false) => TransformDelay += duration;

        /// <summary>
        /// Start a sequence of <see cref="Transform"/>s with a (cumulative) relative delay applied.
        /// </summary>
        /// <param name="delay">The offset in milliseconds from current time. Note that this stacks with other nested sequences.</param>
        /// <param name="recursive">Whether this should be applied to all children. True by default.</param>
        /// <returns>An <see cref="InvokeOnDisposal"/> to be used in a using() statement.</returns>
        public IDisposable BeginDelayedSequence(double delay, bool recursive = true)
        {
            EnsureTransformMutationAllowed();

            if (delay == 0)
                return null;

            AddDelay(delay, recursive);
            double newTransformDelay = TransformDelay;

            return new ValueInvokeOnDisposal<DelayedSequenceSender>(new DelayedSequenceSender(this, delay, recursive, newTransformDelay), sender =>
            {
                if (!Precision.AlmostEquals(sender.NewTransformDelay, sender.Transformable.TransformDelay))
                {
                    throw new InvalidOperationException(
                        $"{nameof(sender.Transformable.TransformStartTime)} at the end of delayed sequence is not the same as at the beginning, but should be. " +
                        $"(begin={sender.NewTransformDelay} end={sender.Transformable.TransformDelay})");
                }

                AddDelay(-sender.Delay, sender.Recursive);
            });
        }

        /// An ad-hoc struct used as a closure environment in <see cref="BeginDelayedSequence" />.
        private readonly struct DelayedSequenceSender
        {
            public readonly Transformable Transformable;
            public readonly double Delay;
            public readonly bool Recursive;
            public readonly double NewTransformDelay;

            public DelayedSequenceSender(Transformable transformable, double delay, bool recursive, double newTransformDelay)
            {
                Transformable = transformable;
                Delay = delay;
                Recursive = recursive;
                NewTransformDelay = newTransformDelay;
            }
        }

        /// <summary>
        /// Start a sequence of <see cref="Transform"/>s from an absolute time value (adjusts <see cref="TransformStartTime"/>).
        /// </summary>
        /// <param name="newTransformStartTime">The new value for <see cref="TransformStartTime"/>.</param>
        /// <param name="recursive">Whether this should be applied to all children. True by default.</param>
        /// <returns>An <see cref="InvokeOnDisposal"/> to be used in a using() statement.</returns>
        /// <exception cref="InvalidOperationException">Absolute sequences should never be nested inside another existing sequence.</exception>
        public virtual IDisposable BeginAbsoluteSequence(double newTransformStartTime, bool recursive = true)
        {
            EnsureTransformMutationAllowed();

            return createAbsoluteSequenceAction(newTransformStartTime);
        }

        internal virtual void CollectAbsoluteSequenceActionsFromSubTree(double newTransformStartTime, List<AbsoluteSequenceSender> actions)
        {
            actions.Add(createAbsoluteSequenceAction(newTransformStartTime));
        }

        private AbsoluteSequenceSender createAbsoluteSequenceAction(double newTransformStartTime)
        {
            double oldTransformDelay = TransformDelay;
            double newTransformDelay = TransformDelay = newTransformStartTime - (Clock?.CurrentTime ?? 0);

            return new AbsoluteSequenceSender(this, oldTransformDelay, newTransformDelay);
        }

        /// An ad-hoc struct used as a closure environment in <see cref="BeginAbsoluteSequence" />.
        internal readonly struct AbsoluteSequenceSender : IDisposable
        {
            public readonly Transformable Sender;

            public readonly double OldTransformDelay;
            public readonly double NewTransformDelay;

            public AbsoluteSequenceSender(Transformable sender, double oldTransformDelay, double newTransformDelay)
            {
                OldTransformDelay = oldTransformDelay;
                NewTransformDelay = newTransformDelay;

                Sender = sender;
            }

            public void Dispose()
            {
                if (!Precision.AlmostEquals(NewTransformDelay, Sender.TransformDelay))
                {
                    throw new InvalidOperationException(
                        $"{nameof(Sender.TransformStartTime)} at the end of absolute sequence is not the same as at the beginning, but should be. " +
                        $"(begin={NewTransformDelay} end={Sender.TransformDelay})");
                }

                Sender.TransformDelay = OldTransformDelay;
            }
        }

        /// <summary>
        /// Adds to this object a <see cref="Transform"/> which was previously populated using this object via
        /// <see cref="TransformableExtensions.PopulateTransform{TValue, TEasing, TThis}"/>.
        /// Added <see cref="Transform"/>s are immediately applied, and therefore have an immediate effect on this object if the current time of this
        /// object falls within <see cref="Transform.StartTime"/> and <see cref="Transform.EndTime"/>.
        /// If <see cref="Clock"/> is null, e.g. because this object has just been constructed, then the given transform will be finished instantaneously.
        /// </summary>
        /// <param name="transform">The <see cref="Transform"/> to be added.</param>
        /// <param name="customTransformID">When not null, the <see cref="Transform.TransformID"/> to assign for ordering.</param>
        public void AddTransform(Transform transform, ulong? customTransformID = null)
        {
            EnsureTransformMutationAllowed();

            if (transform == null)
                throw new ArgumentNullException(nameof(transform));

            if (!ReferenceEquals(transform.TargetTransformable, this))
            {
                throw new InvalidOperationException(
                    $"{nameof(transform)} must have been populated via {nameof(TransformableExtensions)}.{nameof(TransformableExtensions.PopulateTransform)} " +
                    "using this object prior to being added.");
            }

            if (Clock == null)
            {
                if (!transform.HasStartValue)
                {
                    transform.ReadIntoStartValue();
                    transform.HasStartValue = true;
                }

                transform.Apply(transform.EndTime);
                transform.TriggerComplete();

                return;
            }

            getTrackerForGrouping(transform.TargetGrouping, true).AddTransform(transform, customTransformID);

            // If our newly added transform could have an immediate effect, then let's
            // make this effect happen immediately.
            // This is done globally instead of locally in the single member tracker
            // to keep the transformable's state consistent (e.g. with lastUpdateTransformsTime)
            if (transform.StartTime < Time.Current || transform.EndTime <= Time.Current)
                updateTransforms(Time.Current, !RemoveCompletedTransforms && transform.StartTime <= Time.Current);
        }

        /// <summary>
        /// Check whether the current thread is valid for operating on thread-safe properties.
        /// Will throw on failure.
        /// </summary>
        internal abstract void EnsureTransformMutationAllowed();
    }
}
