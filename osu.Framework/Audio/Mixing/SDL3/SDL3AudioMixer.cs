// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using ManagedBass;
using ManagedBass.Fx;
using osu.Framework.Extensions.ObjectExtensions;
using osu.Framework.Statistics;
using NAudio.Dsp;

namespace osu.Framework.Audio.Mixing.SDL3
{
    /// <summary>
    /// Mixes <see cref="ISDL3AudioChannel"/> instances and applies effects on top of them.
    /// </summary>
    internal class SDL3AudioMixer : AudioMixer
    {
        private readonly object syncRoot = new object();

        /// <summary>
        /// List of <see cref="ISDL3AudioChannel"/> instances that are active.
        /// </summary>
        private readonly LinkedList<ISDL3AudioChannel> activeChannels = new LinkedList<ISDL3AudioChannel>();

        /// <summary>
        /// Creates a new <see cref="SDL3AudioMixer"/>
        /// </summary>
        /// <param name="globalMixer"><inheritdoc /></param>
        /// <param name="identifier">An identifier displayed on the audio mixer visualiser.</param>
        public SDL3AudioMixer(AudioMixer? globalMixer, string identifier)
            : base(globalMixer, identifier)
        {
        }

        protected override void AddInternal(IAudioChannel channel)
        {
            if (channel is not ISDL3AudioChannel sdlChannel)
                return;

            lock (syncRoot)
                activeChannels.AddLast(sdlChannel);
        }

        protected override void RemoveInternal(IAudioChannel channel)
        {
            if (channel is not ISDL3AudioChannel sdlChannel)
                return;

            lock (syncRoot)
                activeChannels.Remove(sdlChannel);
        }

        protected override void UpdateState()
        {
            FrameStatistics.Add(StatisticsCounterType.MixChannels, channelCount);
            base.UpdateState();
        }

        private void mixAudio(float[] dst, float[] src, ref int filled, int samples, float left, float right)
        {
            if (left <= 0 && right <= 0)
                return;

            int i = 0;

            for (; i < filled; i++)
                dst[i] += src[i] * ((i % 2) == 0 ? left : right);

            for (; i < samples; i++)
                dst[i] = src[i] * ((i % 2) == 0 ? left : right);

            if (samples > filled)
                filled = samples;
        }

        private float[]? ret;

        private float[]? filterArray;

        private volatile int channelCount;

        /// <summary>
        /// Mix <see cref="activeChannels"/> into a float array given as an argument.
        /// </summary>
        /// <param name="data">A float array that audio will be mixed into.</param>
        /// <param name="sampleCount">Size of data</param>
        /// <param name="filledSamples">Count of usable audio samples in data</param>
        public void MixChannelsInto(float[] data, int sampleCount, ref int filledSamples)
        {
            lock (syncRoot)
            {
                if (ret == null || sampleCount != ret.Length)
                {
                    ret = new float[sampleCount];
                }

                bool useFilters = activeEffects.Count > 0;

                if (useFilters && (filterArray == null || filterArray.Length != sampleCount))
                {
                    filterArray = new float[sampleCount];
                }

                int filterArrayFilled = 0;

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

                        if (size > 0)
                        {
                            var (left, right) = channel.Volume;

                            if (!useFilters)
                            {
                                mixAudio(data, ret, ref filledSamples, size, left, right);
                            }
                            else
                            {
                                mixAudio(filterArray!, ret, ref filterArrayFilled, size, left, right);
                            }
                        }
                    }

                    node = next;
                }

                channelCount = activeChannels.Count;

                if (useFilters)
                {
                    for (int i = 0; i < filterArrayFilled; i++)
                    {
                        foreach (var filter in activeEffects.Values)
                        {
                            if (filter.BiQuadFilter != null)
                            {
                                filterArray![i] = filter.BiQuadFilter.Transform(filterArray[i]);
                            }
                        }
                    }

                    mixAudio(data, filterArray!, ref filledSamples, filterArrayFilled, 1, 1);
                }
            }
        }

        internal class EffectBox
        {
            public readonly BiQuadFilter? BiQuadFilter;
            public readonly IEffectParameter EffectParameter;

            public EffectBox(IEffectParameter param)
            {
                // allowing non-bqf to keep index of list
                if (param is BQFParameters bqfp)
                    BiQuadFilter = getFilter(SDL3AudioManager.AUDIO_FREQ, bqfp);

                EffectParameter = param;
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

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            // Move all contained channels back to the default mixer.
            foreach (var channel in activeChannels.ToArray())
                Remove(channel);
        }

        public void StreamFree(IAudioChannel channel)
        {
            Remove(channel, false);
        }

        private readonly SortedDictionary<int, EffectBox> activeEffects = new SortedDictionary<int, EffectBox>();

        public override void AddEffect(IEffectParameter effect, int priority = 0) => EnqueueAction(() =>
        {
            lock (syncRoot)
            {
                if (activeEffects.ContainsKey(priority))
                    return;

                activeEffects[priority] = new EffectBox(effect);
            }
        });

        public override void RemoveEffect(IEffectParameter effect) => EnqueueAction(() =>
        {
            lock (syncRoot)
            {
                bool found = false;

                do
                {
                    foreach (KeyValuePair<int, EffectBox> pair in activeEffects)
                    {
                        if (pair.Value.EffectParameter == effect)
                        {
                            activeEffects.Remove(pair.Key); // cannot move forward because we removed it!
                            found = true;
                            break;
                        }
                    }
                }
                while (found);
            }
        });

        public override void UpdateEffect(IEffectParameter effect) => EnqueueAction(() =>
        {
        });
    }
}
