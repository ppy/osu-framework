// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Bindables;

namespace osu.Framework.Audio
{
    public class AdjustableAudioComponent : AudioComponent
    {
        /// <summary>
        /// Global volume of this component.
        /// </summary>
        public readonly BindableDouble Volume = new BindableDouble(1)
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

        /// <summary>
        /// Rate at which the component is played back (affects pitch). 1 is 100% playback speed, or default frequency.
        /// </summary>
        public readonly BindableDouble Frequency = new BindableDouble(1);

        protected AdjustableAudioComponent()
        {
            Volume.ValueChanged += e => InvalidateState(e.NewValue);
            Balance.ValueChanged += e => InvalidateState(e.NewValue);
            Frequency.ValueChanged += e => InvalidateState(e.NewValue);
        }

        internal void InvalidateState(double newValue = 0) => EnqueueAction(OnStateChanged);

        internal virtual void OnStateChanged()
        {
        }
    }
}
