// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Runtime.CompilerServices;
using osu.Framework.Layout;

namespace osu.Framework.Graphics
{
    internal struct InvalidationList
    {
        private Invalidation selfInvalidation;
        private Invalidation parentInvalidation;
        private Invalidation childInvalidation;

        public InvalidationList(Invalidation initialState)
        {
            this = default;

            invalidate(ref selfInvalidation, initialState);
            invalidate(ref parentInvalidation, initialState);
            invalidate(ref childInvalidation, initialState);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Invalidate(InvalidationSource source, Invalidation invalidation)
        {
            switch (source)
            {
                case InvalidationSource.Self:
                    return invalidate(ref selfInvalidation, invalidation);

                case InvalidationSource.Parent:
                    return invalidate(ref parentInvalidation, invalidation);

                case InvalidationSource.Child:
                    return invalidate(ref childInvalidation, invalidation);

                default:
                    throw new ArgumentException("Unexpected invalidation source.", nameof(source));
            }
        }

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

            // Some flags act as "markers" to imply the layout hasn't changed but rather that a significant change occurred in the scene graph such that the DrawNode should always be regenerated.
            // Such changes should always be alerted to the Drawable and its immediate hierarchy. Trimming off the flags here will cause them to never block invalidation.
            flags &= ~(Invalidation.DrawNode | Invalidation.Parent);

            target |= flags;
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
