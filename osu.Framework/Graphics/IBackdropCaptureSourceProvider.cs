// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Collections.Generic;

namespace osu.Framework.Graphics
{
    /// <summary>
    /// Provides an ordered list of drawables that should be captured by <see cref="BackdropBlurDrawable"/>.
    /// Sources should be ordered from back to front to match their intended composition.
    /// </summary>
    public interface IBackdropCaptureSourceProvider
    {
        /// <summary>
        /// Fired when the ordered capture source list changes.
        /// </summary>
        event Action SourcesChanged;

        /// <summary>
        /// The ordered sources to capture, from back to front.
        /// </summary>
        IReadOnlyList<Drawable> CaptureSources { get; }
    }
}
