// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using ManagedBass;
using ManagedBass.Fx;
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

            for (int e = 0; e < samples; e += 2)
            {
                if (e < filled)
                {
                    dst[e] += src[e] * left;
                    dst[e + 1] += src[e + 1] * right;
                }
                else
                {
                    dst[e] = src[e] * left;
                    dst[e + 1] = src[e + 1] * right;
                }

                if (dst[e] < -1f)
                    dst[e] = -1f;
                else if (dst[e] > 1f)
                    dst[e] = 1f;

                if (dst[e + 1] < -1f)
                    dst[e + 1] = -1f;
                else if (dst[e + 1] > 1f)
                    dst[e + 1] = 1f;
            }

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
                    ret = new float[sampleCount];

                bool useFilters = activeEffects.Count > 0;

                if (useFilters && (filterArray == null || filterArray.Length != sampleCount))
                    filterArray = new float[sampleCount];

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
                                mixAudio(data, ret, ref filledSamples, size, left, right);
                            else
                                mixAudio(filterArray!, ret, ref filterArrayFilled, size, left, right);
                        }
                    }

                    node = next;
                }

                channelCount = activeChannels.Count;

                if (useFilters)
                {
                    foreach (var filter in activeEffects.Values)
                    {
                        for (int i = 0; i < filterArrayFilled; i++)
                            filterArray![i] = filter.Transform(filterArray[i]);
                    }

                    mixAudio(data, filterArray!, ref filledSamples, filterArrayFilled, 1, 1);
                }
            }
        }

        private static BiQuadFilter updateFilter(BiQuadFilter? filter, float freq, BQFParameters bqfp)
        {
            switch (bqfp.lFilter)
            {
                case BQFType.LowPass:
                    if (filter == null)
                        return BiQuadFilter.LowPassFilter(freq, bqfp.fCenter, bqfp.fQ);
                    else
                        filter.SetLowPassFilter(freq, bqfp.fCenter, bqfp.fQ);

                    return filter;

                case BQFType.HighPass:
                    if (filter == null)
                        return BiQuadFilter.HighPassFilter(freq, bqfp.fCenter, bqfp.fQ);
                    else
                        filter.SetHighPassFilter(freq, bqfp.fCenter, bqfp.fQ);

                    return filter;

                case BQFType.PeakingEQ:
                    if (filter == null)
                        return BiQuadFilter.PeakingEQ(freq, bqfp.fCenter, bqfp.fQ, bqfp.fGain);
                    else
                        filter.SetPeakingEq(freq, bqfp.fCenter, bqfp.fQ, bqfp.fGain);

                    return filter;

                case BQFType.BandPass:
                    return BiQuadFilter.BandPassFilterConstantPeakGain(freq, bqfp.fCenter, bqfp.fQ);

                case BQFType.BandPassQ:
                    return BiQuadFilter.BandPassFilterConstantSkirtGain(freq, bqfp.fCenter, bqfp.fQ);

                case BQFType.Notch:
                    return BiQuadFilter.NotchFilter(freq, bqfp.fCenter, bqfp.fQ);

                case BQFType.LowShelf:
                    return BiQuadFilter.LowShelf(freq, bqfp.fCenter, bqfp.fS, bqfp.fGain);

                case BQFType.HighShelf:
                    return BiQuadFilter.HighShelf(freq, bqfp.fCenter, bqfp.fS, bqfp.fGain);

                case BQFType.AllPass:
                default:
                    return BiQuadFilter.AllPassFilter(freq, bqfp.fCenter, bqfp.fQ);
            }
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

        // Would like something like BiMap in Java, but I cannot write the whole collection here.
        private readonly SortedDictionary<int, BiQuadFilter> activeEffects = new SortedDictionary<int, BiQuadFilter>();
        private readonly Dictionary<IEffectParameter, int> parameterDict = new Dictionary<IEffectParameter, int>();

        public override void AddEffect(IEffectParameter effect, int priority = 0) => EnqueueAction(() =>
        {
            if (parameterDict.ContainsKey(effect) || effect is not BQFParameters bqfp)
                return;

            while (activeEffects.ContainsKey(priority))
                priority++;

            BiQuadFilter filter = updateFilter(null, SDL3AudioManager.AUDIO_FREQ, bqfp);

            lock (syncRoot)
                activeEffects[priority] = filter;

            parameterDict[effect] = priority;
        });

        public override void RemoveEffect(IEffectParameter effect) => EnqueueAction(() =>
        {
            if (!parameterDict.TryGetValue(effect, out int index))
                return;

            lock (syncRoot)
                activeEffects.Remove(index);

            parameterDict.Remove(effect);
        });

        public override void UpdateEffect(IEffectParameter effect) => EnqueueAction(() =>
        {
            if (!parameterDict.TryGetValue(effect, out int index) || effect is not BQFParameters bqfp)
                return;

            lock (syncRoot)
                activeEffects[index] = updateFilter(activeEffects[index], SDL3AudioManager.AUDIO_FREQ, bqfp);
        });
    }
}
