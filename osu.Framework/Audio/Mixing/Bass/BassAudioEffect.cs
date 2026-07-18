// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;

namespace osu.Framework.Audio.Mixing.Bass
{
    internal class BassAudioEffect : AudioEffect
    {
        private readonly Action<Action> enqueueAction;

        private readonly BassAudioMixer mixer;

        private int handle;

        public BassAudioEffect(Action<Action> action, BassAudioMixer mixer, int priority = 0)
            : base(priority)
        {
            enqueueAction = action;
            this.mixer = mixer;
        }

        public override void Apply() => enqueueAction(() =>
        {
            if (EffectParameter == null)
                return;

            if (handle == 0)
                handle = ManagedBass.Bass.ChannelSetFX(mixer.Handle, EffectParameter.FXType, Priority);

            ManagedBass.Bass.FXSetParameters(handle, EffectParameter);
        });

        public override void Remove() => enqueueAction(() =>
        {
            ManagedBass.Bass.ChannelRemoveFX(mixer.Handle, handle);
            handle = 0;
        });
    }
}
