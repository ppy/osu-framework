// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;

namespace osu.Framework.Audio.Mixing.SDL3
{
    /// <summary>
    /// Interface for audio channels that feed audio to <see cref="SDL3AudioMixer"/>.
    /// </summary>
    internal interface ISDL3AudioChannel : IAudioChannel
    {
        IntPtr Handle { get; }

        bool IsActive { get; }
    }
}
