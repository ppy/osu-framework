// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Diagnostics;
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

            invalidate(selfInvalidation, initialState, out selfInvalidation);
            invalidate(parentInvalidation, initialState, out parentInvalidation);
            invalidate(childInvalidation, initialState, out childInvalidation);
        }

        /// <summary>
        /// Invalidates a <see cref="InvalidationSource"/> with given <see cref="Invalidation"/> flags.
        /// </summary>
        /// <remarks>
        /// Call sites must ensure that on a <see cref="InvalidationSource.Self"/>, <see cref="InvalidationSource.Parent"/> or <see cref="InvalidationSource.Child"/> source is provided.
        /// </remarks>
        /// <param name="source">The <see cref="InvalidationSource"/> to invalidate.</param>
        /// <param name="flags">The <see cref="Invalidation"/> flags to invalidate with.</param>
        /// <returns>Whether an invalidation was performed.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Invalidate(InvalidationSource source, Invalidation flags)
        {
            // Guaranteed by preconditions at the call site.
            Debug.Assert(source is InvalidationSource.Self or InvalidationSource.Parent or InvalidationSource.Child);

            switch (source)
            {
                case InvalidationSource.Self:
                    return invalidate(selfInvalidation, flags, out selfInvalidation);

                case InvalidationSource.Parent:
                    return invalidate(parentInvalidation, flags, out parentInvalidation);

                // Guaranteed to be InvalidationSource.Child by the call site of this method.
                default:
                    return invalidate(childInvalidation, flags, out childInvalidation);
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
            return validate(selfInvalidation, validation, out selfInvalidation)
                   | validate(parentInvalidation, validation, out parentInvalidation)
                   | validate(childInvalidation, validation, out childInvalidation);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool invalidate(Invalidation target, Invalidation flags, out Invalidation result)
        {
            // Remove all non-layout flags, as they should always propagate and are thus not to be stored.
            result = target | (flags & Invalidation.Layout);
            return (target & flags) != flags;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool validate(Invalidation target, Invalidation flags, out Invalidation result)
        {
            result = target & ~flags;
            return (target & flags) != 0;
        }
    }
}
