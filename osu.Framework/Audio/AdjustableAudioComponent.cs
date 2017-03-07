// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Framework.Configuration;

namespace osu.Framework.Audio
{
    public class AdjustableAudioComponent : AudioComponent
    {
        private List<BindableDouble> volumeAdjustments = new List<BindableDouble>();
        private List<BindableDouble> balanceAdjustments = new List<BindableDouble>();
        private List<BindableDouble> frequencyAdjustments = new List<BindableDouble>();

        /// <summary>
        /// Global volume of this component.
        /// </summary>
        public readonly BindableDouble Volume = new BindableDouble(1)
        {
            MinValue = 0,
            MaxValue = 1
        };

        protected readonly BindableDouble VolumeCalculated = new BindableDouble(1)
        {
            MinValue = 0,
            MaxValue = 1
        };

        /// <summary>
        /// Playback balance of this sample (-1 .. 1 where 0 is centered)
        /// </summary>
        public readonly BindableDouble Balance = new BindableDouble
        {
            MinValue = -1,
            MaxValue = 1
        };

        protected readonly BindableDouble BalanceCalculated = new BindableDouble
        {
            MinValue = -1,
            MaxValue = 1
        };

        /// <summary>
        /// Rate at which the component is played back (affects pitch). 1 is 100% playback speed, or default frequency.
        /// </summary>
        public readonly BindableDouble Frequency = new BindableDouble(1);

        protected readonly BindableDouble FrequencyCalculated = new BindableDouble(1);

        protected AdjustableAudioComponent()
        {
            Volume.ValueChanged += InvalidateState;
            Balance.ValueChanged += InvalidateState;
            Frequency.ValueChanged += InvalidateState;
        }

        internal void InvalidateState(object sender = null, EventArgs e = null)
        {
            PendingActions.Enqueue(() => OnStateChanged(this, null));
        }

        internal virtual void OnStateChanged(object sender, EventArgs e)
        {
            VolumeCalculated.Value = volumeAdjustments.Aggregate(Volume.Value, (current, adj) => current * adj);
            BalanceCalculated.Value = balanceAdjustments.Aggregate(Balance.Value, (current, adj) => current + adj);
            FrequencyCalculated.Value = frequencyAdjustments.Aggregate(Frequency.Value, (current, adj) => current * adj);
        }

        public void AddAdjustmentDependency(AdjustableAudioComponent component)
        {
            AddAdjustment(AdjustableProperty.Balance, component.BalanceCalculated);
            AddAdjustment(AdjustableProperty.Frequency, component.FrequencyCalculated);
            AddAdjustment(AdjustableProperty.Volume, component.VolumeCalculated);
        }

        public void RemoveAdjustmentDependency(AdjustableAudioComponent component)
        {
            RemoveAdjustment(component.BalanceCalculated);
            RemoveAdjustment(component.FrequencyCalculated);
            RemoveAdjustment(component.VolumeCalculated);
        }

        public void AddAdjustment(AdjustableProperty type, BindableDouble adjustBindable)
        {
            switch (type)
            {
                case AdjustableProperty.Balance:
                    balanceAdjustments.Add(adjustBindable);
                    break;
                case AdjustableProperty.Frequency:
                    frequencyAdjustments.Add(adjustBindable);
                    break;
                case AdjustableProperty.Volume:
                    volumeAdjustments.Add(adjustBindable);
                    break;
            }

            InvalidateState();
        }

        public void RemoveAdjustment(BindableDouble adjustBindable)
        {
            balanceAdjustments.Remove(adjustBindable);
            frequencyAdjustments.Remove(adjustBindable);
            volumeAdjustments.Remove(adjustBindable);

            InvalidateState();
        }

        protected override void Dispose(bool disposing)
        {
            volumeAdjustments.Clear();
            balanceAdjustments.Clear();
            frequencyAdjustments.Clear();

            base.Dispose(disposing);
        }
    }

    public enum AdjustableProperty
    {
        Volume,
        Balance,
        Frequency
    }
}