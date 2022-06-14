// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using osu.Framework.Allocation;
using osu.Framework.Timing;

namespace osu.Framework.Graphics.Transforms
{
    public interface ITransformable
    {
        /// <summary>
        /// Start a sequence of <see cref="Transform"/>s with a (cumulative) relative delay applied.
        /// </summary>
        /// <param name="delay">The offset in milliseconds from current time. Note that this stacks with other nested sequences.</param>
        /// <param name="recursive">Whether this should be applied to all children. True by default.</param>
        /// <returns>An <see cref="InvokeOnDisposal"/> to be used in a using() statement.</returns>
        IDisposable BeginDelayedSequence(double delay, bool recursive = true);

        /// <summary>
        /// Start a sequence of <see cref="Transform"/>s from an absolute time value (adjusts <see cref="TransformStartTime"/>).
        /// </summary>
        /// <param name="newTransformStartTime">The new value for <see cref="TransformStartTime"/>.</param>
        /// <param name="recursive">Whether this should be applied to all children. True by default.</param>
        /// <returns>An <see cref="InvokeOnDisposal"/> to be used in a using() statement.</returns>
        /// <exception cref="InvalidOperationException">Absolute sequences should never be nested inside another existing sequence.</exception>
        IDisposable BeginAbsoluteSequence(double newTransformStartTime, bool recursive = true);

        /// <summary>
        /// The current frame's time as observed by this class's <see cref="Transform"/>s.
        /// </summary>
        FrameTimeInfo Time { get; }

        double TransformStartTime { get; }

        void AddTransform(Transform transform, ulong? customTransformID = null);

        void RemoveTransform(Transform toRemove);
    }
}
