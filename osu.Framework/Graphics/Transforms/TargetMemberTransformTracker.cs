// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using osu.Framework.Lists;
using osu.Framework.Timing;

namespace osu.Framework.Graphics.Transforms
{
    /// <summary>
    /// Tracks the lifetime of transforms for one specified target member.
    /// </summary>
    internal class TargetMemberTransformTracker
    {
        /// <summary>
        /// A lazily-initialized list of <see cref="Transform"/>s applied to this object.
        /// </summary>
        public IReadOnlyList<Transform> Transforms => transforms;

        /// <summary>
        /// The member this instance is tracking.
        /// </summary>
        public readonly string TargetMember;

        private readonly SortedList<Transform> transforms = new SortedList<Transform>(Transform.COMPARER);

        private readonly Transformable transformable;

        private int? lastAppliedTransformIndex;

        private readonly Lazy<List<Action>> removalActions = new Lazy<List<Action>>(() => new List<Action>());

        public TargetMemberTransformTracker(Transformable transformable, string targetMember)
        {
            TargetMember = targetMember;
            this.transformable = transformable;
        }

        public void UpdateTransforms(in double time, bool rewinding)
        {
            if (rewinding && !transformable.RemoveCompletedTransforms)
            {
                resetLastAppliedCache();

                var appliedToEndReverts = new List<string>();

                // Under the case that completed transforms are not removed, reversing the clock is permitted.
                // We need to first look back through all the transforms and apply the start values of the ones that were previously
                // applied, but now exist in the future relative to the current time.
                for (int i = transforms.Count - 1; i >= 0; i--)
                {
                    var t = transforms[i];

                    // rewind logic needs to only run on transforms which have been applied at least once.
                    if (!t.Applied)
                        continue;

                    // some specific transforms can be marked as non-rewindable.
                    if (!t.Rewindable)
                        continue;

                    if (time >= t.StartTime)
                    {
                        // we are in the middle of this transform, so we want to mark as not-completely-applied.
                        // note that we should only do this for the last transform of each TargetMember to avoid incorrect application order.
                        // the actual application will be in the main loop below now that AppliedToEnd is false.
                        if (!appliedToEndReverts.Contains(t.TargetMember))
                        {
                            if (time < t.EndTime)
                                t.AppliedToEnd = false;
                            else
                                t.Apply(t.EndTime);

                            appliedToEndReverts.Add(t.TargetMember);
                        }
                    }
                    else
                    {
                        // we are before the start time of this transform, so we want to eagerly apply the value at current time and mark as not-yet-applied.
                        // this transform will not be applied again unless we play forward in the future.
                        t.Apply(time);
                        t.Applied = false;
                        t.AppliedToEnd = false;
                    }
                }
            }

            for (int i = getLastAppliedIndex() ?? 0; i < transforms.Count; ++i)
            {
                var t = transforms[i];

                var tCanRewind = !transformable.RemoveCompletedTransforms && t.Rewindable;

                bool shouldFlushLastApplicationCache = false;

                if (time < t.StartTime)
                    break;

                if (!t.Applied)
                {
                    // This is the first time we are updating this transform.
                    // We will find other still active transforms which act on the same target member and remove them.
                    // Since following transforms acting on the same target member are immediately removed when a
                    // new one is added, we can be sure that previous transforms were added before this one and can
                    // be safely removed.
                    for (int j = getLastAppliedIndex() ?? 0; j < i; ++j)
                    {
                        var u = transforms[j];
                        if (u.TargetMember != t.TargetMember) continue;

                        if (!u.AppliedToEnd)
                            // we may have applied the existing transforms too far into the future.
                            // we want to prepare to potentially read into the newly activated transform's StartTime,
                            // so we should re-apply using its StartTime as a basis.
                            u.Apply(t.StartTime);

                        if (!tCanRewind)
                        {
                            transforms.RemoveAt(j--);
                            shouldFlushLastApplicationCache = true;
                            i--;

                            if (u.OnAbort != null)
                                removalActions.Value.Add(u.OnAbort);
                        }
                        else
                            u.AppliedToEnd = true;
                    }
                }

                if (!t.HasStartValue)
                {
                    t.ReadIntoStartValue();
                    t.HasStartValue = true;
                }

                if (!t.AppliedToEnd)
                {
                    t.Apply(time);

                    t.AppliedToEnd = time >= t.EndTime;

                    if (t.AppliedToEnd)
                    {
                        if (!tCanRewind)
                        {
                            transforms.RemoveAt(i--);
                            shouldFlushLastApplicationCache = true;
                        }

                        if (t.IsLooping)
                        {
                            if (tCanRewind)
                            {
                                t.IsLooping = false;
                                t = t.Clone();
                            }

                            t.AppliedToEnd = false;
                            t.Applied = false;
                            t.HasStartValue = false;

                            t.IsLooping = true;

                            t.StartTime += t.LoopDelay;
                            t.EndTime += t.LoopDelay;

                            // this could be added back at a lower index than where we are currently iterating, but
                            // running the same transform twice isn't a huge deal.
                            transforms.Add(t);
                            shouldFlushLastApplicationCache = true;
                        }
                        else if (t.OnComplete != null)
                            removalActions.Value.Add(t.OnComplete);
                    }
                }

                if (shouldFlushLastApplicationCache)
                    resetLastAppliedCache();
                // if this transform is applied to end, we can be sure that all previous transforms for the TargetMember have been completed.
                else if (t.AppliedToEnd)
                    setLastAppliedIndex(i + 1);
                // if not applied to end, tracking the first actively applying transform for each TargetMember is required
                // to help find the common minimum index to start processing from each next update call.
                else if (t.Applied && getLastAppliedIndex() == null)
                    setLastAppliedIndex(i);
            }

            invokePendingRemovalActions();
        }

        private void invokePendingRemovalActions()
        {
            if (removalActions.IsValueCreated && removalActions.Value.Count > 0)
            {
                var toRemove = removalActions.Value.ToArray();
                removalActions.Value.Clear();

                foreach (var action in toRemove)
                    action();
            }
        }

        /// <summary>
        /// Retrieve the last transform index that was <see cref="Transform.AppliedToEnd"/> (in <see cref="transforms"/>).
        /// </summary>
        private int? getLastAppliedIndex()
        {
            return lastAppliedTransformIndex;
        }

        /// <summary>
        /// Set the last transform index that was <see cref="Transform.AppliedToEnd"/> for a specific target member.
        /// </summary>
        /// <param name="index">The index of the transform in <see cref="transforms"/>.</param>
        private void setLastAppliedIndex(int? index = null)
        {
            lastAppliedTransformIndex = index;
        }

        /// <summary>
        /// Reset the last applied index cache completely.
        /// </summary>
        private void resetLastAppliedCache() => lastAppliedTransformIndex = null;

        /// <summary>
        /// Removes a <see cref="Transform"/>.
        /// </summary>
        /// <param name="toRemove">The <see cref="Transform"/> to remove.</param>
        public void RemoveTransform(Transform toRemove)
        {
            transforms.Remove(toRemove);
        }

        /// <summary>
        /// Used to assign a monotonically increasing ID to <see cref="Transform"/>s as they are added. This member is
        /// incremented whenever a <see cref="Transform"/> is added.
        /// </summary>
        private ulong currentTransformID;

        /// <summary>
        /// Adds to this object a <see cref="Transform"/> which was previously populated using this object via
        /// <see cref="TransformableExtensions.PopulateTransform{TValue, TEasing, TThis}"/>.
        /// Added <see cref="Transform"/>s are immediately applied, and therefore have an immediate effect on this object if the current time of this
        /// object falls within <see cref="Transform.StartTime"/> and <see cref="Transform.EndTime"/>.
        /// If <see cref="Transformable.Clock"/> is null, e.g. because this object has just been constructed, then the given transform will be finished instantaneously.
        /// </summary>
        /// <param name="transform">The <see cref="Transform"/> to be added.</param>
        /// <param name="customTransformID">When not null, the <see cref="Transform.TransformID"/> to assign for ordering.</param>
        public void AddTransform(Transform transform, ulong? customTransformID = null)
        {
            Debug.Assert(!(transform.TransformID == 0 && transforms.Contains(transform)), $"Zero-id {nameof(Transform)}s should never be contained already.");

            // This contains check may be optimized away in the future, should it become a bottleneck
            if (transform.TransformID != 0 && transforms.Contains(transform))
                throw new InvalidOperationException($"{nameof(Transformable)} may not contain the same {nameof(Transform)} more than once.");

            transform.TransformID = customTransformID ?? ++currentTransformID;
            int insertionIndex = transforms.Add(transform);
            resetLastAppliedCache();

            // Remove all existing following transforms touching the same property as this one.
            for (int i = insertionIndex + 1; i < transforms.Count; ++i)
            {
                var t = transforms[i];

                if (t.TargetMember == transform.TargetMember)
                {
                    transforms.RemoveAt(i--);
                    resetLastAppliedCache();
                    if (t.OnAbort != null)
                        removalActions.Value.Add(t.OnAbort);
                }
            }

            invokePendingRemovalActions();

            // If our newly added transform could have an immediate effect, then let's
            // make this effect happen immediately.
            if (transform.StartTime < Time.Current || transform.EndTime <= Time.Current)
                UpdateTransforms(Time.Current, !transformable.RemoveCompletedTransforms && transform.StartTime <= Time.Current);
        }

        public FrameTimeInfo Time => transformable.Time;

        /// <summary>
        /// Removes <see cref="Transform"/>s that start after <paramref name="time"/>.
        /// </summary>
        /// <param name="time">The time to clear <see cref="Transform"/>s after.</param>
        /// <param name="targetMember">
        /// An optional <see cref="Transform.TargetMember"/> name of <see cref="Transform"/>s to clear.
        /// Null for clearing all <see cref="Transform"/>s.
        /// </param>
        public virtual void ClearTransformsAfter(double time, string targetMember = null)
        {
            resetLastAppliedCache();

            var toAbort = transforms.Where(t => t.StartTime >= time).ToArray();
            transforms.RemoveAll(t => t.StartTime >= time);

            foreach (var t in toAbort)
                t.OnAbort?.Invoke();
        }

        /// <summary>
        /// Finishes specified <see cref="Transform"/>s, using their <see cref="Transform{TValue}.EndValue"/>.
        /// </summary>
        public virtual void FinishTransforms()
        {
            Func<Transform, bool> toFlushPredicate;
            if (TargetMember == null)
                toFlushPredicate = t => !t.IsLooping;
            else
                toFlushPredicate = t => !t.IsLooping && t.TargetMember == TargetMember;

            // Flush is undefined for endlessly looping transforms
            var toFlush = transforms.Where(toFlushPredicate).ToArray();

            transforms.RemoveAll(t => toFlushPredicate(t));
            resetLastAppliedCache();

            foreach (Transform t in toFlush)
            {
                t.Apply(t.EndTime);
                t.OnComplete?.Invoke();
            }
        }
    }
}
