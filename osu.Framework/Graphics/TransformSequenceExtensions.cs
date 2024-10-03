// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osuTK;
using osuTK.Graphics;
using osu.Framework.Graphics.Colour;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Transforms;
using osu.Framework.Threading;
using System;
using osu.Framework.Bindables;

namespace osu.Framework.Graphics
{
    public static class TransformSequenceExtensions
    {
        public static TransformSequence<T> Expire<T>(this TransformSequence<T> t, bool calculateLifetimeStart = false)
            where T : Drawable
        {
            t.Continue().Expire(calculateLifetimeStart);
            return t;
        }

        public static TransformSequence<T> Schedule<T, TData>(this TransformSequence<T> t, Action<TData> scheduledAction, TData data)
            where T : Drawable
        {
            t.Continue().Schedule(scheduledAction, data);
            return t;
        }

        public static TransformSequence<T> Schedule<T>(this TransformSequence<T> t, Action scheduledAction)
            where T : Drawable
        {
            t.Continue().Schedule(scheduledAction);
            return t;
        }

        public static TransformSequence<T> Schedule<T, TData>(this TransformSequence<T> t, Action<TData> scheduledAction, TData data, out ScheduledDelegate scheduledDelegate)
            where T : Drawable
        {
            scheduledDelegate = t.Continue().Schedule(scheduledAction, data);
            return t;
        }

        public static TransformSequence<T> Schedule<T>(this TransformSequence<T> t, Action scheduledAction, out ScheduledDelegate scheduledDelegate)
            where T : Drawable
        {
            scheduledDelegate = t.Continue().Schedule(scheduledAction);
            return t;
        }

        public static TransformSequence<T> TransformTo<T, TValue>(this TransformSequence<T> t, string propertyOrFieldName, TValue newValue, double duration = 0, Easing easing = Easing.None)
            where T : Drawable
            => t.Continue().TransformTo(propertyOrFieldName, newValue, duration, easing);

        public static TransformSequence<T> Spin<T>(this TransformSequence<T> t, double revolutionDuration, RotationDirection direction, float startRotation = 0)
            where T : Drawable
            => t.Continue().Spin(revolutionDuration, direction, startRotation);

        public static TransformSequence<T> Spin<T>(this TransformSequence<T> t, double revolutionDuration, RotationDirection direction, float startRotation, int numRevolutions)
            where T : Drawable
            => t.Continue().Spin(revolutionDuration, direction, startRotation, numRevolutions);

        #region Easing

        /// <summary>
        /// Smoothly adjusts <see cref="Drawable.Alpha"/> to 1 over time.
        /// </summary>
        /// <returns>A <see cref="TransformSequence{T}"/> to which further transforms can be added.</returns>
        public static TransformSequence<T> FadeIn<T>(this TransformSequence<T> t, double duration = 0, Easing easing = Easing.None)
            where T : Drawable
            => t.FadeIn(duration, new DefaultEasingFunction(easing));

        /// <summary>
        /// Smoothly adjusts <see cref="Drawable.Alpha"/> from 0 to 1 over time.
        /// </summary>
        /// <returns>A <see cref="TransformSequence{T}"/> to which further transforms can be added.</returns>
        public static TransformSequence<T> FadeInFromZero<T>(this TransformSequence<T> t, double duration = 0, Easing easing = Easing.None)
            where T : Drawable
            => t.FadeInFromZero(duration, new DefaultEasingFunction(easing));

        /// <summary>
        /// Smoothly adjusts <see cref="Drawable.Alpha"/> to 0 over time.
        /// </summary>
        /// <returns>A <see cref="TransformSequence{T}"/> to which further transforms can be added.</returns>
        public static TransformSequence<T> FadeOut<T>(this TransformSequence<T> t, double duration = 0, Easing easing = Easing.None)
            where T : Drawable
            => t.FadeOut(duration, new DefaultEasingFunction(easing));

        /// <summary>
        /// Smoothly adjusts <see cref="Drawable.Alpha"/> from 1 to 0 over time.
        /// </summary>
        /// <returns>A <see cref="TransformSequence{T}"/> to which further transforms can be added.</returns>
        public static TransformSequence<T> FadeOutFromOne<T>(this TransformSequence<T> t, double duration = 0, Easing easing = Easing.None)
            where T : Drawable
            => t.FadeOutFromOne(duration, new DefaultEasingFunction(easing));

        /// <summary>
        /// Smoothly adjusts <see cref="Drawable.Alpha"/> over time.
        /// </summary>
        /// <returns>A <see cref="TransformSequence{T}"/> to which further transforms can be added.</returns>
        public static TransformSequence<T> FadeTo<T>(this TransformSequence<T> t, float newAlpha, double duration = 0, Easing easing = Easing.None)
            where T : Drawable
            => t.FadeTo(newAlpha, duration, new DefaultEasingFunction(easing));

        /// <summary>
        /// Smoothly adjusts <see cref="Drawable.Colour"/> over time.
        /// </summary>
        /// <returns>A <see cref="TransformSequence{T}"/> to which further transforms can be added.</returns>
        public static TransformSequence<T> FadeColour<T>(this TransformSequence<T> t, ColourInfo newColour, double duration = 0, Easing easing = Easing.None)
            where T : Drawable
            => t.FadeColour(newColour, duration, new DefaultEasingFunction(easing));

        /// <summary>
        /// Instantaneously flashes <see cref="Drawable.Colour"/>, then smoothly changes it back over time.
        /// </summary>
        /// <returns>A <see cref="TransformSequence{T}"/> to which further transforms can be added.</returns>
        public static TransformSequence<T> FlashColour<T>(this TransformSequence<T> t, ColourInfo flashColour, double duration, Easing easing = Easing.None)
            where T : Drawable
            => t.FlashColour(flashColour, duration, new DefaultEasingFunction(easing));

        /// <summary>
        /// Smoothly adjusts <see cref="Drawable.Rotation"/> over time.
        /// </summary>
        /// <returns>A <see cref="TransformSequence{T}"/> to which further transforms can be added.</returns>
        public static TransformSequence<T> RotateTo<T>(this TransformSequence<T> t, float newRotation, double duration = 0, Easing easing = Easing.None)
            where T : Drawable
            => t.RotateTo(newRotation, duration, new DefaultEasingFunction(easing));

        /// <summary>
        /// Smoothly adjusts <see cref="Drawable.Scale"/> over time.
        /// </summary>
        /// <returns>A <see cref="TransformSequence{T}"/> to which further transforms can be added.</returns>
        public static TransformSequence<T> ScaleTo<T>(this TransformSequence<T> t, float newScale, double duration = 0, Easing easing = Easing.None)
            where T : Drawable
            => t.ScaleTo(newScale, duration, new DefaultEasingFunction(easing));

        /// <summary>
        /// Smoothly adjusts <see cref="Drawable.Scale"/> over time.
        /// </summary>
        /// <returns>A <see cref="TransformSequence{T}"/> to which further transforms can be added.</returns>
        public static TransformSequence<T> ScaleTo<T>(this TransformSequence<T> t, Vector2 newScale, double duration = 0, Easing easing = Easing.None)
            where T : Drawable
            => t.ScaleTo(newScale, duration, new DefaultEasingFunction(easing));

        /// <summary>
        /// Smoothly adjusts <see cref="Drawable.Size"/> over time.
        /// </summary>
        /// <returns>A <see cref="TransformSequence{T}"/> to which further transforms can be added.</returns>
        public static TransformSequence<T> ResizeTo<T>(this TransformSequence<T> t, float newSize, double duration = 0, Easing easing = Easing.None)
            where T : Drawable
            => t.ResizeTo(newSize, duration, new DefaultEasingFunction(easing));

        /// <summary>
        /// Smoothly adjusts <see cref="Drawable.Size"/> over time.
        /// </summary>
        /// <returns>A <see cref="TransformSequence{T}"/> to which further transforms can be added.</returns>
        public static TransformSequence<T> ResizeTo<T>(this TransformSequence<T> t, Vector2 newSize, double duration = 0, Easing easing = Easing.None)
            where T : Drawable
            => t.ResizeTo(newSize, duration, new DefaultEasingFunction(easing));

        /// <summary>
        /// Smoothly adjusts <see cref="Drawable.Width"/> over time.
        /// </summary>
        /// <returns>A <see cref="TransformSequence{T}"/> to which further transforms can be added.</returns>
        public static TransformSequence<T> ResizeWidthTo<T>(this TransformSequence<T> t, float newWidth, double duration = 0, Easing easing = Easing.None)
            where T : Drawable
            => t.ResizeWidthTo(newWidth, duration, new DefaultEasingFunction(easing));

        /// <summary>
        /// Smoothly adjusts <see cref="Drawable.Height"/> over time.
        /// </summary>
        /// <returns>A <see cref="TransformSequence{T}"/> to which further transforms can be added.</returns>
        public static TransformSequence<T> ResizeHeightTo<T>(this TransformSequence<T> t, float newHeight, double duration = 0, Easing easing = Easing.None)
            where T : Drawable
            => t.ResizeHeightTo(newHeight, duration, new DefaultEasingFunction(easing));

        /// <summary>
        /// Smoothly adjusts <see cref="Drawable.Position"/> over time.
        /// </summary>
        /// <returns>A <see cref="TransformSequence{T}"/> to which further transforms can be added.</returns>
        public static TransformSequence<T> MoveTo<T>(this TransformSequence<T> t, Vector2 newPosition, double duration = 0, Easing easing = Easing.None)
            where T : Drawable
            => t.MoveTo(newPosition, duration, new DefaultEasingFunction(easing));

        /// <summary>
        /// Smoothly adjusts <see cref="Drawable.X"/> or <see cref="Drawable.Y"/> over time.
        /// </summary>
        /// <returns>A <see cref="TransformSequence{T}"/> to which further transforms can be added.</returns>
        public static TransformSequence<T> MoveTo<T>(this TransformSequence<T> t, Direction direction, float destination, double duration = 0, Easing easing = Easing.None)
            where T : Drawable
            => t.MoveTo(direction, destination, duration, new DefaultEasingFunction(easing));

        /// <summary>
        /// Smoothly adjusts <see cref="Drawable.X"/> over time.
        /// </summary>
        /// <returns>A <see cref="TransformSequence{T}"/> to which further transforms can be added.</returns>
        public static TransformSequence<T> MoveToX<T>(this TransformSequence<T> t, float destination, double duration = 0, Easing easing = Easing.None)
            where T : Drawable
            => t.MoveToX(destination, duration, new DefaultEasingFunction(easing));

        /// <summary>
        /// Smoothly adjusts <see cref="Drawable.Y"/> over time.
        /// </summary>
        /// <returns>A <see cref="TransformSequence{T}"/> to which further transforms can be added.</returns>
        public static TransformSequence<T> MoveToY<T>(this TransformSequence<T> t, float destination, double duration = 0, Easing easing = Easing.None)
            where T : Drawable
            => t.MoveToY(destination, duration, new DefaultEasingFunction(easing));

        /// <summary>
        /// Smoothly adjusts <see cref="Drawable.Position"/> by an offset to its final value over time.
        /// </summary>
        /// <returns>A <see cref="TransformSequence{T}"/> to which further transforms can be added.</returns>
        public static TransformSequence<T> MoveToOffset<T>(this TransformSequence<T> t, Vector2 offset, double duration = 0, Easing easing = Easing.None)
            where T : Drawable
            => t.MoveToOffset(offset, duration, new DefaultEasingFunction(easing));

        /// <summary>
        /// Smoothly adjusts the alpha channel of the colour of <see cref="IContainer.EdgeEffect"/> over time.
        /// </summary>
        /// <returns>A <see cref="TransformSequence{T}"/> to which further transforms can be added.</returns>
        public static TransformSequence<T> FadeEdgeEffectTo<T>(this TransformSequence<T> t, float newAlpha, double duration, Easing easing = Easing.None)
            where T : class, IContainer
            => t.FadeEdgeEffectTo(newAlpha, duration, new DefaultEasingFunction(easing));

        /// <summary>
        /// Smoothly adjusts the colour of <see cref="IContainer.EdgeEffect"/> over time.
        /// </summary>
        /// <returns>A <see cref="TransformSequence{T}"/> to which further transforms can be added.</returns>
        public static TransformSequence<T> FadeEdgeEffectTo<T>(this TransformSequence<T> t, Color4 newColour, double duration = 0, Easing easing = Easing.None)
            where T : class, IContainer
            => t.FadeEdgeEffectTo(newColour, duration, new DefaultEasingFunction(easing));

        /// <summary>
        /// Smoothly adjusts <see cref="IContainer.RelativeChildSize"/> over time.
        /// </summary>
        /// <returns>A <see cref="TransformSequence{T}"/> to which further transforms can be added.</returns>
        public static TransformSequence<T> TransformRelativeChildSizeTo<T>(this TransformSequence<T> t, Vector2 newSize, double duration = 0, Easing easing = Easing.None)
            where T : class, IContainer
            => t.TransformRelativeChildSizeTo(newSize, duration, new DefaultEasingFunction(easing));

        /// <summary>
        /// Smoothly adjusts <see cref="IContainer.RelativeChildOffset"/> over time.
        /// </summary>
        /// <returns>A <see cref="TransformSequence{T}"/> to which further transforms can be added.</returns>
        public static TransformSequence<T> TransformRelativeChildOffsetTo<T>(this TransformSequence<T> t, Vector2 newOffset, double duration = 0, Easing easing = Easing.None)
            where T : class, IContainer
            => t.TransformRelativeChildOffsetTo(newOffset, duration, new DefaultEasingFunction(easing));

        /// <summary>
        /// Smoothly adjusts <see cref="IBufferedContainer.BlurSigma"/> over time.
        /// </summary>
        /// <returns>A <see cref="TransformSequence{T}"/> to which further transforms can be added.</returns>
        public static TransformSequence<T> BlurTo<T>(this TransformSequence<T> t, Vector2 newBlurSigma, double duration = 0, Easing easing = Easing.None)
            where T : class, IBufferedContainer
            => t.BlurTo(newBlurSigma, duration, new DefaultEasingFunction(easing));

        /// <summary>
        /// Smoothly adjusts <see cref="IFillFlowContainer.Spacing"/> over time.
        /// </summary>
        /// <returns>A <see cref="TransformSequence{T}"/> to which further transforms can be added.</returns>
        public static TransformSequence<T> TransformSpacingTo<T>(this TransformSequence<T> t, Vector2 newSpacing, double duration = 0, Easing easing = Easing.None)
            where T : class, IFillFlowContainer
            => t.TransformSpacingTo(newSpacing, duration, new DefaultEasingFunction(easing));

        /// <summary>
        /// Smoothly adjusts the value of a <see cref="Bindable{TValue}"/> over time.
        /// </summary>
        /// <returns>A <see cref="TransformSequence{T}"/> to which further transforms can be added.</returns>
        public static TransformSequence<T> TransformBindableTo<T, TValue>(this TransformSequence<T> t, Bindable<TValue> bindable, TValue newValue, double duration = 0,
                                                                          Easing easing = Easing.None)
            where T : class, ITransformable
            => t.TransformBindableTo(bindable, newValue, duration, new DefaultEasingFunction(easing));

        #endregion

        #region Generic Easing

        /// <summary>
        /// Smoothly adjusts <see cref="Drawable.Alpha"/> to 1 over time.
        /// </summary>
        /// <returns>A <see cref="TransformSequence{T}"/> to which further transforms can be added.</returns>
        public static TransformSequence<T> FadeIn<T, TEasing>(this TransformSequence<T> t, double duration, TEasing easing)
            where T : Drawable
            where TEasing : IEasingFunction
            => t.Continue().FadeIn(duration, easing);

        /// <summary>
        /// Smoothly adjusts <see cref="Drawable.Alpha"/> from 0 to 1 over time.
        /// </summary>
        /// <returns>A <see cref="TransformSequence{T}"/> to which further transforms can be added.</returns>
        public static TransformSequence<T> FadeInFromZero<T, TEasing>(this TransformSequence<T> t, double duration, TEasing easing)
            where T : Drawable
            where TEasing : IEasingFunction
            => t.Continue().FadeInFromZero(duration, easing);

        /// <summary>
        /// Smoothly adjusts <see cref="Drawable.Alpha"/> to 0 over time.
        /// </summary>
        /// <returns>A <see cref="TransformSequence{T}"/> to which further transforms can be added.</returns>
        public static TransformSequence<T> FadeOut<T, TEasing>(this TransformSequence<T> t, double duration, TEasing easing)
            where T : Drawable
            where TEasing : IEasingFunction
            => t.Continue().FadeOut(duration, easing);

        /// <summary>
        /// Smoothly adjusts <see cref="Drawable.Alpha"/> from 1 to 0 over time.
        /// </summary>
        /// <returns>A <see cref="TransformSequence{T}"/> to which further transforms can be added.</returns>
        public static TransformSequence<T> FadeOutFromOne<T, TEasing>(this TransformSequence<T> t, double duration, TEasing easing)
            where T : Drawable
            where TEasing : IEasingFunction
            => t.Continue().FadeOutFromOne(duration, easing);

        /// <summary>
        /// Smoothly adjusts <see cref="Drawable.Alpha"/> over time.
        /// </summary>
        /// <returns>A <see cref="TransformSequence{T}"/> to which further transforms can be added.</returns>
        public static TransformSequence<T> FadeTo<T, TEasing>(this TransformSequence<T> t, float newAlpha, double duration, TEasing easing)
            where T : Drawable
            where TEasing : IEasingFunction
            => t.Continue().FadeTo(newAlpha, duration, easing);

        /// <summary>
        /// Smoothly adjusts <see cref="Drawable.Colour"/> over time.
        /// </summary>
        /// <returns>A <see cref="TransformSequence{T}"/> to which further transforms can be added.</returns>
        public static TransformSequence<T> FadeColour<T, TEasing>(this TransformSequence<T> t, ColourInfo newColour, double duration, TEasing easing)
            where T : Drawable
            where TEasing : IEasingFunction
            => t.Continue().FadeColour(newColour, duration, easing);

        /// <summary>
        /// Instantaneously flashes <see cref="Drawable.Colour"/>, then smoothly changes it back over time.
        /// </summary>
        /// <returns>A <see cref="TransformSequence{T}"/> to which further transforms can be added.</returns>
        public static TransformSequence<T> FlashColour<T, TEasing>(this TransformSequence<T> t, ColourInfo flashColour, double duration, TEasing easing)
            where T : Drawable
            where TEasing : IEasingFunction
            => t.Continue().FlashColour(flashColour, duration, easing);

        /// <summary>
        /// Smoothly adjusts <see cref="Drawable.Rotation"/> over time.
        /// </summary>
        /// <returns>A <see cref="TransformSequence{T}"/> to which further transforms can be added.</returns>
        public static TransformSequence<T> RotateTo<T, TEasing>(this TransformSequence<T> t, float newRotation, double duration, TEasing easing)
            where T : Drawable
            where TEasing : IEasingFunction
            => t.Continue().RotateTo(newRotation, duration, easing);

        /// <summary>
        /// Smoothly adjusts <see cref="Drawable.Scale"/> over time.
        /// </summary>
        /// <returns>A <see cref="TransformSequence{T}"/> to which further transforms can be added.</returns>
        public static TransformSequence<T> ScaleTo<T, TEasing>(this TransformSequence<T> t, float newScale, double duration, TEasing easing)
            where T : Drawable
            where TEasing : IEasingFunction
            => t.Continue().ScaleTo(newScale, duration, easing);

        /// <summary>
        /// Smoothly adjusts <see cref="Drawable.Scale"/> over time.
        /// </summary>
        /// <returns>A <see cref="TransformSequence{T}"/> to which further transforms can be added.</returns>
        public static TransformSequence<T> ScaleTo<T, TEasing>(this TransformSequence<T> t, Vector2 newScale, double duration, TEasing easing)
            where T : Drawable
            where TEasing : IEasingFunction
            => t.Continue().ScaleTo(newScale, duration, easing);

        /// <summary>
        /// Smoothly adjusts <see cref="Drawable.Size"/> over time.
        /// </summary>
        /// <returns>A <see cref="TransformSequence{T}"/> to which further transforms can be added.</returns>
        public static TransformSequence<T> ResizeTo<T, TEasing>(this TransformSequence<T> t, float newSize, double duration, TEasing easing)
            where T : Drawable
            where TEasing : IEasingFunction
            => t.Continue().ResizeTo(newSize, duration, easing);

        /// <summary>
        /// Smoothly adjusts <see cref="Drawable.Size"/> over time.
        /// </summary>
        /// <returns>A <see cref="TransformSequence{T}"/> to which further transforms can be added.</returns>
        public static TransformSequence<T> ResizeTo<T, TEasing>(this TransformSequence<T> t, Vector2 newSize, double duration, TEasing easing)
            where T : Drawable
            where TEasing : IEasingFunction
            => t.Continue().ResizeTo(newSize, duration, easing);

        /// <summary>
        /// Smoothly adjusts <see cref="Drawable.Width"/> over time.
        /// </summary>
        /// <returns>A <see cref="TransformSequence{T}"/> to which further transforms can be added.</returns>
        public static TransformSequence<T> ResizeWidthTo<T, TEasing>(this TransformSequence<T> t, float newWidth, double duration, TEasing easing)
            where T : Drawable
            where TEasing : IEasingFunction
            => t.Continue().ResizeWidthTo(newWidth, duration, easing);

        /// <summary>
        /// Smoothly adjusts <see cref="Drawable.Height"/> over time.
        /// </summary>
        /// <returns>A <see cref="TransformSequence{T}"/> to which further transforms can be added.</returns>
        public static TransformSequence<T> ResizeHeightTo<T, TEasing>(this TransformSequence<T> t, float newHeight, double duration, TEasing easing)
            where T : Drawable
            where TEasing : IEasingFunction
            => t.Continue().ResizeHeightTo(newHeight, duration, easing);

        /// <summary>
        /// Smoothly adjusts <see cref="Drawable.Position"/> over time.
        /// </summary>
        /// <returns>A <see cref="TransformSequence{T}"/> to which further transforms can be added.</returns>
        public static TransformSequence<T> MoveTo<T, TEasing>(this TransformSequence<T> t, Vector2 newPosition, double duration, TEasing easing)
            where T : Drawable
            where TEasing : IEasingFunction
            => t.Continue().MoveTo(newPosition, duration, easing);

        /// <summary>
        /// Smoothly adjusts <see cref="Drawable.X"/> or <see cref="Drawable.Y"/> over time.
        /// </summary>
        /// <returns>A <see cref="TransformSequence{T}"/> to which further transforms can be added.</returns>
        public static TransformSequence<T> MoveTo<T, TEasing>(this TransformSequence<T> t, Direction direction, float destination, double duration, TEasing easing)
            where T : Drawable
            where TEasing : IEasingFunction
            => t.Continue().MoveTo(direction, destination, duration, easing);

        /// <summary>
        /// Smoothly adjusts <see cref="Drawable.X"/> over time.
        /// </summary>
        /// <returns>A <see cref="TransformSequence{T}"/> to which further transforms can be added.</returns>
        public static TransformSequence<T> MoveToX<T, TEasing>(this TransformSequence<T> t, float destination, double duration, TEasing easing)
            where T : Drawable
            where TEasing : IEasingFunction
            => t.Continue().MoveToX(destination, duration, easing);

        /// <summary>
        /// Smoothly adjusts <see cref="Drawable.Y"/> over time.
        /// </summary>
        /// <returns>A <see cref="TransformSequence{T}"/> to which further transforms can be added.</returns>
        public static TransformSequence<T> MoveToY<T, TEasing>(this TransformSequence<T> t, float destination, double duration, TEasing easing)
            where T : Drawable
            where TEasing : IEasingFunction
            => t.Continue().MoveToY(destination, duration, easing);

        /// <summary>
        /// Smoothly adjusts <see cref="Drawable.Position"/> by an offset to its final value over time.
        /// </summary>
        /// <returns>A <see cref="TransformSequence{T}"/> to which further transforms can be added.</returns>
        public static TransformSequence<T> MoveToOffset<T, TEasing>(this TransformSequence<T> t, Vector2 offset, double duration, TEasing easing)
            where T : Drawable
            where TEasing : IEasingFunction
            => t.Continue().MoveToOffset(offset, duration, easing);

        /// <summary>
        /// Smoothly adjusts the alpha channel of the colour of <see cref="IContainer.EdgeEffect"/> over time.
        /// </summary>
        /// <returns>A <see cref="TransformSequence{T}"/> to which further transforms can be added.</returns>
        public static TransformSequence<T> FadeEdgeEffectTo<T, TEasing>(this TransformSequence<T> t, float newAlpha, double duration, TEasing easing)
            where T : class, IContainer
            where TEasing : IEasingFunction
            => t.Continue().FadeEdgeEffectTo(newAlpha, duration, easing);

        /// <summary>
        /// Smoothly adjusts the colour of <see cref="IContainer.EdgeEffect"/> over time.
        /// </summary>
        /// <returns>A <see cref="TransformSequence{T}"/> to which further transforms can be added.</returns>
        public static TransformSequence<T> FadeEdgeEffectTo<T, TEasing>(this TransformSequence<T> t, Color4 newColour, double duration, TEasing easing)
            where T : class, IContainer
            where TEasing : IEasingFunction
            => t.Continue().FadeEdgeEffectTo(newColour, duration, easing);

        /// <summary>
        /// Smoothly adjusts <see cref="IContainer.RelativeChildSize"/> over time.
        /// </summary>
        /// <returns>A <see cref="TransformSequence{T}"/> to which further transforms can be added.</returns>
        public static TransformSequence<T> TransformRelativeChildSizeTo<T, TEasing>(this TransformSequence<T> t, Vector2 newSize, double duration, TEasing easing)
            where T : class, IContainer
            where TEasing : IEasingFunction
            => t.Continue().TransformRelativeChildSizeTo(newSize, duration, easing);

        /// <summary>
        /// Smoothly adjusts <see cref="IContainer.RelativeChildOffset"/> over time.
        /// </summary>
        /// <returns>A <see cref="TransformSequence{T}"/> to which further transforms can be added.</returns>
        public static TransformSequence<T> TransformRelativeChildOffsetTo<T, TEasing>(this TransformSequence<T> t, Vector2 newOffset, double duration, TEasing easing)
            where T : class, IContainer
            where TEasing : IEasingFunction
            => t.Continue().TransformRelativeChildOffsetTo(newOffset, duration, easing);

        /// <summary>
        /// Smoothly adjusts <see cref="IBufferedContainer.BlurSigma"/> over time.
        /// </summary>
        /// <returns>A <see cref="TransformSequence{T}"/> to which further transforms can be added.</returns>
        public static TransformSequence<T> BlurTo<T, TEasing>(this TransformSequence<T> t, Vector2 newBlurSigma, double duration, TEasing easing)
            where T : class, IBufferedContainer
            where TEasing : IEasingFunction
            => t.Continue().BlurTo(newBlurSigma, duration, easing);

        /// <summary>
        /// Smoothly adjusts <see cref="IFillFlowContainer.Spacing"/> over time.
        /// </summary>
        /// <returns>A <see cref="TransformSequence{T}"/> to which further transforms can be added.</returns>
        public static TransformSequence<T> TransformSpacingTo<T, TEasing>(this TransformSequence<T> t, Vector2 newSpacing, double duration, TEasing easing)
            where T : class, IFillFlowContainer
            where TEasing : IEasingFunction
            => t.Continue().TransformSpacingTo(newSpacing, duration, easing);

        /// <summary>
        /// Smoothly adjusts the value of a <see cref="Bindable{TValue}"/> over time.
        /// </summary>
        /// <returns>A <see cref="TransformSequence{T}"/> to which further transforms can be added.</returns>
        public static TransformSequence<T> TransformBindableTo<T, TValue, TEasing>(this TransformSequence<T> t, Bindable<TValue> bindable, TValue newValue, double duration, TEasing easing)
            where T : class, ITransformable
            where TEasing : IEasingFunction
            => t.Continue().TransformBindableTo(bindable, newValue, duration, easing);

        #endregion

        #region Compatibility

        public static TransformSequence<T> Loop<T>(this TransformSequence<T> t, params TransformSequence<T>.Generator[] childGenerators)
            where T : Drawable
            => t.Loop(0, -1, childGenerators);

        public static TransformSequence<T> Loop<T>(this TransformSequence<T> t, double pause, params TransformSequence<T>.Generator[] childGenerators)
            where T : Drawable
            => t.Loop(pause, -1, childGenerators);

        public static TransformSequence<T> Loop<T>(this TransformSequence<T> t, double pause, int numIters, params TransformSequence<T>.Generator[] childGenerators)
            where T : Drawable
        {
            var branch = t.CreateBranch();

            foreach (var gen in childGenerators)
                branch.Commit(branch.Head.Continue(gen));
            branch.Commit(branch.Head.Loop(pause, numIters));

            return branch.Merge();
        }

        public static TransformSequence<T> Delay<T>(this TransformSequence<T> t, double delay, params TransformSequence<T>.Generator[] childGenerators)
            where T : Drawable
            => t.Then(delay, childGenerators);

        public static TransformSequence<T> Then<T>(this TransformSequence<T> t, params TransformSequence<T>.Generator[] childGenerators)
            where T : Drawable
            => t.Then(0, childGenerators);

        public static TransformSequence<T> Then<T>(this TransformSequence<T> t, double delay, params TransformSequence<T>.Generator[] childGenerators)
            where T : Drawable
        {
            var branch = t.CreateBranch();

            branch.Commit(branch.Head.Delay(delay));
            foreach (var gen in childGenerators)
                branch.Commit(branch.Head.Continue(gen));

            return branch.Merge();
        }

        #endregion
    }
}
