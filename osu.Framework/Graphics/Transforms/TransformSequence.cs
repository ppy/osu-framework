// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Collections.Generic;

namespace osu.Framework.Graphics.Transforms
{
    /// <summary>
    /// A sequence of <see cref="Transform"/>s all operating upon the same <see cref="ITransformable"/>
    /// of type <typeparamref name="T"/>.
    /// Exposes various operations to extend the sequence by additional <see cref="Transforms"/> such as
    /// delays, loops, continuations, and events.
    /// </summary>
    /// <typeparam name="T">
    /// The type of the <see cref="ITransformable"/> the <see cref="Transform"/>s in this sequence operate upon.
    /// </typeparam>
    public class TransformSequence<T> : ITransformSequence where T : class, ITransformable
    {
        /// <summary>
        /// A delegate that generates a new <see cref="TransformSequence{T}"/> on a given <paramref name="origin"/>.
        /// </summary>
        /// <param name="origin">The origin to generate a <see cref="TransformSequence{T}"/> for.</param>
        /// <returns>The generated <see cref="TransformSequence{T}"/>.</returns>
        public delegate TransformSequence<T> Generator(T origin);

        private readonly T origin;

        private readonly List<Transform> transforms = new List<Transform>(1); // the most common usage of transforms sees one transform being added.

        private bool hasCompleted = true;

        private readonly double startTime;
        private double currentTime;
        private double endTime => Math.Max(currentTime, lastEndTime);

        private Transform last;
        private double lastEndTime;

        private bool hasEnd => lastEndTime != double.PositiveInfinity;

        /// <summary>
        /// Creates a new empty <see cref="TransformSequence{T}"/> attached to a given <paramref name="origin"/>.
        /// </summary>
        /// <param name="origin">The <typeparamref name="T"/> to attach the new <see cref="TransformSequence{T}"/> to.</param>
        public TransformSequence(T origin)
        {
            if (origin == null)
                throw new ArgumentNullException(nameof(origin), $"May not create a {nameof(TransformSequence<T>)} with a null {nameof(origin)}.");

            this.origin = origin;
            startTime = currentTime = lastEndTime = origin.TransformStartTime;
        }

        private void onLoopingTransform()
        {
            // As soon as we have an infinitely looping transform,
            // completion no longer makes sense.
            if (last != null)
                last.CompletionTargetSequence = null;

            last = null;
            lastEndTime = double.PositiveInfinity;
            hasCompleted = false;
        }

        public TransformSequence<T> TransformTo<TValue>(string propertyOrFieldName, TValue newValue, double duration = 0, Easing easing = Easing.None) =>
            Append(o => o.TransformTo(propertyOrFieldName, newValue, duration, easing));

        /// <summary>
        /// Adds an existing <see cref="Transform"/> operating on <see cref="origin"/> to this <see cref="TransformSequence{T}"/>.
        /// </summary>
        /// <param name="transform">The <see cref="Transform"/> to add.</param>
        internal void Add(Transform transform)
        {
            if (!ReferenceEquals(transform.TargetTransformable, origin))
            {
                throw new InvalidOperationException(
                    $"{nameof(transform)} must operate upon {nameof(origin)}={origin}, but operates upon {transform.TargetTransformable}.");
            }

            transforms.Add(transform);

            transform.CompletionTargetSequence = null;
            transform.AbortTargetSequence = this;

            if (transform.IsLooping)
                onLoopingTransform();

            // Update last transform for completion callback
            if (last == null || transform.EndTime > lastEndTime)
            {
                if (last != null)
                    last.CompletionTargetSequence = null;

                last = transform;
                last.CompletionTargetSequence = this;
                lastEndTime = last.EndTime;
                hasCompleted = false;
            }
        }

        /// <summary>
        /// Appends multiple <see cref="Generator"/>s to this <see cref="TransformSequence{T}"/>.
        /// </summary>
        /// <param name="childGenerators">The <see cref="Generator"/>s to be appended.</param>
        /// <returns>This <see cref="TransformSequence{T}"/>.</returns>
        public TransformSequence<T> Append(IEnumerable<Generator> childGenerators)
        {
            foreach (var p in childGenerators)
                Append(p);

            return this;
        }

        /// <summary>
        /// Appends a <see cref="Generator"/>s to this <see cref="TransformSequence{T}"/>.
        /// The <see cref="Generator"/> is invoked within a <see cref="Transformable.BeginAbsoluteSequence(double, bool)"/>
        /// such that the generated <see cref="TransformSequence{T}"/> starts at the correct point in time.
        /// Its <see cref="Transform"/>s are then merged into this <see cref="TransformSequence{T}"/>.
        /// </summary>
        /// <param name="childGenerator">The <see cref="Generator"/> to be appended.</param>
        /// <returns>This <see cref="TransformSequence{T}"/>.</returns>
        public TransformSequence<T> Append(Generator childGenerator)
        {
            TransformSequence<T> child;
            using (origin.BeginAbsoluteSequence(currentTime, false))
                child = childGenerator(origin);

            if (!ReferenceEquals(child.origin, origin))
                throw new InvalidOperationException($"May not append {nameof(TransformSequence<T>)} with different origin.");

            var oldLast = last;
            foreach (var t in child.transforms)
                Add(t);

            // If we flatten a child into ourselves that already completed, then
            // we need to make sure to update the hasCompleted value, too, since
            // the already completed final transform will no longer fire any events.
            if (oldLast != last)
                hasCompleted = child.hasCompleted;

            return this;
        }

        /// <summary>
        /// Invokes <paramref name="originFunc"/> inside a <see cref="Transformable.BeginAbsoluteSequence(double, bool)"/>
        /// such that <see cref="ITransformable.TransformStartTime"/> is the current time of this <see cref="TransformSequence{T}"/>.
        /// It is the responsibility of <paramref name="originFunc"/> to make appropriate use of <see cref="ITransformable.TransformStartTime"/>.
        /// </summary>
        /// <typeparam name="TResult">The return type of <paramref name="originFunc"/>.</typeparam>
        /// <param name="originFunc">The function to be invoked.</param>
        /// <param name="result">The resulting value of the invocation of <paramref name="originFunc"/>.</param>
        /// <returns>This <see cref="TransformSequence{T}"/>.</returns>
        public TransformSequence<T> Append<TResult>(Func<T, TResult> originFunc, out TResult result)
        {
            using (origin.BeginAbsoluteSequence(currentTime, false))
                result = originFunc(origin);

            return this;
        }

        /// <summary>
        /// Invokes <paramref name="originAction"/> inside a <see cref="Transformable.BeginAbsoluteSequence(double, bool)"/>
        /// such that <see cref="ITransformable.TransformStartTime"/> is the current time of this <see cref="TransformSequence{T}"/>.
        /// It is the responsibility of <paramref name="originAction"/> to make appropriate use of <see cref="ITransformable.TransformStartTime"/>.
        /// </summary>
        /// <param name="originAction">The function to be invoked.</param>
        /// <returns>This <see cref="TransformSequence{T}"/>.</returns>
        public TransformSequence<T> Append(Action<T> originAction)
        {
            using (origin.BeginAbsoluteSequence(currentTime, false))
                originAction(origin);

            return this;
        }

        private void subscribeComplete(Action func)
        {
            if (onComplete != null)
            {
                throw new InvalidOperationException(
                    "May not subscribe completion multiple times." +
                    $"This exception is also caused by calling {nameof(Then)} or {nameof(Finally)} on an infinitely looping {nameof(TransformSequence<T>)}.");
            }

            onComplete = func;

            // Completion can be immediately triggered by instant transforms,
            // and therefore when subscribing we need to take into account
            // potential previous completions.
            if (hasCompleted)
                func();
        }

        private void subscribeAbort(Action func)
        {
            if (onAbort != null)
                throw new InvalidOperationException("May not subscribe abort multiple times.");

            // No need to worry about new transforms immediately aborting, so
            // we can just subscribe here and be sure abort couldn't have been
            // triggered already.
            onAbort = func;
        }

        private Action onComplete;
        private Action onAbort;

        /// <summary>
        /// Append a looping <see cref="TransformSequence{T}"/> to this <see cref="TransformSequence{T}"/>.
        /// All <see cref="Transform"/>s generated by <paramref name="childGenerators"/> are appended to
        /// this <see cref="TransformSequence{T}"/> and then repeated <paramref name="numIters"/> times
        /// with <paramref name="pause"/> milliseconds between iterations.
        /// </summary>
        /// <param name="pause">The pause between iterations in milliseconds.</param>
        /// <param name="numIters">The number of iterations.</param>
        /// <param name="childGenerators">The functions to generate the <see cref="TransformSequence{T}"/>s to be looped.</param>
        /// <returns>This <see cref="TransformSequence{T}"/>.</returns>
        public TransformSequence<T> Loop(double pause, int numIters, params Generator[] childGenerators)
        {
            Append(o =>
            {
                var childSequence = new TransformSequence<T>(o);
                childSequence.Append(childGenerators);
                childSequence.Loop(pause, numIters);
                return childSequence;
            });

            return this;
        }

        /// <summary>
        /// Repeats all <see cref="Transform"/>s within this <see cref="TransformSequence{T}"/>
        /// <paramref name="numIters"/> times with <paramref name="pause"/> milliseconds between iterations.
        /// </summary>
        /// <param name="pause">The pause between iterations in milliseconds.</param>
        /// <param name="numIters">The number of iterations.</param>
        /// <returns>This <see cref="TransformSequence{T}"/>.</returns>
        public TransformSequence<T> Loop(double pause, int numIters)
        {
            if (numIters < 1)
                throw new InvalidOperationException($"May not {nameof(Loop)} for fewer than 1 iteration ({numIters} attempted).");

            if (!hasEnd)
                throw new InvalidOperationException($"Can not perform {nameof(Loop)} on an endless {nameof(TransformSequence<T>)}.");

            double iterDuration = endTime - startTime + pause;
            var toLoop = transforms.ToArray();

            // Duplicate existing transforms numIters times
            for (int i = 1; i < numIters; ++i)
            {
                foreach (var t in toLoop)
                {
                    var clone = t.Clone();

                    clone.StartTime += i * iterDuration;
                    clone.EndTime += i * iterDuration;

                    clone.AppliedToEnd = false;
                    clone.Applied = false;

                    Add(clone);
                    t.TargetTransformable.AddTransform(clone);
                }
            }

            return this;
        }

        /// <summary>
        /// Append a looping <see cref="TransformSequence{T}"/> to this <see cref="TransformSequence{T}"/>.
        /// All <see cref="Transform"/>s generated by <paramref name="childGenerators"/> are appended to
        /// this <see cref="TransformSequence{T}"/> and then repeated indefinitely.
        /// </summary>
        /// <param name="childGenerators">The functions to generate the <see cref="TransformSequence{T}"/>s to be looped.</param>
        /// <returns>This <see cref="TransformSequence{T}"/>.</returns>
        public TransformSequence<T> Loop(params Generator[] childGenerators) => Loop(0, childGenerators);

        /// <summary>
        /// Append a looping <see cref="TransformSequence{T}"/> to this <see cref="TransformSequence{T}"/>.
        /// All <see cref="Transform"/>s generated by <paramref name="childGenerators"/> are appended to
        /// this <see cref="TransformSequence{T}"/> and then repeated indefinitely with <paramref name="pause"/>
        /// milliseconds between iterations.
        /// </summary>
        /// <param name="pause">The pause between iterations in milliseconds.</param>
        /// <param name="childGenerators">The functions to generate the <see cref="TransformSequence{T}"/>s to be looped.</param>
        /// <returns>This <see cref="TransformSequence{T}"/>.</returns>
        public TransformSequence<T> Loop(double pause, params Generator[] childGenerators)
        {
            Append(o =>
            {
                var childSequence = new TransformSequence<T>(o);
                childSequence.Append(childGenerators);
                childSequence.Loop(pause);
                return childSequence;
            });

            return this;
        }

        /// <summary>
        /// Repeats all <see cref="Transform"/>s within this <see cref="TransformSequence{T}"/> indefinitely.
        /// </summary>
        /// <param name="pause">The pause between iterations in milliseconds.</param>
        /// <returns>This <see cref="TransformSequence{T}"/>.</returns>
        public TransformSequence<T> Loop(double pause = 0)
        {
            if (!hasEnd)
                throw new InvalidOperationException($"Can not perform {nameof(Loop)} on an endless {nameof(TransformSequence<T>)}.");

            double iterDuration = endTime - startTime + pause;

            foreach (var t in transforms)
            {
                var tmpOnAbort = t.AbortTargetSequence;
                t.AbortTargetSequence = null;
                t.TargetTransformable.RemoveTransform(t);
                t.AbortTargetSequence = tmpOnAbort;

                // Update start and end times such that no transformations need to be instantly
                // looped right after they're added. This is required so that transforms can be
                // inserted in the correct order such that none of them trigger abortions on
                // each other due to instant re-sorting upon adding.
                double currentTransformTime = t.TargetTransformable.Time.Current;

                while (t.EndTime <= currentTransformTime)
                {
                    t.StartTime += iterDuration;
                    t.EndTime += iterDuration;
                }
            }

            // This sort is required such that no abortions happen.
            var sortedTransforms = new List<Transform>(transforms);
            sortedTransforms.Sort(Transform.COMPARER);

            foreach (var t in sortedTransforms)
            {
                t.IsLooping = true;
                t.LoopDelay = iterDuration;

                t.Applied = false;
                t.AppliedToEnd = false; // we want to force a reprocess of this transform. it may have been applied-to-end in the Add, but not correctly looped as a result.

                t.TargetTransformable.AddTransform(t, t.TransformID);
            }

            onLoopingTransform();
            return this;
        }

        /// <summary>
        /// Advances the start time of future appended <see cref="TransformSequence{T}"/>s to the latest end time of all
        /// <see cref="Transform"/>s in this <see cref="TransformSequence{T}"/>.
        /// Then, <paramref name="childGenerators"/> are appended via <see cref="Append(IEnumerable{Generator})"/>.
        /// </summary>
        /// <param name="childGenerators">The optional <see cref="Generator"/>s for <see cref="TransformSequence{T}"/>s to be appended.</param>
        /// <returns>This <see cref="TransformSequence{T}"/>.</returns>
        public TransformSequence<T> Then(params Generator[] childGenerators) => Then(0, childGenerators);

        /// <summary>
        /// Advances the start time of future appended <see cref="TransformSequence{T}"/>s to the latest end time of all
        /// <see cref="Transform"/>s in this <see cref="TransformSequence{T}"/> plus <paramref name="delay"/> milliseconds.
        /// Then, <paramref name="childGenerators"/> are appended via <see cref="Append(IEnumerable{Generator})"/>.
        /// </summary>
        /// <param name="delay">The delay after the latest end time of all <see cref="Transform"/>s.</param>
        /// <param name="childGenerators">The optional <see cref="Generator"/>s for <see cref="TransformSequence{T}"/>s to be appended.</param>
        /// <returns>This <see cref="TransformSequence{T}"/>.</returns>
        public TransformSequence<T> Then(double delay, params Generator[] childGenerators)
        {
            if (!hasEnd)
                throw new InvalidOperationException($"Can not perform {nameof(Then)} on an endless {nameof(TransformSequence<T>)}.");

            // "Then" simply sets the currentTime to endTime to continue where the last transform left off,
            // followed by a subsequent delay call.
            currentTime = endTime;
            return Delay(delay, childGenerators);
        }

        /// <summary>
        /// Advances the start time of future appended <see cref="TransformSequence{T}"/>s by <paramref name="delay"/> milliseconds.
        /// Then, <paramref name="childGenerators"/> are appended via <see cref="Append(IEnumerable{Generator})"/>.
        /// </summary>
        /// <param name="delay">The delay to advance the start time by.</param>
        /// <param name="childGenerators">The optional <see cref="Generator"/>s for <see cref="TransformSequence{T}"/>s to be appended.</param>
        /// <returns>This <see cref="TransformSequence{T}"/>.</returns>
        public TransformSequence<T> Delay(double delay, params Generator[] childGenerators)
        {
            // After a delay statement, future transforms are appended after a currentTime which got offset by a delay.
            currentTime += delay;
            return Append(childGenerators);
        }

        /// <summary>
        /// Registers a callback <paramref name="function"/> which is triggered once all <see cref="Transform"/>s in this
        /// <see cref="TransformSequence{T}"/> complete successfully.
        /// If all <see cref="Transform"/>s already completed successfully at the point of this call, then
        /// <paramref name="function"/> is triggered immediately.
        /// Only a single callback function may be registered.
        /// </summary>
        /// <param name="function">The callback function.</param>
        public void OnComplete(Action<T> function)
        {
            if (!hasEnd)
                throw new InvalidOperationException($"Can not perform {nameof(Then)} on an endless {nameof(TransformSequence<T>)}.");

            subscribeComplete(() => function(origin));
        }

        /// <summary>
        /// Registers a callback <paramref name="function"/> which is triggered once any <see cref="Transform"/> in this
        /// <see cref="TransformSequence{T}"/> is aborted (e.g. by another <see cref="Transform"/> overriding it).
        /// Only a single callback function may be registered.
        /// </summary>
        /// <param name="function">The callback function.</param>
        public void OnAbort(Action<T> function) => subscribeAbort(() => function(origin));

        /// <summary>
        /// Registers a callback <paramref name="function"/> which is triggered once any <see cref="Transform"/> in this
        /// <see cref="TransformSequence{T}"/> is aborted or when all <see cref="Transform"/>s complete successfully.
        /// This is equivalent with calling both <see cref="OnComplete(Action{T})"/> and <see cref="OnAbort(Action{T})"/>.
        /// Only a single callback function may be registered.
        /// </summary>
        /// <param name="function">The callback function.</param>
        public void Finally(Action<T> function)
        {
            if (hasEnd)
                OnComplete(function);
            OnAbort(function);
        }

        void ITransformSequence.TransformAborted()
        {
            if (transforms.Count == 0)
                return;

            // No need for OnAbort events to trigger anymore, since
            // we are already aware of the abortion.
            foreach (var t in transforms)
            {
                t.AbortTargetSequence = null;
                t.CompletionTargetSequence = null;

                if (!t.HasStartValue)
                    t.TargetTransformable.RemoveTransform(t);
            }

            transforms.Clear();
            last = null;

            onAbort?.Invoke();
        }

        void ITransformSequence.TransformCompleted()
        {
            hasCompleted = true;
            onComplete?.Invoke();
        }
    }
}
