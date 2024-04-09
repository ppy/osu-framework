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
            EnqueueAction(() => Effects.BindCollectionChanged(onEffectsChanged, true));
        }

        public override BindableList<IEffectParameter> Effects { get; } = new BindableList<IEffectParameter>();

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

        private unsafe void mixAudio(float* dst, float* src, ref int filled, int samples, float left, float right)
        {
            if (left <= 0 && right <= 0)
                return;

            for (int i = 0; i < samples; i++)
                *(dst + i) = (*(src + i) * ((i % 2) == 0 ? left : right)) + (i < filled ? *(dst + i) : 0);

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
        public unsafe void MixChannelsInto(float* data, int sampleCount, ref int filledSamples)
        {
            lock (syncRoot)
            {
                if (ret == null || sampleCount != ret.Length)
                {
                    ret = new float[sampleCount];
                }

                bool useFilters = audioFilters.Count > 0;

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
                                fixed (float* retPtr = ret)
                                {
                                    mixAudio(data, retPtr, ref filledSamples, size, left, right);
                                }
                            }
                            else
                            {
                                fixed (float* filterArrPtr = filterArray)
                                fixed (float* retPtr = ret)
                                {
                                    mixAudio(filterArrPtr, retPtr, ref filterArrayFilled, size, left, right);
                                }
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
                        foreach (var filter in audioFilters)
                        {
                            if (filter.BiQuadFilter != null)
                            {
                                filterArray![i] = filter.BiQuadFilter.Transform(filterArray[i]);
                            }
                        }
                    }

                    fixed (float* filterArrPtr = filterArray)
                    {
                        mixAudio(data, filterArrPtr, ref filledSamples, filterArrayFilled, 1, 1);
                    }
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
                    BiQuadFilter = getFilter(SDL3AudioManager.AUDIO_FREQ, bqfp);
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
    }
}
