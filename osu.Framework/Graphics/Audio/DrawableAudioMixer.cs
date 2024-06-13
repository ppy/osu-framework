// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System.Diagnostics;
using ManagedBass;
using osu.Framework.Allocation;
using osu.Framework.Audio;
using osu.Framework.Audio.Mixing;
using osu.Framework.Graphics.Containers;

namespace osu.Framework.Graphics.Audio
{
    public partial class DrawableAudioMixer : AudioContainer, IAudioMixer
    {
        private AudioMixer mixer;

        [BackgroundDependencyLoader]
        private void load(AudioManager audio)
        {
            mixer = audio.CreateAudioMixer(Name);
        }

        public void Add(IAudioChannel channel)
        {
            if (LoadState < LoadState.Ready)
                Schedule(() => mixer.Add(channel));
            else
            {
                Debug.Assert(mixer != null);
                mixer.Add(channel);
            }
        }

        public void Remove(IAudioChannel channel)
        {
            if (LoadState < LoadState.Ready)
                Schedule(() => mixer.Remove(channel));
            else
            {
                Debug.Assert(mixer != null);
                mixer.Remove(channel);
            }
        }

        public void AddEffect(IEffectParameter effect, int priority = 0)
        {
            if (LoadState < LoadState.Ready)
                Schedule(() => mixer.AddEffect(effect, priority));
            else
            {
                Debug.Assert(mixer != null);
                mixer.AddEffect(effect, priority);
            }
        }

        public void RemoveEffect(IEffectParameter effect)
        {
            if (LoadState < LoadState.Ready)
                Schedule(() => mixer.RemoveEffect(effect));
            else
            {
                Debug.Assert(mixer != null);
                mixer.RemoveEffect(effect);
            }
        }

        public void UpdateEffect(IEffectParameter effect)
        {
            if (LoadState < LoadState.Ready)
                Schedule(() => mixer.UpdateEffect(effect));
            else
            {
                Debug.Assert(mixer != null);
                mixer.UpdateEffect(effect);
            }
        }

        protected override void Dispose(bool isDisposing)
        {
            base.Dispose(isDisposing);
            mixer?.Dispose();
        }
    }
}
