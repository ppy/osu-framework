// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using ManagedBass;
using ManagedBass.Fx;
using osu.Framework.Bindables;
using osu.Framework.Extensions.ObjectExtensions;
using osu.Framework.Statistics;
using NAudio.Dsp;

namespace osu.Framework.Audio.Mixing.SDL2
{
    /// <summary>
    /// Mixes <see cref="ISDL2AudioChannel"/> instances and applies effects on top of them.
    /// </summary>
    internal class SDL2AudioMixer : AudioMixer
    {
        private readonly object syncRoot = new object();

        /// <summary>
        /// List of <see cref="ISDL2AudioChannel"/> instances that are active.
        /// </summary>
        private readonly LinkedList<ISDL2AudioChannel> activeChannels = new LinkedList<ISDL2AudioChannel>();

        /// <summary>
        /// Creates a new <see cref="SDL2AudioMixer"/>
        /// </summary>
        /// <param name="globalMixer"><inheritdoc /></param>
        /// <param name="identifier">An identifier displayed on the audio mixer visualiser.</param>
        public SDL2AudioMixer(AudioMixer? globalMixer, string identifier)
            : base(globalMixer, identifier)
        {
            EnqueueAction(() => Effects.BindCollectionChanged(onEffectsChanged, true));
        }

        public override BindableList<IEffectParameter> Effects { get; } = new BindableList<IEffectParameter>();

        protected override void AddInternal(IAudioChannel channel)
        {
            if (channel is not ISDL2AudioChannel sdlChannel)
                return;

            lock (syncRoot)
                activeChannels.AddLast(sdlChannel);
        }

        protected override void RemoveInternal(IAudioChannel channel)
        {
            if (channel is not ISDL2AudioChannel sdlChannel)
                return;

            lock (syncRoot)
                activeChannels.Remove(sdlChannel);
        }

        protected override void UpdateState()
        {
            FrameStatistics.Add(StatisticsCounterType.MixChannels, channelCount);
            base.UpdateState();
        }

        // https://github.com/libsdl-org/SDL/blob/SDL2/src/audio/SDL_mixer.c#L292
        private const float max_vol = 3.402823466e+38F;
        private const float min_vol = -3.402823466e+38F;

        private void mixAudio(float[] dst, float[] src, int samples, float left, float right)
        {
            if (left <= 0 && right <= 0)
                return;

            for (int i = 0; i < samples; i++)
                dst[i] = Math.Clamp(src[i] * ((i % 2) == 0 ? left : right) + dst[i], min_vol, max_vol);
        }

        private float[]? ret;

        private volatile int channelCount;

        /// <summary>
        /// Mix <see cref="activeChannels"/> into a float array given as an argument.
        /// </summary>
        /// <param name="data">A float array that audio will be mixed into.</param>
        public void MixChannelsInto(float[] data)
        {
            lock (syncRoot)
            {
                int sampleCount = data.Length;
                if (ret == null || sampleCount != ret.Length)
                    ret = new float[sampleCount];

                bool useFilters = audioFilters.Count > 0;
                float[] put = useFilters ? new float[sampleCount] : data;

                var node = activeChannels.First;

                while (node != null)
                {
                    var next = node.Next;
                    var channel = node.Value;

                    if (!(channel is AudioComponent ac && ac.IsAlive))
                    {
                        activeChannels.Remove(node);
                    }
                    else if (channel.Playing)
                    {
                        int size = channel.GetRemainingSamples(ret);
                        float left = 1;
                        float right = 1;

                        if (size > 0)
                        {
                            if (channel.Balance < 0)
                                right += (float)channel.Balance;
                            else if (channel.Balance > 0)
                                left -= (float)channel.Balance;

                            right *= channel.Volume;
                            left *= channel.Volume;

                            mixAudio(put, ret, size, left, right);
                        }
                    }

                    node = next;
                }

                channelCount = activeChannels.Count;

                if (useFilters)
                {
                    for (int i = 0; i < sampleCount; i++)
                    {
                        foreach (var filter in audioFilters)
                        {
                            if (filter.BiQuadFilter != null)
                                put[i] = filter.BiQuadFilter.Transform(put[i]);
                        }
                    }

                    mixAudio(data, put, sampleCount, 1, 1);
                }
            }
        }

        private readonly List<EffectBox> audioFilters = new List<EffectBox>();

        private void onEffectsChanged(object? sender, NotifyCollectionChangedEventArgs e) => EnqueueAction(() =>
        {
            lock (syncRoot)
            {
                switch (e.Action)
                {
                    case NotifyCollectionChangedAction.Add:
                    {
                        Debug.Assert(e.NewItems != null);
                        int startIndex = Math.Max(0, e.NewStartingIndex);
                        audioFilters.InsertRange(startIndex, e.NewItems.OfType<IEffectParameter>().Select(eff => new EffectBox(eff)));
                        break;
                    }

                    case NotifyCollectionChangedAction.Move:
                    {
                        EffectBox effect = audioFilters[e.OldStartingIndex];
                        audioFilters.RemoveAt(e.OldStartingIndex);
                        audioFilters.Insert(e.NewStartingIndex, effect);
                        break;
                    }

                    case NotifyCollectionChangedAction.Remove:
                    {
                        Debug.Assert(e.OldItems != null);

                        audioFilters.RemoveRange(e.OldStartingIndex, e.OldItems.Count);
                        break;
                    }

                    case NotifyCollectionChangedAction.Replace:
                    {
                        Debug.Assert(e.NewItems != null);

                        EffectBox newFilter = new EffectBox((IEffectParameter)e.NewItems[0].AsNonNull());
                        audioFilters[e.NewStartingIndex] = newFilter;
                        break;
                    }

                    case NotifyCollectionChangedAction.Reset:
                    {
                        audioFilters.Clear();
                        break;
                    }
                }
            }
        });

        internal class EffectBox
        {
            public readonly BiQuadFilter? BiQuadFilter;

            public EffectBox(IEffectParameter param)
            {
                // allowing non-bqf to keep index of list
                if (param is BQFParameters bqfp)
                    BiQuadFilter = getFilter(SDL2AudioManager.AUDIO_FREQ, bqfp);
            }
        }

        private static BiQuadFilter getFilter(float freq, BQFParameters bqfp)
        {
            BiQuadFilter filter;

            switch (bqfp.lFilter)
            {
                case BQFType.LowPass:
                    filter = BiQuadFilter.LowPassFilter(freq, bqfp.fCenter, bqfp.fQ);
                    break;

                case BQFType.HighPass:
                    filter = BiQuadFilter.HighPassFilter(freq, bqfp.fCenter, bqfp.fQ);
                    break;

                case BQFType.BandPass:
                    filter = BiQuadFilter.BandPassFilterConstantPeakGain(freq, bqfp.fCenter, bqfp.fQ);
                    break;

                case BQFType.BandPassQ:
                    filter = BiQuadFilter.BandPassFilterConstantSkirtGain(freq, bqfp.fCenter, bqfp.fQ);
                    break;

                case BQFType.Notch:
                    filter = BiQuadFilter.NotchFilter(freq, bqfp.fCenter, bqfp.fQ);
                    break;

                case BQFType.PeakingEQ:
                    filter = BiQuadFilter.PeakingEQ(freq, bqfp.fCenter, bqfp.fQ, bqfp.fGain);
                    break;

                case BQFType.LowShelf:
                    filter = BiQuadFilter.LowShelf(freq, bqfp.fCenter, bqfp.fS, bqfp.fGain);
                    break;

                case BQFType.HighShelf:
                    filter = BiQuadFilter.HighShelf(freq, bqfp.fCenter, bqfp.fS, bqfp.fGain);
                    break;

                case BQFType.AllPass:
                default: // NAudio BiQuadFilter covers all, this default is kind of meaningless
                    filter = BiQuadFilter.AllPassFilter(freq, bqfp.fCenter, bqfp.fQ);
                    break;
            }

            return filter;
        }

        public void StreamFree(IAudioChannel channel)
        {
            Remove(channel, false);
        }
    }
}
