﻿// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using osu.Framework.Lists;
using osu.Framework.Timing;
using System.Linq;
using osu.Framework.Allocation;
using System.Collections.Generic;
using System.Diagnostics;
using osu.Framework.MathUtils;

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

        private SortedList<Transform> transformsLazy;

        private SortedList<Transform> transforms => transformsLazy ?? (transformsLazy = new SortedList<Transform>(Transform.COMPARER));

        /// <summary>
        /// A lazily-initialized list of <see cref="Transform"/>s applied to this object.
        /// </summary>
        public IReadOnlyList<Transform> Transforms => transforms;

        /// <summary>
        /// Resets <see cref="TransformDelay"/> and processes updates to this class based on loaded <see cref="Transform"/>s.
        /// </summary>
        protected void UpdateTransforms()
        {
            TransformDelay = 0;
            updateTransforms();
        }

        private List<Action> removalActionsLazy;
        private List<Action> removalActions => removalActionsLazy ?? (removalActionsLazy = new List<Action>());

        /// <summary>
        /// Process updates to this class based on loaded <see cref="Transform"/>s. This does not reset <see cref="TransformDelay"/>.
        /// This is used for performing extra updates on <see cref="Transform"/>s when new <see cref="Transform"/>s are added.
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
                Debug.Assert(removalActionsLazy != null);

                var toRemove = removalActionsLazy.ToArray();
                removalActionsLazy.Clear();

                foreach (var action in toRemove)
                    action();
            }
        }

        /// <summary>
        /// Removes a <see cref="Transform"/>.
        /// </summary>
        /// <param name="toRemove">The <see cref="Transform"/> to remove.</param>
        public void RemoveTransform(Transform toRemove)
        {
            if (transformsLazy == null || !transformsLazy.Remove(toRemove))
                return;

            toRemove.OnAbort?.Invoke();
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
            if (transformsLazy == null)
                return;

            Transform[] toAbort;
            if (targetMember == null)
            {
                toAbort = transformsLazy.ToArray();
                transformsLazy.Clear();
            }
            else
            {
                toAbort = transformsLazy.Where(t => t.TargetMember == targetMember).ToArray();
                transformsLazy.RemoveAll(t => t.TargetMember == targetMember);
            }

            foreach (var t in toAbort)
                t.OnAbort?.Invoke();
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
            if (transformsLazy == null)
                return;

            Func<Transform, bool> toFlushPredicate;
            if (targetMember == null)
                toFlushPredicate = t => !t.IsLooping;
            else
                toFlushPredicate = t => !t.IsLooping && t.TargetMember == targetMember;

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
        /// <param name="recursive">Whether this should be applied to all children.</param>
        /// <returns>A <see cref="InvokeOnDisposal"/> to be used in a using() statement.</returns>
        public InvokeOnDisposal BeginDelayedSequence(double delay, bool recursive = false)
        {
            if (delay == 0)
                return null;

            AddDelay(delay, recursive);
            double newTransformDelay = TransformDelay;

            return new InvokeOnDisposal(() =>
            {
                if (!Precision.AlmostEquals(newTransformDelay, TransformDelay))
                    throw new InvalidOperationException(
                        $"{nameof(TransformStartTime)} at the end of delayed sequence is not the same as at the beginning, but should be. " +
                        $"(begin={newTransformDelay} end={TransformDelay})");

                AddDelay(-delay, recursive);
            });
        }

        /// <summary>
        /// Start a sequence of <see cref="Transform"/>s from an absolute time value (adjusts <see cref="TransformStartTime"/>).
        /// </summary>
        /// <param name="newTransformStartTime">The new value for <see cref="TransformStartTime"/>.</param>
        /// <param name="recursive">Whether this should be applied to all children.</param>
        /// <returns>A <see cref="InvokeOnDisposal"/> to be used in a using() statement.</returns>
        /// <exception cref="InvalidOperationException">Absolute sequences should never be nested inside another existing sequence.</exception>
        public virtual InvokeOnDisposal BeginAbsoluteSequence(double newTransformStartTime, bool recursive = false)
        {
            double oldTransformDelay = TransformDelay;
            double newTransformDelay = TransformDelay = newTransformStartTime - (Clock?.CurrentTime ?? 0);

            return new InvokeOnDisposal(() =>
            {
                if (!Precision.AlmostEquals(newTransformDelay, TransformDelay))
                    throw new InvalidOperationException(
                        $"{nameof(TransformStartTime)} at the end of absolute sequence is not the same as at the beginning, but should be. " +
                        $"(begin={newTransformDelay} end={TransformDelay})");

                TransformDelay = oldTransformDelay;
            });
        }

        /// <summary>
        /// Used to assign a monotonically increasing ID to <see cref="Transform"/>s as they are added. This member is
        /// incremented whenever a <see cref="Transform"/> is added.
        /// </summary>
        private ulong currentTransformID;

        /// <summary>
        /// Adds to this object a <see cref="Transform"/> which was previously populated using this object via
        /// <see cref="TransformableExtensions.PopulateTransform{TValue, TThis}(TThis, Transform{TValue, TThis}, TValue, double, Easing)"/>.
        /// Added <see cref="Transform"/>s are immediately applied, and therefore have an immediate effect on this object if the current time of this
        /// object falls within <see cref="Transform.StartTime"/> and <see cref="Transform.EndTime"/>.
        /// If <see cref="Clock"/> is null, e.g. because this object has just been constructed, then the given transform will be finished instantaneously.
        /// </summary>
        /// <param name="transform">The <see cref="Transform"/> to be added.</param>
        public void AddTransform(Transform transform)
        {
            if (transform == null)
                throw new ArgumentNullException(nameof(transform));

            if (!ReferenceEquals(transform.TargetTransformable, this))
                throw new InvalidOperationException(
                    $"{nameof(transform)} must have been populated via {nameof(TransformableExtensions)}.{nameof(TransformableExtensions.PopulateTransform)} " +
                    "using this object prior to being added.");

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
