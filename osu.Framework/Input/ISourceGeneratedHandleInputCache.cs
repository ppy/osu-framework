// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;

namespace osu.Framework.Input
{
    public interface ISourceGeneratedHandleInputCache
    {
        /// <summary>
        /// The most-derived type that implements this interface.
        /// This is used to handle non-partial leaf types that can't rely on the source-generated values and must fall back to reflection.
        /// </summary>
        protected internal Type KnownType { get; }

        /// <summary>
        /// Whether this type or any of its base types request positional input.
        /// </summary>
        protected internal bool RequestsPositionalInput { get; }

        /// <summary>
        /// Whether this type or any of its base types request non-positional input.
        /// </summary>
        protected internal bool RequestsNonPositionalInput { get; }
    }
}
