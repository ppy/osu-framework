// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System.Diagnostics;
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

        public AudioEffect GetNewEffect(int priority = 0)
        {
            if (LoadState < LoadState.Ready)
                return new LazyAudioEffect(this, priority);
            else
            {
                Debug.Assert(mixer != null);
                return mixer.GetNewEffect(priority);
            }
        }

        public class LazyAudioEffect : AudioEffect
        {
            private readonly DrawableAudioMixer parent;
            private readonly int priority;
            private AudioEffect effect;
            private volatile bool applied;

            public LazyAudioEffect(DrawableAudioMixer parent, int priority)
            {
                this.parent = parent;
                this.priority = priority;
            }

            private void update()
            {
                if (effect == null)
                {
                    if (parent.LoadState >= LoadState.Ready)
                    {
                        effect = parent.mixer.GetNewEffect(priority);
                    }
                    else
                    {
                        parent.Scheduler.Add(update, true);
                        return;
                    }
                }

                effect.EffectParameter = EffectParameter;

                if (applied)
                    effect.Apply();
                else
                    effect.Remove();
            }

            public override void Apply()
            {
                applied = true;
                parent.Schedule(update);
            }

            public override void Remove()
            {
                applied = false;
                parent.Schedule(update);
            }
        }

        protected override void Dispose(bool isDisposing)
        {
            base.Dispose(isDisposing);
            mixer?.Dispose();
        }
    }
}
