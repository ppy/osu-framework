// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Threading;
using ManagedBass.Fx;
using mysoundlib_cs;
using osu.Framework.Extensions;

namespace osu.Framework.Audio.Mixing.SDL3
{
    internal class SDL3AudioEffect : AudioEffect
    {
        private readonly SDL3AudioMixer mixer;

        private volatile IntPtr bqFilter = IntPtr.Zero;

        private readonly Action<Action> enqueueAction;

        public SDL3AudioEffect(Action<Action> enqueueAction, SDL3AudioMixer mixer, int priority = 0)
            : base(priority)
        {
            this.mixer = mixer;
            this.enqueueAction = enqueueAction;
        }

        public override void Apply() => enqueueAction(() =>
        {
            if (EffectParameter is BQFParameters bqfp)
            {
                MySoundLibrary.BiQuadType type;

                switch (bqfp.lFilter)
                {
                    case BQFType.LowPass:
                        type = MySoundLibrary.BiQuadType.LPF;
                        break;

                    case BQFType.HighPass:
                        type = MySoundLibrary.BiQuadType.HPF;
                        break;

                    case BQFType.BandPass:
                        type = MySoundLibrary.BiQuadType.BPF;
                        break;

                    case BQFType.BandPassQ:
                        type = MySoundLibrary.BiQuadType.BPQ;
                        break;

                    case BQFType.Notch:
                        type = MySoundLibrary.BiQuadType.NOTCH;
                        break;

                    case BQFType.PeakingEQ:
                        type = MySoundLibrary.BiQuadType.PEQ;
                        break;

                    case BQFType.LowShelf:
                        type = MySoundLibrary.BiQuadType.LSH;
                        break;

                    case BQFType.HighShelf:
                        type = MySoundLibrary.BiQuadType.HSH;
                        break;

                    case BQFType.AllPass:
                    default:
                        type = MySoundLibrary.BiQuadType.APF;
                        break;
                }

                IntPtr coeffIntPtr = MySoundLibrary.mslBiQuadUpdate(type, bqfp.fGain, bqfp.fCenter, SDL3AudioManager.AUDIO_SPEC.freq, bqfp.fQ).ThrowIfNull();

                if (bqFilter == IntPtr.Zero)
                    bqFilter = MySoundLibrary.mslApplyBiquadFilter(mixer.Handle, coeffIntPtr, Priority).ThrowIfNull();
                else
                    MySoundLibrary.mslUpdateBiquadFilter(mixer.Handle, coeffIntPtr, bqFilter);
            }
        });

        public override void Remove() => enqueueAction(() =>
        {
            if (bqFilter != IntPtr.Zero)
                MySoundLibrary.mslRemoveBiquadFilter(mixer.Handle, Interlocked.Exchange(ref bqFilter, IntPtr.Zero));
        });
    }
}
