// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace osu.Framework.Audio.Mixing
{
    /// <summary>
    /// A no-sound audio mixer.
    /// </summary>
    public class NullAudioMixer : AudioMixer
    {
        protected override void AddInternal(IAudioChannel channel)
        {
        }

        protected override void RemoveInternal(IAudioChannel channel)
        {
        }
    }
}
