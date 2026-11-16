// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using ManagedBass;

namespace osu.Framework.Audio.Wasapi
{
    internal static class WasapiExclusiveBufferLadder
    {
        public static readonly int[] TransientRetryDelaysMs = [0, 100, 250];

        public static bool IsTransientInitError(Errors error)
            => error == Errors.Busy || error == Errors.Driver || error == Errors.Unknown;
    }
}
