// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using OpenTK;
using OpenTK.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Transforms;
using System;
using System.Linq;

namespace osu.Framework.Graphics
{
    public static class TransformableExtensions
    {
        /// <summary>
        /// Applies a <see cref="Transform{TValue, T}"/> to a given <see cref="ITransformable"/>.
        /// </summary>
        /// <typeparam name="TThis">The type of the <see cref="ITransformable"/> to apply the <see cref="Transform{TValue, T}"/> to.</typeparam>
        /// <typeparam name="TValue">The value type which is being transformed.</typeparam>
        /// <typeparam name="TBase">The minimum type required for the given <see cref="Transform{TValue, T}"/>.</typeparam>
        /// <param name="t">The <see cref="ITransformable"/> to apply the <see cref="Transform{TValue, T}"/> to.</param>
        /// <param name="newValue">The value to transform to.</param>
        /// <param name="duration">The transform duration.</param>
        /// <param name="easing">The transform easing.</param>
        /// <param name="transform">The transform to use.</param>
        /// <returns>A <see cref="TransformSequence{T}"/> to which further transforms can be added.</returns>
        public static TransformSequence<TThis> TransformTo<TThis>(this TThis t, Transform transform) where TThis : ITransformable
        {
            var result = new TransformSequence<TThis>(t, transform);
            t.AddTransform(transform);
            return result;
        }

        public static Transform<TValue, TBase> MakeTransform<TThis, TValue, TBase>(
            this TThis t, TValue newValue, double duration, EasingTypes easing, Transform<TValue, TBase> transform)
            where TThis : ITransformable, TBase
        {
            transform.Target = t;

            double startTime = t.TransformStartTime;

            transform.StartTime = startTime;
            transform.EndTime = startTime + duration;
            transform.EndValue = newValue;
            transform.Easing = easing;

            return transform;
        }

        public static TransformSequence<T> Delayed<T>(this T transformable, double delay) where T : ITransformable =>
            new TransformSequence<T>(transformable, true, delay);

        public static TransformSequence<T> Loop<T>(this T transformable, double pause, int numIters, Func<T, TransformSequence<T>> firstChildGenerator, params Func<T, TransformSequence<T>>[] childGenerators)
            where T : ITransformable =>
            transformable.Delayed(0).Loop(pause, numIters, firstChildGenerator, childGenerators);

        public static TransformSequence<T> Loop<T>(this T transformable, double pause, Func<T, TransformSequence<T>> firstChildGenerator, params Func<T, TransformSequence<T>>[] childGenerators)
            where T : ITransformable =>
            transformable.Loop(pause, -1, firstChildGenerator, childGenerators);

        public static TransformSequence<T> Loop<T>(this T transformable, Func<T, TransformSequence<T>> firstChildGenerator, params Func<T, TransformSequence<T>>[] childGenerators)
            where T : ITransformable =>
            transformable.Loop(0, -1, firstChildGenerator, childGenerators);

        public static TransformSequence<T> FadeIn<T>(this T drawable, double duration = 0, EasingTypes easing = EasingTypes.None) where T : Drawable =>
            drawable.FadeTo(1, duration, easing);

        public static TransformSequence<T> FadeInFromZero<T>(this T drawable, double duration = 0, EasingTypes easing = EasingTypes.None) where T : Drawable
        {
            drawable.FadeTo(0);
            return drawable.FadeIn(duration, easing);
        }

        public static TransformSequence<T> FadeOut<T>(this T drawable, double duration = 0, EasingTypes easing = EasingTypes.None) where T : Drawable =>
            drawable.FadeTo(0, duration, easing);

        public static TransformSequence<T> FadeOutFromOne<T>(this T drawable, double duration = 0, EasingTypes easing = EasingTypes.None) where T : Drawable
        {
            drawable.FadeTo(1);
            return drawable.FadeOut(duration, easing);
        }

        public static TransformSequence<T> FadeTo<T>(this T drawable, float newAlpha, double duration = 0, EasingTypes easing = EasingTypes.None) where T : Drawable =>
            drawable.TransformTo(drawable.MakeTransform(newAlpha, duration, easing, new TransformAlpha()));

        public static TransformSequence<T> RotateTo<T>(this T drawable, float newRotation, double duration = 0, EasingTypes easing = EasingTypes.None) where T : Drawable =>
            drawable.TransformTo(drawable.MakeTransform(newRotation, duration, easing, new TransformRotation()));

        public static TransformSequence<T> Spin<T>(this T drawable, double revolutionDuration, float startRotation = 0, int numRevolutions = -1) where T : Drawable =>
            drawable.Delayed(0).Spin(revolutionDuration, startRotation, numRevolutions);

        public static TransformSequence<T> MoveTo<T>(this T drawable, Direction direction, float destination, double duration = 0, EasingTypes easing = EasingTypes.None) where T : Drawable
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

        public static TransformSequence<T> MoveToX<T>(this T drawable, float destination, double duration = 0, EasingTypes easing = EasingTypes.None) where T : Drawable =>
            drawable.TransformTo(drawable.MakeTransform(destination, duration, easing, new TransformPositionX()));

        public static TransformSequence<T> MoveToY<T>(this T drawable, float destination, double duration = 0, EasingTypes easing = EasingTypes.None) where T : Drawable =>
            drawable.TransformTo(drawable.MakeTransform(destination, duration, easing, new TransformPositionY()));

        public static TransformSequence<T> ScaleTo<T>(this T drawable, float newScale, double duration = 0, EasingTypes easing = EasingTypes.None) where T : Drawable =>
            drawable.TransformTo(drawable.MakeTransform(new Vector2(newScale), duration, easing, new TransformScale()));

        public static TransformSequence<T> ScaleTo<T>(this T drawable, Vector2 newScale, double duration = 0, EasingTypes easing = EasingTypes.None) where T : Drawable =>
            drawable.TransformTo(drawable.MakeTransform(newScale, duration, easing, new TransformScale()));

        public static TransformSequence<T> ResizeTo<T>(this T drawable, float newSize, double duration = 0, EasingTypes easing = EasingTypes.None) where T : Drawable =>
            drawable.TransformTo(drawable.MakeTransform(new Vector2(newSize), duration, easing, new TransformSize()));

        public static TransformSequence<T> ResizeTo<T>(this T drawable, Vector2 newSize, double duration = 0, EasingTypes easing = EasingTypes.None) where T : Drawable =>
            drawable.TransformTo(drawable.MakeTransform(newSize, duration, easing, new TransformSize()));

        public static TransformSequence<T> ResizeWidthTo<T>(this T drawable, float newWidth, double duration = 0, EasingTypes easing = EasingTypes.None) where T : Drawable =>
            drawable.TransformTo(drawable.MakeTransform(newWidth, duration, easing, new TransformWidth()));

        public static TransformSequence<T> ResizeHeightTo<T>(this T drawable, float newHeight, double duration = 0, EasingTypes easing = EasingTypes.None) where T : Drawable =>
            drawable.TransformTo(drawable.MakeTransform(newHeight, duration, easing, new TransformHeight()));

        public static TransformSequence<T> MoveTo<T>(this T drawable, Vector2 newPosition, double duration = 0, EasingTypes easing = EasingTypes.None) where T : Drawable =>
            drawable.TransformTo(drawable.MakeTransform(newPosition, duration, easing, new TransformPosition()));

        public static TransformSequence<T> MoveToOffset<T>(this T drawable, Vector2 offset, double duration = 0, EasingTypes easing = EasingTypes.None) where T : Drawable =>
            drawable.MoveTo((drawable.Transforms.LastOrDefault(t => t is TransformPosition) as TransformPosition)?.EndValue ?? drawable.Position + offset, duration, easing);

        public static TransformSequence<T> FadeColour<T>(this T drawable, Color4 newColour, double duration = 0, EasingTypes easing = EasingTypes.None) where T : Drawable =>
            drawable.TransformTo(drawable.MakeTransform(newColour, duration, easing, new TransformColour()));

        public static TransformSequence<T> FlashColour<T>(this T drawable, Color4 flashColour, double duration, EasingTypes easing = EasingTypes.None) where T : Drawable
        {
            Color4 endValue = (drawable.Transforms.LastOrDefault(t => t is TransformColour) as TransformColour)?.EndValue ?? drawable.Colour;

            drawable.Flush(false, typeof(TransformColour));

            drawable.FadeColour(flashColour);
            return drawable.FadeColour(endValue, duration, easing);
        }

        /// <summary>
        /// Helper function for creating and adding a transform that fades the current <see cref="IContainer.EdgeEffect"/>.
        /// </summary>
        public static TransformSequence<T> FadeEdgeEffectTo<T>(this T container, float newAlpha, double duration = 0, EasingTypes easing = EasingTypes.None)
            where T : IContainer
        {
            container.Flush(false, typeof(TransformEdgeEffectColour));
            return container.TransformTo(container.MakeTransform(newAlpha, duration, easing, new TransformEdgeEffectAlpha()));
        }

        /// <summary>
        /// Helper function for creating and adding a transform that fades the current <see cref="IContainer.EdgeEffect"/>.
        /// </summary>
        public static TransformSequence<T> FadeEdgeEffectTo<T>(this T container, Color4 newColour, double duration = 0, EasingTypes easing = EasingTypes.None)
            where T : IContainer
        {
            container.Flush(false, typeof(TransformEdgeEffectAlpha));
            return container.TransformTo(container.MakeTransform(newColour, duration, easing, new TransformEdgeEffectColour()));
        }

        /// <summary>
        /// Tweens the <see cref="Container.RelativeChildSize"/> of a <see cref="Container"/>.
        /// </summary>
        /// <typeparam name="T">The type of the <see cref="Container"/> to be tweened.</typeparam>
        /// <param name="container">The <see cref="Container"/> to be tweened.</param>
        /// <param name="newSize">The child size to tween to.</param>
        /// <param name="duration">The tween duration.</param>
        /// <param name="easing">The tween easing.</param>
        /// <returns>A <see cref="TransformSequence{T}"/> to which further transforms can be added.</returns>
        public static TransformSequence<T> TransformRelativeChildSizeTo<T>(this T container, Vector2 newSize, double duration = 0, EasingTypes easing = EasingTypes.None)
            where T : IContainer
            => container.TransformTo(container.MakeTransform(newSize, duration, easing, new TransformRelativeChildSize()));

        /// <summary>
        /// Tweens the <see cref="Container.RelativeChildOffset"/> of a <see cref="Container"/>.
        /// </summary>
        /// <typeparam name="T">The type of the <see cref="Container"/> to be tweened.</typeparam>
        /// <param name="container">The <see cref="Container"/> to be tweened.</param>
        /// <param name="newOffset">The child offset to tween to.</param>
        /// <param name="duration">The tween duration.</param>
        /// <param name="easing">The tween easing.</param>
        /// <returns>A <see cref="TransformSequence{T}"/> to which further transforms can be added.</returns>
        public static TransformSequence<T> TransformRelativeChildOffsetTo<T>(this T container, Vector2 newOffset, double duration = 0, EasingTypes easing = EasingTypes.None)
            where T : IContainer
            => container.TransformTo(container.MakeTransform(newOffset, duration, easing, new TransformRelativeChildOffset()));

        /// <summary>
        /// Helper function for creating and adding a <see cref="Transform{TValue, T}"/> that blurs a <see cref="BufferedContainer{T}"/>.
        /// </summary>
        public static TransformSequence<T> BlurTo<T>(this T bufferedContainer, Vector2 newBlurSigma, double duration = 0, EasingTypes easing = EasingTypes.None)
            where T : IBufferedContainer
            => bufferedContainer.TransformTo(bufferedContainer.MakeTransform(newBlurSigma, duration, easing, new TransformBlurSigma()));

        public static TransformSequence<T> TransformSpacingTo<T>(this T flowContainer, Vector2 newSpacing, double duration = 0, EasingTypes easing = EasingTypes.None)
            where T : IFillFlowContainer
            => flowContainer.TransformTo(flowContainer.MakeTransform(newSpacing, duration, easing, new TransformSpacing()));
    }
}
