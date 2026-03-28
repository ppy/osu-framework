// Minimal Wasapi-based AudioMixer prototype.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using ManagedBass;
using osu.Framework.Audio.Mixing;
using osu.Framework.Audio;

namespace osu.Framework.Audio.Mixing.Wasapi
{
    /// <summary>
    /// Prototype audio mixer for WASAPI backend. This implementation is intentionally minimal
    /// and provides the mixing host semantics without integrating with a native API yet.
    /// </summary>
    internal class WasapiAudioMixer : AudioMixer
    {
        private readonly List<IAudioChannel> activeChannels = new List<IAudioChannel>();

        public IAudioBackend Backend { get; }

        public WasapiAudioMixer(IAudioBackend backend, AudioMixer? fallbackMixer, string identifier)
            : base(fallbackMixer, identifier)
        {
            Backend = backend ?? throw new ArgumentNullException(nameof(backend));
        }

        public override void AddEffect(IEffectParameter effect, int priority = 0) => EnqueueAction(() => { /* TODO: implement FX support */ });

        public override void RemoveEffect(IEffectParameter effect) => EnqueueAction(() => { /* TODO */ });

        public override void UpdateEffect(IEffectParameter effect) => EnqueueAction(() => { /* TODO */ });

        protected override void AddInternal(IAudioChannel channel)
        {
            Debug.Assert(CanPerformInline);

            if (channel == null) return;

            if (!activeChannels.Contains(channel))
                activeChannels.Add(channel);
        }

        protected override void RemoveInternal(IAudioChannel channel)
        {
            Debug.Assert(CanPerformInline);
            activeChannels.Remove(channel);
        }

        protected override void UpdateState()
        {
            base.UpdateState();
        }
    }
}
