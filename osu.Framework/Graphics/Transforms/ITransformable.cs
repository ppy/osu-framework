// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Timing;

namespace osu.Framework.Graphics.Transforms
{
    public interface ITransformable
    {
        IDisposable BeginDelayedSequence(double delay, bool recursive = false);

        IDisposable BeginAbsoluteSequence(double newTransformStartTime, bool recursive = false);

        /// <summary>
        /// The current frame's time as observed by this class's <see cref="Transform"/>s.
        /// </summary>
        FrameTimeInfo Time { get; }

        double TransformStartTime { get; }

        void AddTransform(Transform transform, ulong? customTransformID = null);

        void RemoveTransform(Transform toRemove);
    }
}
