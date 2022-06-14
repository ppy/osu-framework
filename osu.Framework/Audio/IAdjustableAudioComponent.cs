// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Audio;
using osu.Framework.Graphics.Transforms;

namespace osu.Framework.Audio
{
    public interface IAdjustableAudioComponent : IAggregateAudioAdjustment
    {
        /// <summary>
        /// The volume of this component.
        /// </summary>
        BindableNumber<double> Volume { get; }

        /// <summary>
        /// The playback balance of this sample (-1 .. 1 where 0 is centered)
        /// </summary>
        BindableNumber<double> Balance { get; }

        /// <summary>
        /// Rate at which the component is played back (affects pitch). 1 is 100% playback speed, or default frequency.
        /// </summary>
        BindableNumber<double> Frequency { get; }

        /// <summary>
        /// Rate at which the component is played back (does not affect pitch). 1 is 100% playback speed.
        /// </summary>
        BindableNumber<double> Tempo { get; }

        /// <summary>
        /// Bind all adjustments from an <see cref="IAggregateAudioAdjustment"/>.
        /// </summary>
        /// <param name="component">The adjustment source.</param>
        void BindAdjustments(IAggregateAudioAdjustment component);

        /// <summary>
        /// Unbind all adjustments from an <see cref="IAggregateAudioAdjustment"/>.
        /// </summary>
        /// <param name="component">The adjustment source.</param>
        void UnbindAdjustments(IAggregateAudioAdjustment component);

        /// <summary>
        /// Add a bindable adjustment source.
        /// </summary>
        /// <param name="type">The target type for this adjustment.</param>
        /// <param name="adjustBindable">The bindable adjustment.</param>
        void AddAdjustment(AdjustableProperty type, IBindable<double> adjustBindable);

        /// <summary>
        /// Remove a bindable adjustment source.
        /// </summary>
        /// <param name="type">The target type for this adjustment.</param>
        /// <param name="adjustBindable">The bindable adjustment.</param>
        void RemoveAdjustment(AdjustableProperty type, IBindable<double> adjustBindable);

        /// <summary>
        /// Removes all adjustments of a type.
        /// </summary>
        /// <param name="type">The target type to remove all adjustments of.</param>
        void RemoveAllAdjustments(AdjustableProperty type);
    }

    public static class AdjustableAudioComponentExtensions
    {
        #region Easing

        /// <summary>
        /// Smoothly adjusts <see cref="DrawableAudioWrapper.Volume"/> over time.
        /// </summary>
        /// <returns>A <see cref="TransformSequence{T}"/> to which further transforms can be added.</returns>
        public static TransformSequence<T> VolumeTo<T>(this T component, double newVolume, double duration = 0, Easing easing = Easing.None)
            where T : class, IAdjustableAudioComponent, IDrawable
            => component.VolumeTo(newVolume, duration, new DefaultEasingFunction(easing));

        /// <summary>
        /// Smoothly adjusts <see cref="IAdjustableAudioComponent.Balance"/> over time.
        /// </summary>
        /// <returns>A <see cref="TransformSequence{T}"/> to which further transforms can be added.</returns>
        public static TransformSequence<T> BalanceTo<T>(this T component, double newBalance, double duration = 0, Easing easing = Easing.None)
            where T : class, IAdjustableAudioComponent, IDrawable
            => component.BalanceTo(newBalance, duration, new DefaultEasingFunction(easing));

        /// <summary>
        /// Smoothly adjusts <see cref="IAdjustableAudioComponent.Frequency"/> over time.
        /// </summary>
        /// <returns>A <see cref="TransformSequence{T}"/> to which further transforms can be added.</returns>
        public static TransformSequence<T> FrequencyTo<T>(this T component, double newFrequency, double duration = 0, Easing easing = Easing.None)
            where T : class, IAdjustableAudioComponent, IDrawable
            => component.FrequencyTo(newFrequency, duration, new DefaultEasingFunction(easing));

        /// <summary>
        /// Smoothly adjusts <see cref="IAdjustableAudioComponent.Tempo"/> over time.
        /// </summary>
        /// <returns>A <see cref="TransformSequence{T}"/> to which further transforms can be added.</returns>
        public static TransformSequence<T> TempoTo<T>(this T component, double newTempo, double duration = 0, Easing easing = Easing.None)
            where T : class, IAdjustableAudioComponent, IDrawable
            => component.TempoTo(newTempo, duration, new DefaultEasingFunction(easing));

        /// <summary>
        /// Smoothly adjusts <see cref="IAdjustableAudioComponent.Volume"/> over time.
        /// </summary>
        /// <returns>A <see cref="TransformSequence{T}"/> to which further transforms can be added.</returns>
        public static TransformSequence<T> VolumeTo<T>(this TransformSequence<T> sequence, double newVolume, double duration = 0, Easing easing = Easing.None)
            where T : class, IAdjustableAudioComponent, IDrawable
            => sequence.VolumeTo(newVolume, duration, new DefaultEasingFunction(easing));

        /// <summary>
        /// Smoothly adjusts <see cref="IAdjustableAudioComponent.Balance"/> over time.
        /// </summary>
        /// <returns>A <see cref="TransformSequence{T}"/> to which further transforms can be added.</returns>
        public static TransformSequence<T> BalanceTo<T>(this TransformSequence<T> sequence, double newBalance, double duration = 0, Easing easing = Easing.None)
            where T : class, IAdjustableAudioComponent, IDrawable
            => sequence.BalanceTo(newBalance, duration, new DefaultEasingFunction(easing));

        /// <summary>
        /// Smoothly adjusts <see cref="IAdjustableAudioComponent.Frequency"/> over time.
        /// </summary>
        /// <returns>A <see cref="TransformSequence{T}"/> to which further transforms can be added.</returns>
        public static TransformSequence<T> FrequencyTo<T>(this TransformSequence<T> sequence, double newFrequency, double duration = 0, Easing easing = Easing.None)
            where T : class, IAdjustableAudioComponent, IDrawable
            => sequence.FrequencyTo(newFrequency, duration, new DefaultEasingFunction(easing));

        /// <summary>
        /// Smoothly adjusts <see cref="IAdjustableAudioComponent.Tempo"/> over time.
        /// </summary>
        /// <returns>A <see cref="TransformSequence{T}"/> to which further transforms can be added.</returns>
        public static TransformSequence<T> TempoTo<T>(this TransformSequence<T> sequence, double newTempo, double duration = 0, Easing easing = Easing.None)
            where T : class, IAdjustableAudioComponent, IDrawable
            => sequence.TempoTo(newTempo, duration, new DefaultEasingFunction(easing));

        #endregion

        #region Generic Easing

        /// <summary>
        /// Smoothly adjusts <see cref="IAdjustableAudioComponent.Volume"/> over time.
        /// </summary>
        /// <returns>A <see cref="TransformSequence{T}"/> to which further transforms can be added.</returns>
        public static TransformSequence<T> VolumeTo<T, TEasing>(this T component, double newVolume, double duration, TEasing easing)
            where T : class, IAdjustableAudioComponent, IDrawable
            where TEasing : IEasingFunction
            => component.TransformBindableTo(component.Volume, newVolume, duration, easing);

        /// <summary>
        /// Smoothly adjusts <see cref="IAdjustableAudioComponent.Balance"/> over time.
        /// </summary>
        /// <returns>A <see cref="TransformSequence{T}"/> to which further transforms can be added.</returns>
        public static TransformSequence<T> BalanceTo<T, TEasing>(this T component, double newBalance, double duration, TEasing easing)
            where T : class, IAdjustableAudioComponent, IDrawable
            where TEasing : IEasingFunction
            => component.TransformBindableTo(component.Balance, newBalance, duration, easing);

        /// <summary>
        /// Smoothly adjusts <see cref="IAdjustableAudioComponent.Frequency"/> over time.
        /// </summary>
        /// <returns>A <see cref="TransformSequence{T}"/> to which further transforms can be added.</returns>
        public static TransformSequence<T> FrequencyTo<T, TEasing>(this T component, double newFrequency, double duration, TEasing easing)
            where T : class, IAdjustableAudioComponent, IDrawable
            where TEasing : IEasingFunction
            => component.TransformBindableTo(component.Frequency, newFrequency, duration, easing);

        /// <summary>
        /// Smoothly adjusts <see cref="IAdjustableAudioComponent.Tempo"/> over time.
        /// </summary>
        /// <returns>A <see cref="TransformSequence{T}"/> to which further transforms can be added.</returns>
        public static TransformSequence<T> TempoTo<T, TEasing>(this T component, double newTempo, double duration, TEasing easing)
            where T : class, IAdjustableAudioComponent, IDrawable
            where TEasing : IEasingFunction
            => component.TransformBindableTo(component.Tempo, newTempo, duration, easing);

        /// <summary>
        /// Smoothly adjusts <see cref="IAdjustableAudioComponent.Volume"/> over time.
        /// </summary>
        /// <returns>A <see cref="TransformSequence{T}"/> to which further transforms can be added.</returns>
        public static TransformSequence<T> VolumeTo<T, TEasing>(this TransformSequence<T> sequence, double newVolume, double duration, TEasing easing)
            where T : class, IAdjustableAudioComponent, IDrawable
            where TEasing : IEasingFunction
            => sequence.Append(o => o.TransformBindableTo(o.Volume, newVolume, duration, easing));

        /// <summary>
        /// Smoothly adjusts <see cref="IAdjustableAudioComponent.Balance"/> over time.
        /// </summary>
        /// <returns>A <see cref="TransformSequence{T}"/> to which further transforms can be added.</returns>
        public static TransformSequence<T> BalanceTo<T, TEasing>(this TransformSequence<T> sequence, double newBalance, double duration, TEasing easing)
            where T : class, IAdjustableAudioComponent, IDrawable
            where TEasing : IEasingFunction
            => sequence.Append(o => o.TransformBindableTo(o.Balance, newBalance, duration, easing));

        /// <summary>
        /// Smoothly adjusts <see cref="IAdjustableAudioComponent.Frequency"/> over time.
        /// </summary>
        /// <returns>A <see cref="TransformSequence{T}"/> to which further transforms can be added.</returns>
        public static TransformSequence<T> FrequencyTo<T, TEasing>(this TransformSequence<T> sequence, double newFrequency, double duration, TEasing easing)
            where T : class, IAdjustableAudioComponent, IDrawable
            where TEasing : IEasingFunction
            => sequence.Append(o => o.TransformBindableTo(o.Frequency, newFrequency, duration, easing));

        /// <summary>
        /// Smoothly adjusts <see cref="IAdjustableAudioComponent.Tempo"/> over time.
        /// </summary>
        /// <returns>A <see cref="TransformSequence{T}"/> to which further transforms can be added.</returns>
        public static TransformSequence<T> TempoTo<T, TEasing>(this TransformSequence<T> sequence, double newTempo, double duration, TEasing easing)
            where T : class, IAdjustableAudioComponent, IDrawable
            where TEasing : IEasingFunction
            => sequence.Append(o => o.TransformBindableTo(o.Tempo, newTempo, duration, easing));

        #endregion
    }
}
