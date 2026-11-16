// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;

namespace osu.Framework.Audio.Windows
{
    internal static class WasapiExclusiveEndpointGainPolicy
    {
        public static float MixerVolume(float scalar, bool muted)
        {
            if (muted)
                return 0f;

            if (float.IsNaN(scalar) || float.IsInfinity(scalar))
                return 1f;

            return Math.Clamp(scalar, 0f, 1f);
        }
    }
}
