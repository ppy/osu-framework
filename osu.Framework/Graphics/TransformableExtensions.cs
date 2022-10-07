// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using osuTK;
using osuTK.Graphics;
using osu.Framework.Graphics.Colour;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Transforms;
using System;
using System.Linq;
using JetBrains.Annotations;
using osu.Framework.Bindables;
using osu.Framework.Graphics.Effects;
using osu.Framework.Utils;

namespace osu.Framework.Graphics
{
    public static class TransformableExtensions
    {
        /// <summary>
        /// Transforms a given property or field member of a given <see cref="ITransformable"/> <typeparamref name="TThis"/> to <paramref name="newValue"/>.
        /// The value of the given member is smoothly changed over time using the given <paramref name="easing"/> for tweening.
        /// </summary>
        /// <typeparam name="TThis">The type of the <see cref="ITransformable"/> to apply the <see cref="Transform{TValue, T}"/> to.</typeparam>
        /// <typeparam name="TValue">The value type which is being transformed.</typeparam>
        /// <param name="t">The <see cref="ITransformable"/> to apply the <see cref="Transform{TValue, T}"/> to.</param>
        /// <param name="propertyOrFieldName">The property or field name of the member ot <typeparamref name="TThis"/> to transform.</param>
        /// <param name="newValue">The value to transform to.</param>
        /// <param name="duration">The transform duration.</param>
        /// <param name="easing">The transform easing to be used for tweening.</param>
        /// <returns>A <see cref="TransformSequence{T}"/> to which further transforms can be added.</returns>
        public static TransformSequence<TThis> TransformTo<TThis, TValue>(this TThis t, string propertyOrFieldName, TValue newValue, double duration = 0, Easing easing = Easing.None)
            where TThis : class, ITransformable
            => t.TransformTo(t.MakeTransform(propertyOrFieldName, newValue, duration, new DefaultEasingFunction(easing)));

        /// <summary>
        /// Transforms a given property or field member of a given <see cref="ITransformable"/> <typeparamref name="TThis"/> to <paramref name="newValue"/>.
        /// The value of the given member is smoothly changed over time using the given <paramref name="easing"/> for tweening.
        /// </summary>
        /// <typeparam name="TThis">The type of the <see cref="ITransformable"/> to apply the <see cref="Transform{TValue, T}"/> to.</typeparam>
        /// <typeparam name="TValue">The value type which is being transformed.</typeparam>
        /// <typeparam name="TEasing">The type of easing.</typeparam>
        /// <param name="t">The <see cref="ITransformable"/> to apply the <see cref="Transform{TValue, T}"/> to.</param>
        /// <param name="propertyOrFieldName">The property or field name of the member ot <typeparamref name="TThis"/> to transform.</param>
        /// <param name="newValue">The value to transform to.</param>
        /// <param name="duration">The transform duration.</param>
        /// <param name="easing">The transform easing to be used for tweening.</param>
        /// <param name="grouping">An optional grouping specification to be used when the same property may be touched by multiple transform types.</param>
        /// <returns>A <see cref="TransformSequence{T}"/> to which further transforms can be added.</returns>
        public static TransformSequence<TThis> TransformTo<TThis, TValue, TEasing>(this TThis t, string propertyOrFieldName, TValue newValue, double duration, in TEasing easing, string grouping = null)
            where TThis : class, ITransformable
            where TEasing : IEasingFunction
            => t.TransformTo(t.MakeTransform(propertyOrFieldName, newValue, duration, easing, grouping));

        /// <summary>
        /// Applies a <see cref="Transform"/> to a given <see cref="ITransformable"/>.
        /// </summary>
        /// <typeparam name="TThis">The type of the <see cref="ITransformable"/> to apply the <see cref="Transform"/> to.</typeparam>
        /// <param name="t">The <see cref="ITransformable"/> to apply the <see cref="Transform{TValue, T}"/> to.</param>
        /// <param name="transform">The transform to use.</param>
        /// <returns>A <see cref="TransformSequence{T}"/> to which further transforms can be added.</returns>
        public static TransformSequence<TThis> TransformTo<TThis>(this TThis t, Transform transform) where TThis : class, ITransformable
        {
            var result = new TransformSequence<TThis>(t);
            result.Add(transform);
            t.AddTransform(transform);
            return result;
        }

        /// <summary>
        /// Creates a <see cref="Transform{TValue, T}"/> for smoothly changing <paramref name="propertyOrFieldName"/>
        /// over time using the given <paramref name="easing"/> for tweening.
        /// <see cref="PopulateTransform{TValue, DefaultEasingFunction, TThis}"/> is invoked as part of this method.
        /// </summary>
        /// <typeparam name="TThis">The type of the <see cref="ITransformable"/> the <see cref="Transform{TValue, T}"/> can be applied to.</typeparam>
        /// <typeparam name="TValue">The value type which is being transformed.</typeparam>
        /// <param name="t">The <see cref="ITransformable"/> the <see cref="Transform{TValue, T}"/> will be applied to.</param>
        /// <param name="propertyOrFieldName">The property or field name of the member ot <typeparamref name="TThis"/> to transform.</param>
        /// <param name="newValue">The value to transform to.</param>
        /// <param name="duration">The transform duration.</param>
        /// <param name="easing">The transform easing to be used for tweening.</param>
        /// <param name="grouping">An optional grouping specification to be used when the same property may be touched by multiple transform types.</param>
        /// <returns>The resulting <see cref="Transform{TValue, T}"/>.</returns>
        public static Transform<TValue, DefaultEasingFunction, TThis> MakeTransform<TThis, TValue>(this TThis t, string propertyOrFieldName, TValue newValue, double duration = 0,
                                                                                                   Easing easing = Easing.None, string grouping = null)
            where TThis : class, ITransformable
            => t.MakeTransform(propertyOrFieldName, newValue, duration, new DefaultEasingFunction(easing), grouping);

        /// <summary>
        /// Creates a <see cref="Transform{TValue, T}"/> for smoothly changing <paramref name="propertyOrFieldName"/>
        /// over time using the given <paramref name="easing"/> for tweening.
        /// <see cref="PopulateTransform{TValue, TEasing, TThis}"/> is invoked as part of this method.
        /// </summary>
        /// <typeparam name="TThis">The type of the <see cref="ITransformable"/> the <see cref="Transform{TValue, T}"/> can be applied to.</typeparam>
        /// <typeparam name="TValue">The value type which is being transformed.</typeparam>
        /// <typeparam name="TEasing">The type of easing.</typeparam>
        /// <param name="t">The <see cref="ITransformable"/> the <see cref="Transform{TValue, T}"/> will be applied to.</param>
        /// <param name="propertyOrFieldName">The property or field name of the member ot <typeparamref name="TThis"/> to transform.</param>
        /// <param name="newValue">The value to transform to.</param>
        /// <param name="duration">The transform duration.</param>
        /// <param name="easing">The transform easing to be used for tweening.</param>
        /// <param name="grouping">An optional grouping specification to be used when the same property may be touched by multiple transform types.</param>
        /// <returns>The resulting <see cref="Transform{TValue, T}"/>.</returns>
        public static Transform<TValue, TEasing, TThis> MakeTransform<TThis, TEasing, TValue>(this TThis t, string propertyOrFieldName, TValue newValue, double duration, in TEasing easing, string grouping = null)
            where TThis : class, ITransformable
            where TEasing : IEasingFunction
            => t.PopulateTransform(new TransformCustom<TValue, TEasing, TThis>(propertyOrFieldName, grouping), newValue, duration, easing);

        /// <summary>
        /// Populates a newly created <see cref="Transform{TValue, T}"/> with necessary values.
        /// All <see cref="Transform{TValue, T}"/>s must be populated by this method prior to being used.
        /// </summary>
        /// <typeparam name="TThis">The type of the <see cref="ITransformable"/> the <see cref="Transform{TValue, T}"/> can be applied to.</typeparam>
        /// <typeparam name="TValue">The value type which is being transformed.</typeparam>
        /// <param name="t">The <see cref="ITransformable"/> the <see cref="Transform{TValue, T}"/> will be applied to.</param>
        /// <param name="transform">The transform to populate.</param>
        /// <param name="newValue">The value to transform to.</param>
        /// <param name="duration">The transform duration.</param>
        /// <param name="easing">The transform easing to be used for tweening.</param>
        /// <returns>The populated <paramref name="transform"/>.</returns>
        public static Transform<TValue, DefaultEasingFunction, TThis> PopulateTransform<TValue, TThis>(this TThis t, Transform<TValue, DefaultEasingFunction, TThis> transform, TValue newValue,
                                                                                                       double duration = 0, Easing easing = Easing.None)
            where TThis : class, ITransformable
            => t.PopulateTransform(transform, newValue, duration, new DefaultEasingFunction(easing));

        /// <summary>
        /// Populates a newly created <see cref="Transform{TValue, T}"/> with necessary values.
        /// All <see cref="Transform{TValue, T}"/>s must be populated by this method prior to being used.
        /// </summary>
        /// <typeparam name="TThis">The type of the <see cref="ITransformable"/> the <see cref="Transform{TValue, T}"/> can be applied to.</typeparam>
        /// <typeparam name="TValue">The value type which is being transformed.</typeparam>
        /// <typeparam name="TEasing">The type of easing.</typeparam>
        /// <param name="t">The <see cref="ITransformable"/> the <see cref="Transform{TValue, T}"/> will be applied to.</param>
        /// <param name="transform">The transform to populate.</param>
        /// <param name="newValue">The value to transform to.</param>
        /// <param name="duration">The transform duration.</param>
        /// <param name="easing">The transform easing to be used for tweening.</param>
        /// <returns>The populated <paramref name="transform"/>.</returns>
        public static Transform<TValue, TEasing, TThis> PopulateTransform<TValue, TEasing, TThis>(this TThis t, Transform<TValue, TEasing, TThis> transform, TValue newValue, double duration,
                                                                                                  in TEasing easing)
            where TThis : class, ITransformable
            where TEasing : IEasingFunction
        {
            if (!isFinite(newValue))
                throw new ArgumentException($"{nameof(newValue)} must be finite, but is {newValue}.", nameof(newValue));

            if (duration < 0)
                throw new ArgumentOutOfRangeException(nameof(duration), $"{nameof(duration)} must be positive.");

            if (transform.Target != null)
                throw new InvalidOperationException($"May not {nameof(PopulateTransform)} the same {nameof(Transform<TValue, TThis>)} more than once.");

            transform.Target = t;

            double startTime = t.TransformStartTime;

            transform.StartTime = startTime;
            transform.EndTime = startTime + duration;
            transform.EndValue = newValue;
            transform.Easing = easing;

            return transform;

            static bool isFinite(TValue value)
            {
                if (typeof(TValue) == typeof(float))
                    return float.IsFinite((float)(object)value);
                if (typeof(TValue) == typeof(double))
                    return double.IsFinite((double)(object)value);
                if (typeof(TValue) == typeof(Vector2))
                    return Validation.IsFinite((Vector2)(object)value);
                if (typeof(TValue) == typeof(MarginPadding))
                    return Validation.IsFinite((MarginPadding)(object)value);

                return true;
            }
        }

        /// <summary>
        /// Applies <paramref name="childGenerators"/> via TransformSequence.Append(IEnumerable{Generator})/>.
        /// </summary>
        /// <typeparam name="T">The type of the <see cref="ITransformable"/> the <see cref="Transform{TValue, T}"/> can be applied to.</typeparam>
        /// <param name="transformable">The <see cref="ITransformable"/> the <see cref="Transform{TValue, T}"/> will be applied to.</param>
        /// <param name="childGenerators">The optional Generators for <see cref="TransformSequence{T}"/>s to be appended.</param>
        /// <returns>This <see cref="TransformSequence{T}"/>.</returns>
        public static TransformSequence<T> Animate<T>(this T transformable, params TransformSequence<T>.Generator[] childGenerators) where T : class, ITransformable =>
            transformable.Delay(0, childGenerators);

        /// <summary>
        /// Advances the start time of future appended <see cref="TransformSequence{T}"/>s by <paramref name="delay"/> milliseconds.
        /// Then, <paramref name="childGenerators"/> are appended via TransformSequence.Append(IEnumerable{Generator})/>.
        /// </summary>
        /// <typeparam name="T">The type of the <see cref="ITransformable"/> the <see cref="Transform{TValue, T}"/> can be applied to.</typeparam>
        /// <param name="transformable">The <see cref="ITransformable"/> the <see cref="Transform{TValue, T}"/> will be applied to.</param>
        /// <param name="delay">The delay to advance the start time by.</param>
        /// <param name="childGenerators">The optional Generators for <see cref="TransformSequence{T}"/>s to be appended.</param>
        /// <returns>This <see cref="TransformSequence{T}"/>.</returns>
        public static TransformSequence<T> Delay<T>(this T transformable, double delay, params TransformSequence<T>.Generator[] childGenerators) where T : class, ITransformable =>
            new TransformSequence<T>(transformable).Delay(delay, childGenerators);

        /// <summary>
        /// Returns a <see cref="TransformSequence{T}"/> which waits for all existing transforms to finish.
        /// </summary>
        /// <returns>A <see cref="TransformSequence{T}"/> which has a delay waiting for all transforms to be completed.</returns>
        public static TransformSequence<T> DelayUntilTransformsFinished<T>(this T transformable)
            where T : Transformable =>
            transformable.Delay(Math.Max(0, transformable.LatestTransformEndTime - transformable.Time.Current));

        /// <summary>
        /// Append a looping <see cref="TransformSequence{T}"/> to this <see cref="TransformSequence{T}"/>.
        /// All <see cref="Transform"/>s generated by <paramref name="childGenerators"/> are appended to
        /// this <see cref="TransformSequence{T}"/> and then repeated <paramref name="numIters"/> times
        /// with <paramref name="pause"/> milliseconds between iterations.
        /// </summary>
        /// <typeparam name="T">The type of the <see cref="ITransformable"/> the <see cref="Transform{TValue, T}"/> can be applied to.</typeparam>
        /// <param name="transformable">The <see cref="ITransformable"/> the <see cref="Transform{TValue, T}"/> will be applied to.</param>
        /// <param name="pause">The pause between iterations in milliseconds.</param>
        /// <param name="numIters">The number of iterations.</param>
        /// <param name="childGenerators">The functions to generate the <see cref="TransformSequence{T}"/>s to be looped.</param>
        /// <returns>This <see cref="TransformSequence{T}"/>.</returns>
        public static TransformSequence<T> Loop<T>(this T transformable, double pause, int numIters, params TransformSequence<T>.Generator[] childGenerators)
            where T : class, ITransformable =>
            transformable.Delay(0).Loop(pause, numIters, childGenerators);

        /// <summary>
        /// Append a looping <see cref="TransformSequence{T}"/> to this <see cref="TransformSequence{T}"/>.
        /// All <see cref="Transform"/>s generated by <paramref name="childGenerators"/> are appended to
        /// this <see cref="TransformSequence{T}"/> and then repeated indefinitely with <paramref name="pause"/>
        /// milliseconds between iterations.
        /// </summary>
        /// <typeparam name="T">The type of the <see cref="ITransformable"/> the <see cref="Transform{TValue, T}"/> can be applied to.</typeparam>
        /// <param name="transformable">The <see cref="ITransformable"/> the <see cref="Transform{TValue, T}"/> will be applied to.</param>
        /// <param name="pause">The pause between iterations in milliseconds.</param>
        /// <param name="childGenerators">The functions to generate the <see cref="TransformSequence{T}"/>s to be looped.</param>
        /// <returns>This <see cref="TransformSequence{T}"/>.</returns>
        public static TransformSequence<T> Loop<T>(this T transformable, double pause, params TransformSequence<T>.Generator[] childGenerators)
            where T : class, ITransformable =>
            transformable.Delay(0).Loop(pause, childGenerators);

        /// <summary>
        /// Append a looping <see cref="TransformSequence{T}"/> to this <see cref="TransformSequence{T}"/>.
        /// All <see cref="Transform"/>s generated by <paramref name="childGenerators"/> are appended to
        /// this <see cref="TransformSequence{T}"/> and then repeated indefinitely.
        /// milliseconds between iterations.
        /// </summary>
        /// <typeparam name="T">The type of the <see cref="ITransformable"/> the <see cref="Transform{TValue, T}"/> can be applied to.</typeparam>
        /// <param name="transformable">The <see cref="ITransformable"/> the <see cref="Transform{TValue, T}"/> will be applied to.</param>
        /// <param name="childGenerators">The functions to generate the <see cref="TransformSequence{T}"/>s to be looped.</param>
        /// <returns>This <see cref="TransformSequence{T}"/>.</returns>
        public static TransformSequence<T> Loop<T>(this T transformable, params TransformSequence<T>.Generator[] childGenerators)
            where T : class, ITransformable =>
            transformable.Delay(0).Loop(childGenerators);

        /// <summary>
        /// Append a looping <see cref="TransformSequence{T}"/> to this <see cref="TransformSequence{T}"/> to repeat indefinitely with <paramref name="pause"/>
        /// milliseconds between iterations.
        /// </summary>
        /// <typeparam name="T">The type of the <see cref="ITransformable"/> the <see cref="Transform{TValue, T}"/> can be applied to.</typeparam>
        /// <param name="transformable">The <see cref="ITransformable"/> the <see cref="Transform{TValue, T}"/> will be applied to.</param>
        /// <param name="pause">The pause between iterations in milliseconds.</param>
        /// <returns>This <see cref="TransformSequence{T}"/>.</returns>
        public static TransformSequence<T> Loop<T>(this T transformable, double pause = 0)
            where T : class, ITransformable =>
            transformable.Delay(0).Loop(pause);

        /// <summary>
        /// Rotate over one full rotation with provided parameters.
        /// </summary>
        /// <returns>A <see cref="TransformSequence{T}"/> to which further transforms can be added.</returns>
        public static TransformSequence<T> Spin<T>(this T drawable, double revolutionDuration, RotationDirection direction, float startRotation = 0)
            where T : Drawable
            => drawable.Delay(0).Spin(revolutionDuration, direction, startRotation);

        /// <summary>
        /// Rotate <paramref name="numRevolutions"/> times with provided parameters.
        /// </summary>
        /// <returns>A <see cref="TransformSequence{T}"/> to which further transforms can be added.</returns>
        public static TransformSequence<T> Spin<T>(this T drawable, double revolutionDuration, RotationDirection direction, float startRotation, int numRevolutions)
            where T : Drawable
            => drawable.Delay(0).Spin(revolutionDuration, direction, startRotation, numRevolutions);

        #region Easing

        /// <summary>
        /// Smoothly adjusts <see cref="Drawable.Alpha"/> to 1 over time.
        /// </summary>
        /// <returns>A <see cref="TransformSequence{T}"/> to which further transforms can be added.</returns>
        public static TransformSequence<T> FadeIn<T>(this T drawable, double duration = 0, Easing easing = Easing.None)
            where T : Drawable
            => drawable.FadeIn(duration, new DefaultEasingFunction(easing));

        /// <summary>
        /// Smoothly adjusts <see cref="Drawable.Alpha"/> from 0 to 1 over time.
        /// </summary>
        /// <returns>A <see cref="TransformSequence{T}"/> to which further transforms can be added.</returns>
        public static TransformSequence<T> FadeInFromZero<T>(this T drawable, double duration = 0, Easing easing = Easing.None)
            where T : Drawable
            => drawable.FadeInFromZero(duration, new DefaultEasingFunction(easing));

        /// <summary>
        /// Smoothly adjusts <see cref="Drawable.Alpha"/> to 0 over time.
        /// </summary>
        /// <returns>A <see cref="TransformSequence{T}"/> to which further transforms can be added.</returns>
        public static TransformSequence<T> FadeOut<T>(this T drawable, double duration = 0, Easing easing = Easing.None)
            where T : Drawable
            => drawable.FadeOut(duration, new DefaultEasingFunction(easing));

        /// <summary>
        /// Smoothly adjusts <see cref="Drawable.Alpha"/> from 1 to 0 over time.
        /// </summary>
        /// <returns>A <see cref="TransformSequence{T}"/> to which further transforms can be added.</returns>
        public static TransformSequence<T> FadeOutFromOne<T>(this T drawable, double duration = 0, Easing easing = Easing.None)
            where T : Drawable
            => drawable.FadeOutFromOne(duration, new DefaultEasingFunction(easing));

        /// <summary>
        /// Smoothly adjusts <see cref="Drawable.Alpha"/> over time.
        /// </summary>
        /// <returns>A <see cref="TransformSequence{T}"/> to which further transforms can be added.</returns>
        public static TransformSequence<T> FadeTo<T>(this T drawable, float newAlpha, double duration = 0, Easing easing = Easing.None)
            where T : Drawable
            => drawable.FadeTo(newAlpha, duration, new DefaultEasingFunction(easing));

        /// <summary>
        /// Smoothly adjusts <see cref="Drawable.Colour"/> over time.
        /// </summary>
        /// <returns>A <see cref="TransformSequence{T}"/> to which further transforms can be added.</returns>
        public static TransformSequence<T> FadeColour<T>(this T drawable, ColourInfo newColour, double duration = 0, Easing easing = Easing.None)
            where T : Drawable
            => drawable.FadeColour(newColour, duration, new DefaultEasingFunction(easing));

        /// <summary>
        /// Instantaneously flashes <see cref="Drawable.Colour"/>, then smoothly changes it back over time.
        /// </summary>
        /// <returns>A <see cref="TransformSequence{T}"/> to which further transforms can be added.</returns>
        public static TransformSequence<T> FlashColour<T>(this T drawable, ColourInfo flashColour, double duration, Easing easing = Easing.None)
            where T : Drawable
            => drawable.FlashColour(flashColour, duration, new DefaultEasingFunction(easing));

        /// <summary>
        /// Smoothly adjusts <see cref="Drawable.Rotation"/> over time.
        /// </summary>
        /// <returns>A <see cref="TransformSequence{T}"/> to which further transforms can be added.</returns>
        public static TransformSequence<T> RotateTo<T>(this T drawable, float newRotation, double duration = 0, Easing easing = Easing.None)
            where T : Drawable
            => drawable.RotateTo(newRotation, duration, new DefaultEasingFunction(easing));

        /// <summary>
        /// Smoothly adjusts <see cref="Drawable.Scale"/> over time.
        /// </summary>
        /// <returns>A <see cref="TransformSequence{T}"/> to which further transforms can be added.</returns>
        public static TransformSequence<T> ScaleTo<T>(this T drawable, float newScale, double duration = 0, Easing easing = Easing.None)
            where T : Drawable
            => drawable.ScaleTo(newScale, duration, new DefaultEasingFunction(easing));

        /// <summary>
        /// Smoothly adjusts <see cref="Drawable.Scale"/> over time.
        /// </summary>
        /// <returns>A <see cref="TransformSequence{T}"/> to which further transforms can be added.</returns>
        public static TransformSequence<T> ScaleTo<T>(this T drawable, Vector2 newScale, double duration = 0, Easing easing = Easing.None)
            where T : Drawable
            => drawable.ScaleTo(newScale, duration, new DefaultEasingFunction(easing));

        /// <summary>
        /// Smoothly adjusts <see cref="Drawable.Size"/> over time.
        /// </summary>
        /// <returns>A <see cref="TransformSequence{T}"/> to which further transforms can be added.</returns>
        public static TransformSequence<T> ResizeTo<T>(this T drawable, float newSize, double duration = 0, Easing easing = Easing.None)
            where T : Drawable
            => drawable.ResizeTo(newSize, duration, new DefaultEasingFunction(easing));

        /// <summary>
        /// Smoothly adjusts <see cref="Drawable.Size"/> over time.
        /// </summary>
        /// <returns>A <see cref="TransformSequence{T}"/> to which further transforms can be added.</returns>
        public static TransformSequence<T> ResizeTo<T>(this T drawable, Vector2 newSize, double duration = 0, Easing easing = Easing.None)
            where T : Drawable
            => drawable.ResizeTo(newSize, duration, new DefaultEasingFunction(easing));

        /// <summary>
        /// Smoothly adjusts <see cref="Drawable.Width"/> over time.
        /// </summary>
        /// <returns>A <see cref="TransformSequence{T}"/> to which further transforms can be added.</returns>
        public static TransformSequence<T> ResizeWidthTo<T>(this T drawable, float newWidth, double duration = 0, Easing easing = Easing.None)
            where T : Drawable
            => drawable.ResizeWidthTo(newWidth, duration, new DefaultEasingFunction(easing));

        /// <summary>
        /// Smoothly adjusts <see cref="Drawable.Height"/> over time.
        /// </summary>
        /// <returns>A <see cref="TransformSequence{T}"/> to which further transforms can be added.</returns>
        public static TransformSequence<T> ResizeHeightTo<T>(this T drawable, float newHeight, double duration = 0, Easing easing = Easing.None)
            where T : Drawable
            => drawable.ResizeHeightTo(newHeight, duration, new DefaultEasingFunction(easing));

        /// <summary>
        /// Smoothly adjusts <see cref="Drawable.Position"/> over time.
        /// </summary>
        /// <returns>A <see cref="TransformSequence{T}"/> to which further transforms can be added.</returns>
        public static TransformSequence<T> MoveTo<T>(this T drawable, Vector2 newPosition, double duration = 0, Easing easing = Easing.None)
            where T : Drawable
            => drawable.MoveTo(newPosition, duration, new DefaultEasingFunction(easing));

        /// <summary>
        /// Smoothly adjusts <see cref="Drawable.X"/> or <see cref="Drawable.Y"/> over time.
        /// </summary>
        /// <returns>A <see cref="TransformSequence{T}"/> to which further transforms can be added.</returns>
        public static TransformSequence<T> MoveTo<T>(this T drawable, Direction direction, float destination, double duration = 0, Easing easing = Easing.None)
            where T : Drawable
            => drawable.MoveTo(direction, destination, duration, new DefaultEasingFunction(easing));

        /// <summary>
        /// Smoothly adjusts <see cref="Drawable.X"/> over time.
        /// </summary>
        /// <returns>A <see cref="TransformSequence{T}"/> to which further transforms can be added.</returns>
        public static TransformSequence<T> MoveToX<T>(this T drawable, float destination, double duration = 0, Easing easing = Easing.None)
            where T : Drawable
            => drawable.MoveToX(destination, duration, new DefaultEasingFunction(easing));

        /// <summary>
        /// Smoothly adjusts <see cref="Drawable.Y"/> over time.
        /// </summary>
        /// <returns>A <see cref="TransformSequence{T}"/> to which further transforms can be added.</returns>
        public static TransformSequence<T> MoveToY<T>(this T drawable, float destination, double duration = 0, Easing easing = Easing.None)
            where T : Drawable
            => drawable.MoveToY(destination, duration, new DefaultEasingFunction(easing));

        /// <summary>
        /// Smoothly adjusts <see cref="Drawable.Position"/> by an offset to its final value over time.
        /// </summary>
        /// <returns>A <see cref="TransformSequence{T}"/> to which further transforms can be added.</returns>
        public static TransformSequence<T> MoveToOffset<T>(this T drawable, Vector2 offset, double duration = 0, Easing easing = Easing.None)
            where T : Drawable
            => drawable.MoveToOffset(offset, duration, new DefaultEasingFunction(easing));

        /// <summary>
        /// Smoothly adjusts <see cref="IContainer.RelativeChildSize"/> over time.
        /// </summary>
        /// <returns>A <see cref="TransformSequence{T}"/> to which further transforms can be added.</returns>
        public static TransformSequence<T> TransformRelativeChildSizeTo<T>(this T container, Vector2 newSize, double duration = 0, Easing easing = Easing.None)
            where T : class, IContainer
            => container.TransformRelativeChildSizeTo(newSize, duration, new DefaultEasingFunction(easing));

        /// <summary>
        /// Smoothly adjusts <see cref="IContainer.RelativeChildOffset"/> over time.
        /// </summary>
        /// <returns>A <see cref="TransformSequence{T}"/> to which further transforms can be added.</returns>
        public static TransformSequence<T> TransformRelativeChildOffsetTo<T>(this T container, Vector2 newOffset, double duration = 0, Easing easing = Easing.None)
            where T : class, IContainer
            => container.TransformRelativeChildOffsetTo(newOffset, duration, new DefaultEasingFunction(easing));

        /// <summary>
        /// Smoothly adjusts <see cref="IBufferedContainer.BlurSigma"/> over time.
        /// </summary>
        /// <returns>A <see cref="TransformSequence{T}"/> to which further transforms can be added.</returns>
        public static TransformSequence<T> BlurTo<T>(this T bufferedContainer, Vector2 newBlurSigma, double duration = 0, Easing easing = Easing.None)
            where T : class, IBufferedContainer
            => bufferedContainer.BlurTo(newBlurSigma, duration, new DefaultEasingFunction(easing));

        /// <summary>
        /// Smoothly adjusts <see cref="IFillFlowContainer.Spacing"/> over time.
        /// </summary>
        /// <returns>A <see cref="TransformSequence{T}"/> to which further transforms can be added.</returns>
        public static TransformSequence<T> TransformSpacingTo<T>(this T flowContainer, Vector2 newSpacing, double duration = 0, Easing easing = Easing.None)
            where T : class, IFillFlowContainer
            => flowContainer.TransformSpacingTo(newSpacing, duration, new DefaultEasingFunction(easing));

        /// <summary>
        /// Smoothly adjusts the alpha channel of the colour of <see cref="IContainer.EdgeEffect"/> over time.
        /// </summary>
        /// <returns>A <see cref="TransformSequence{T}"/> to which further transforms can be added.</returns>
        public static TransformSequence<T> FadeEdgeEffectTo<T>(this T container, float newAlpha, double duration = 0, Easing easing = Easing.None)
            where T : class, IContainer
            => container.FadeEdgeEffectTo(newAlpha, duration, new DefaultEasingFunction(easing));

        /// <summary>
        /// Smoothly adjusts the colour of <see cref="IContainer.EdgeEffect"/> over time.
        /// </summary>
        /// <returns>A <see cref="TransformSequence{T}"/> to which further transforms can be added.</returns>
        public static TransformSequence<T> FadeEdgeEffectTo<T>(this T container, Color4 newColour, double duration = 0, Easing easing = Easing.None)
            where T : class, IContainer
            => container.FadeEdgeEffectTo(newColour, duration, new DefaultEasingFunction(easing));

        /// <summary>
        /// Smoothly adjusts all parameters of <see cref="IContainer.EdgeEffect"/> over time.
        /// </summary>
        /// <returns>A <see cref="TransformSequence{T}"/> to which further transforms can be added.</returns>
        public static TransformSequence<T> TweenEdgeEffectTo<T>(this T container, EdgeEffectParameters newParameters, double duration = 0, Easing easing = Easing.None)
            where T : class, IContainer
            => container.TweenEdgeEffectTo(newParameters, duration, new DefaultEasingFunction(easing));

        /// <summary>
        /// Smoothly adjusts the value of a <see cref="Bindable{TValue}"/> over time.
        /// </summary>
        /// <returns>A <see cref="TransformSequence{T}"/> to which further transforms can be added.</returns>
        public static TransformSequence<T> TransformBindableTo<T, TValue>(this T drawable, [NotNull] Bindable<TValue> bindable, TValue newValue, double duration = 0, Easing easing = Easing.None)
            where T : class, ITransformable
            => drawable.TransformBindableTo(bindable, newValue, duration, new DefaultEasingFunction(easing));

        #endregion

        #region Generic Easing

        /// <summary>
        /// Smoothly adjusts <see cref="Drawable.Alpha"/> to 1 over time.
        /// </summary>
        /// <returns>A <see cref="TransformSequence{T}"/> to which further transforms can be added.</returns>
        public static TransformSequence<T> FadeIn<T, TEasing>(this T drawable, double duration, in TEasing easing)
            where T : Drawable
            where TEasing : IEasingFunction
            => drawable.FadeTo(1, duration, easing);

        /// <summary>
        /// Smoothly adjusts <see cref="Drawable.Alpha"/> from 0 to 1 over time.
        /// </summary>
        /// <returns>A <see cref="TransformSequence{T}"/> to which further transforms can be added.</returns>
        public static TransformSequence<T> FadeInFromZero<T, TEasing>(this T drawable, double duration, in TEasing easing)
            where T : Drawable
            where TEasing : IEasingFunction
            => drawable.FadeTo(0).FadeIn(duration, easing);

        /// <summary>
        /// Smoothly adjusts <see cref="Drawable.Alpha"/> to 0 over time.
        /// </summary>
        /// <returns>A <see cref="TransformSequence{T}"/> to which further transforms can be added.</returns>
        public static TransformSequence<T> FadeOut<T, TEasing>(this T drawable, double duration, in TEasing easing)
            where T : Drawable
            where TEasing : IEasingFunction
            => drawable.FadeTo(0, duration, easing);

        /// <summary>
        /// Smoothly adjusts <see cref="Drawable.Alpha"/> from 1 to 0 over time.
        /// </summary>
        /// <returns>A <see cref="TransformSequence{T}"/> to which further transforms can be added.</returns>
        public static TransformSequence<T> FadeOutFromOne<T, TEasing>(this T drawable, double duration, in TEasing easing)
            where T : Drawable
            where TEasing : IEasingFunction
            => drawable.FadeTo(1).FadeOut(duration, easing);

        /// <summary>
        /// Smoothly adjusts <see cref="Drawable.Alpha"/> over time.
        /// </summary>
        /// <returns>A <see cref="TransformSequence{T}"/> to which further transforms can be added.</returns>
        public static TransformSequence<T> FadeTo<T, TEasing>(this T drawable, float newAlpha, double duration, in TEasing easing)
            where T : Drawable
            where TEasing : IEasingFunction
            => drawable.TransformTo(nameof(drawable.Alpha), newAlpha, duration, easing);

        /// <summary>
        /// Smoothly adjusts <see cref="Drawable.Colour"/> over time.
        /// </summary>
        /// <returns>A <see cref="TransformSequence{T}"/> to which further transforms can be added.</returns>
        public static TransformSequence<T> FadeColour<T, TEasing>(this T drawable, ColourInfo newColour, double duration, in TEasing easing)
            where T : Drawable
            where TEasing : IEasingFunction
            => drawable.TransformTo(nameof(drawable.Colour), newColour, duration, easing);

        /// <summary>
        /// Instantaneously flashes <see cref="Drawable.Colour"/>, then smoothly changes it back over time.
        /// </summary>
        /// <returns>A <see cref="TransformSequence{T}"/> to which further transforms can be added.</returns>
        public static TransformSequence<T> FlashColour<T, TEasing>(this T drawable, ColourInfo flashColour, double duration, in TEasing easing)
            where T : Drawable
            where TEasing : IEasingFunction
        {
            ColourInfo endValue = (drawable.Transforms.LastOrDefault(t => t.TargetMember == nameof(drawable.Colour)) as Transform<ColourInfo>)?.EndValue ?? drawable.Colour;
            return drawable.FadeColour(flashColour).FadeColour(endValue, duration, easing);
        }

        /// <summary>
        /// Smoothly adjusts <see cref="Drawable.Rotation"/> over time.
        /// </summary>
        /// <returns>A <see cref="TransformSequence{T}"/> to which further transforms can be added.</returns>
        public static TransformSequence<T> RotateTo<T, TEasing>(this T drawable, float newRotation, double duration, in TEasing easing)
            where T : Drawable
            where TEasing : IEasingFunction
            => drawable.TransformTo(nameof(drawable.Rotation), newRotation, duration, easing);

        /// <summary>
        /// Smoothly adjusts <see cref="Drawable.Scale"/> over time.
        /// </summary>
        /// <returns>A <see cref="TransformSequence{T}"/> to which further transforms can be added.</returns>
        public static TransformSequence<T> ScaleTo<T, TEasing>(this T drawable, float newScale, double duration, in TEasing easing)
            where T : Drawable
            where TEasing : IEasingFunction
            => drawable.ScaleTo(new Vector2(newScale), duration, easing);

        /// <summary>
        /// Smoothly adjusts <see cref="Drawable.Scale"/> over time.
        /// </summary>
        /// <returns>A <see cref="TransformSequence{T}"/> to which further transforms can be added.</returns>
        public static TransformSequence<T> ScaleTo<T, TEasing>(this T drawable, Vector2 newScale, double duration, in TEasing easing)
            where T : Drawable
            where TEasing : IEasingFunction
            => drawable.TransformTo(nameof(drawable.Scale), newScale, duration, easing);

        /// <summary>
        /// Smoothly adjusts <see cref="Drawable.Size"/> over time.
        /// </summary>
        /// <returns>A <see cref="TransformSequence{T}"/> to which further transforms can be added.</returns>
        public static TransformSequence<T> ResizeTo<T, TEasing>(this T drawable, float newSize, double duration, in TEasing easing)
            where T : Drawable
            where TEasing : IEasingFunction
            => drawable.ResizeTo(new Vector2(newSize), duration, easing);

        /// <summary>
        /// Smoothly adjusts <see cref="Drawable.Size"/> over time.
        /// </summary>
        /// <returns>A <see cref="TransformSequence{T}"/> to which further transforms can be added.</returns>
        public static TransformSequence<T> ResizeTo<T, TEasing>(this T drawable, Vector2 newSize, double duration, in TEasing easing)
            where T : Drawable
            where TEasing : IEasingFunction
            => drawable.TransformTo(nameof(drawable.Size), newSize, duration, easing);

        /// <summary>
        /// Smoothly adjusts <see cref="Drawable.Width"/> over time.
        /// </summary>
        /// <returns>A <see cref="TransformSequence{T}"/> to which further transforms can be added.</returns>
        public static TransformSequence<T> ResizeWidthTo<T, TEasing>(this T drawable, float newWidth, double duration, in TEasing easing)
            where T : Drawable
            where TEasing : IEasingFunction
            => drawable.TransformTo(nameof(drawable.Width), newWidth, duration, easing, nameof(drawable.Size));

        /// <summary>
        /// Smoothly adjusts <see cref="Drawable.Height"/> over time.
        /// </summary>
        /// <returns>A <see cref="TransformSequence{T}"/> to which further transforms can be added.</returns>
        public static TransformSequence<T> ResizeHeightTo<T, TEasing>(this T drawable, float newHeight, double duration, in TEasing easing)
            where T : Drawable
            where TEasing : IEasingFunction
            => drawable.TransformTo(nameof(drawable.Height), newHeight, duration, easing, nameof(drawable.Size));

        /// <summary>
        /// Smoothly adjusts <see cref="Drawable.Position"/> over time.
        /// </summary>
        /// <returns>A <see cref="TransformSequence{T}"/> to which further transforms can be added.</returns>
        public static TransformSequence<T> MoveTo<T, TEasing>(this T drawable, Vector2 newPosition, double duration, in TEasing easing)
            where T : Drawable
            where TEasing : IEasingFunction
            => drawable.TransformTo(nameof(drawable.Position), newPosition, duration, easing);

        /// <summary>
        /// Smoothly adjusts <see cref="Drawable.X"/> or <see cref="Drawable.Y"/> over time.
        /// </summary>
        /// <returns>A <see cref="TransformSequence{T}"/> to which further transforms can be added.</returns>
        public static TransformSequence<T> MoveTo<T, TEasing>(this T drawable, Direction direction, float destination, double duration, in TEasing easing)
            where T : Drawable
            where TEasing : IEasingFunction
        {
            switch (direction)
            {
                case Direction.Horizontal:
                    return drawable.MoveToX(destination, duration, easing);

                case Direction.Vertical:
                    return drawable.MoveToY(destination, duration, easing);
            }

            throw new InvalidOperationException($"Invalid direction ({direction}) passed to {nameof(MoveTo)}.");
        }

        /// <summary>
        /// Smoothly adjusts <see cref="Drawable.X"/> over time.
        /// </summary>
        /// <returns>A <see cref="TransformSequence{T}"/> to which further transforms can be added.</returns>
        public static TransformSequence<T> MoveToX<T, TEasing>(this T drawable, float destination, double duration, in TEasing easing)
            where T : Drawable
            where TEasing : IEasingFunction
            => drawable.TransformTo(nameof(drawable.X), destination, duration, easing, nameof(drawable.Position));

        /// <summary>
        /// Smoothly adjusts <see cref="Drawable.Y"/> over time.
        /// </summary>
        /// <returns>A <see cref="TransformSequence{T}"/> to which further transforms can be added.</returns>
        public static TransformSequence<T> MoveToY<T, TEasing>(this T drawable, float destination, double duration, in TEasing easing)
            where T : Drawable
            where TEasing : IEasingFunction
            => drawable.TransformTo(nameof(drawable.Y), destination, duration, easing, nameof(drawable.Position));

        /// <summary>
        /// Smoothly adjusts <see cref="Drawable.Position"/> by an offset to its final value over time.
        /// </summary>
        /// <returns>A <see cref="TransformSequence{T}"/> to which further transforms can be added.</returns>
        public static TransformSequence<T> MoveToOffset<T, TEasing>(this T drawable, Vector2 offset, double duration, in TEasing easing)
            where T : Drawable
            where TEasing : IEasingFunction
            => drawable.TransformTo(drawable.PopulateTransform(new PositionOffsetTransform<TEasing>(offset), default, duration, easing));

        /// <summary>
        /// Smoothly adjusts <see cref="IContainer.RelativeChildSize"/> over time.
        /// </summary>
        /// <returns>A <see cref="TransformSequence{T}"/> to which further transforms can be added.</returns>
        public static TransformSequence<T> TransformRelativeChildSizeTo<T, TEasing>(this T container, Vector2 newSize, double duration, in TEasing easing)
            where T : class, IContainer
            where TEasing : IEasingFunction
        {
            if (newSize.X == 0 || newSize.Y == 0)
                throw new ArgumentException($@"{nameof(newSize)} must be non-zero, but is {newSize}.", nameof(newSize));

            return container.TransformTo(nameof(container.RelativeChildSize), newSize, duration, easing);
        }

        /// <summary>
        /// Smoothly adjusts <see cref="IContainer.RelativeChildOffset"/> over time.
        /// </summary>
        /// <returns>A <see cref="TransformSequence{T}"/> to which further transforms can be added.</returns>
        public static TransformSequence<T> TransformRelativeChildOffsetTo<T, TEasing>(this T container, Vector2 newOffset, double duration, in TEasing easing)
            where T : class, IContainer
            where TEasing : IEasingFunction
            => container.TransformTo(nameof(container.RelativeChildOffset), newOffset, duration, easing);

        /// <summary>
        /// Smoothly adjusts <see cref="IBufferedContainer.BlurSigma"/> over time.
        /// </summary>
        /// <returns>A <see cref="TransformSequence{T}"/> to which further transforms can be added.</returns>
        public static TransformSequence<T> BlurTo<T, TEasing>(this T bufferedContainer, Vector2 newBlurSigma, double duration, in TEasing easing)
            where T : class, IBufferedContainer
            where TEasing : IEasingFunction
            => bufferedContainer.TransformTo(nameof(bufferedContainer.BlurSigma), newBlurSigma, duration, easing);

        /// <summary>
        /// Smoothly adjusts <see cref="IFillFlowContainer.Spacing"/> over time.
        /// </summary>
        /// <returns>A <see cref="TransformSequence{T}"/> to which further transforms can be added.</returns>
        public static TransformSequence<T> TransformSpacingTo<T, TEasing>(this T flowContainer, Vector2 newSpacing, double duration, in TEasing easing)
            where T : class, IFillFlowContainer
            where TEasing : IEasingFunction
            => flowContainer.TransformTo(nameof(flowContainer.Spacing), newSpacing, duration, easing);

        /// <summary>
        /// Smoothly adjusts the alpha channel of the colour of <see cref="IContainer.EdgeEffect"/> over time.
        /// </summary>
        /// <returns>A <see cref="TransformSequence{T}"/> to which further transforms can be added.</returns>
        public static TransformSequence<T> FadeEdgeEffectTo<T, TEasing>(this T container, float newAlpha, double duration, in TEasing easing)
            where T : class, IContainer
            where TEasing : IEasingFunction
        {
            Color4 targetColour = container.EdgeEffect.Colour;
            targetColour.A = newAlpha;
            return container.FadeEdgeEffectTo(targetColour, duration, easing);
        }

        /// <summary>
        /// Smoothly adjusts the colour of <see cref="IContainer.EdgeEffect"/> over time.
        /// </summary>
        /// <returns>A <see cref="TransformSequence{T}"/> to which further transforms can be added.</returns>
        public static TransformSequence<T> FadeEdgeEffectTo<T, TEasing>(this T container, Color4 newColour, double duration, in TEasing easing)
            where T : class, IContainer
            where TEasing : IEasingFunction
        {
            var effect = container.EdgeEffect;
            effect.Colour = newColour;
            return container.TweenEdgeEffectTo(effect, duration, easing);
        }

        /// <summary>
        /// Smoothly adjusts all parameters of <see cref="IContainer.EdgeEffect"/> over time.
        /// </summary>
        /// <returns>A <see cref="TransformSequence{T}"/> to which further transforms can be added.</returns>
        public static TransformSequence<T> TweenEdgeEffectTo<T, TEasing>(this T container, EdgeEffectParameters newParameters, double duration, in TEasing easing)
            where T : class, IContainer
            where TEasing : IEasingFunction
            => container.TransformTo(nameof(container.EdgeEffect), newParameters, duration, easing);

        /// <summary>
        /// Smoothly adjusts the value of a <see cref="Bindable{TValue}"/> over time.
        /// </summary>
        /// <returns>A <see cref="TransformSequence{T}"/> to which further transforms can be added.</returns>
        public static TransformSequence<T> TransformBindableTo<T, TValue, TEasing>(this T drawable, [NotNull] Bindable<TValue> bindable, TValue newValue, double duration, in TEasing easing)
            where T : class, ITransformable
            where TEasing : IEasingFunction
            => drawable.TransformTo(drawable.PopulateTransform(new TransformBindable<TValue, TEasing, T>(bindable), newValue, duration, easing));

        #endregion

        private class PositionOffsetTransform<TEasing> : Transform<Vector2, TEasing, Drawable>
            where TEasing : IEasingFunction
        {
            private readonly Vector2 offset;

            public override string TargetMember => nameof(Drawable.Position);

            public PositionOffsetTransform(Vector2 offset)
            {
                this.offset = offset;
            }

            private Vector2 positionAt(double time)
            {
                if (time < StartTime) return StartValue;
                if (time >= EndTime) return EndValue;

                return Interpolation.ValueAt(time, StartValue, EndValue, StartTime, EndTime, Easing);
            }

            protected override void Apply(Drawable d, double time) => d.Position = positionAt(time);

            protected override void ReadIntoStartValue(Drawable d)
            {
                StartValue = d.Position;
                EndValue = d.Position + offset;
            }
        }
    }
}
