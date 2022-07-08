// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Runtime.CompilerServices;
using osu.Framework.Layout;

namespace osu.Framework.Graphics
{
    /// <summary>
    /// Contains the internal logic for the state management of <see cref="Drawable.Invalidate"/>.
    /// </summary>
    internal struct InvalidationList
    {
        private Invalidation selfInvalidation;
        private Invalidation parentInvalidation;
        private Invalidation childInvalidation;

        /// <summary>
        /// Creates a new <see cref="InvalidationList"/>.
        /// </summary>
        /// <param name="initialState">The initial invalidation state.</param>
        public InvalidationList(Invalidation initialState)
        {
            this = default;

            invalidate(ref selfInvalidation, initialState);
            invalidate(ref parentInvalidation, initialState);
            invalidate(ref childInvalidation, initialState);
        }

        /// <summary>
        /// Invalidates a <see cref="InvalidationSource"/> with given <see cref="Invalidation"/> flags.
        /// </summary>
        /// <param name="source">The <see cref="InvalidationSource"/> to invalidate.</param>
        /// <param name="flags">The <see cref="Invalidation"/> flags to invalidate with.</param>
        /// <returns>Whether an invalidation was performed.</returns>
        /// <exception cref="ArgumentException">If <see cref="InvalidationSource"/> was not a valid source.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Invalidate(InvalidationSource source, Invalidation flags)
        {
            switch (source)
            {
                case InvalidationSource.Self:
                    return invalidate(ref selfInvalidation, flags);

                case InvalidationSource.Parent:
                    return invalidate(ref parentInvalidation, flags);

                case InvalidationSource.Child:
                    return invalidate(ref childInvalidation, flags);

                default:
                    throw new ArgumentException("Unexpected invalidation source.", nameof(source));
            }
        }

        /// <summary>
        /// Validates all <see cref="InvalidationSource"/>s with given <see cref="Invalidation"/> flags.
        /// </summary>
        /// <param name="validation">The <see cref="Invalidation"/> flags to validate with.</param>
        /// <returns>Whether any <see cref="InvalidationSource"/> was validated.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Validate(Invalidation validation)
        {
            return validate(ref selfInvalidation, validation)
                   | validate(ref parentInvalidation, validation)
                   | validate(ref childInvalidation, validation);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool invalidate(ref Invalidation target, Invalidation flags)
        {
            if ((target & flags) == flags)
                return false;

            // Remove all non-layout flags, as they should always propagate and are thus not to be stored.
            target |= flags & Invalidation.Layout;
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool validate(ref Invalidation target, Invalidation flags)
        {
            if ((target & flags) == 0)
                return false;

            target &= ~flags;
            return true;
        }
    }
}
