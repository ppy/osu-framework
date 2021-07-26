// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable enable

using osu.Framework.Audio.Mixing;

namespace osu.Framework.Audio
{
    public interface IAudioChannel
    {
        AudioMixer? Mixer { get; set; }
    }
}
