// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using OpenTK;
using OpenTK.Graphics;
using osu.Framework.Graphics.Containers;
using System;
using System.Linq;

namespace osu.Framework.Graphics.Transforms
{
    public static class ITransformableExtensions
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
        /// <returns>A <see cref="TransformContinuation{T}"/> to which further transforms can be added.</returns>
        public static TransformContinuation<TThis> TransformTo<TThis, TValue, TBase>(
            this TThis t, TValue newValue, double duration, EasingTypes easing, Transform<TValue, TBase> transform)
            where TThis : ITransformable, TBase
        {
            double startTime = t.TransformStartTime;

            transform.StartTime = startTime;
            transform.EndTime = startTime + duration;
            transform.EndValue = newValue;
            transform.Easing = easing;

            t.AddTransform(transform);
            return new TransformContinuation<TThis>(t, transform);
        }

        public static TransformContinuation<T> Delayed<T>(this T transformable, double delay) where T : ITransformable =>
            new TransformContinuation<T>(transformable, true, delay);

        public static TransformContinuation<T> Loop<T>(this T transformable, double pause, int numIters, Func<T, TransformContinuation<T>> firstChildGenerator, params Func<T, TransformContinuation<T>>[] childGenerators)
            where T : ITransformable =>
            transformable.Delayed(0).Loop(pause, numIters, firstChildGenerator, childGenerators);

        public static TransformContinuation<T> Loop<T>(this T transformable, double pause, Func<T, TransformContinuation<T>> firstChildGenerator, params Func<T, TransformContinuation<T>>[] childGenerators)
            where T : ITransformable =>
            transformable.Loop(pause, -1, firstChildGenerator, childGenerators);

        public static TransformContinuation<T> Loop<T>(this T transformable, Func<T, TransformContinuation<T>> firstChildGenerator, params Func<T, TransformContinuation<T>>[] childGenerators)
            where T : ITransformable =>
            transformable.Loop(0, -1, firstChildGenerator, childGenerators);

        public static TransformContinuation<T> FadeIn<T>(this T drawable, double duration = 0, EasingTypes easing = EasingTypes.None) where T : Drawable =>
            drawable.FadeTo(1, duration, easing);

        public static TransformContinuation<T> FadeInFromZero<T>(this T drawable, double duration = 0, EasingTypes easing = EasingTypes.None) where T : Drawable
        {
            drawable.FadeTo(0);
            return drawable.FadeIn(duration, easing);
        }

        public static TransformContinuation<T> FadeOut<T>(this T drawable, double duration = 0, EasingTypes easing = EasingTypes.None) where T : Drawable =>
            drawable.FadeTo(0, duration, easing);

        public static TransformContinuation<T> FadeOutFromOne<T>(this T drawable, double duration = 0, EasingTypes easing = EasingTypes.None) where T : Drawable
        {
            drawable.FadeTo(1);
            return drawable.FadeOut(duration, easing);
        }

        public static TransformContinuation<T> FadeTo<T>(this T drawable, float newAlpha, double duration = 0, EasingTypes easing = EasingTypes.None) where T : Drawable =>
            drawable.TransformTo(newAlpha, duration, easing, new TransformAlpha(drawable));

        public static TransformContinuation<T> RotateTo<T>(this T drawable, float newRotation, double duration = 0, EasingTypes easing = EasingTypes.None) where T : Drawable =>
            drawable.TransformTo(newRotation, duration, easing, new TransformRotation(drawable));

        public static TransformContinuation<T> MoveTo<T>(this T drawable, Direction direction, float destination, double duration = 0, EasingTypes easing = EasingTypes.None) where T : Drawable
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

        public static TransformContinuation<T> MoveToX<T>(this T drawable, float destination, double duration = 0, EasingTypes easing = EasingTypes.None) where T : Drawable =>
            drawable.TransformTo(destination, duration, easing, new TransformPositionX(drawable));

        public static TransformContinuation<T> MoveToY<T>(this T drawable, float destination, double duration = 0, EasingTypes easing = EasingTypes.None) where T : Drawable =>
            drawable.TransformTo(destination, duration, easing, new TransformPositionY(drawable));

        public static TransformContinuation<T> ScaleTo<T>(this T drawable, float newScale, double duration = 0, EasingTypes easing = EasingTypes.None) where T : Drawable =>
            drawable.TransformTo(new Vector2(newScale), duration, easing, new TransformScale(drawable));

        public static TransformContinuation<T> ScaleTo<T>(this T drawable, Vector2 newScale, double duration = 0, EasingTypes easing = EasingTypes.None) where T : Drawable =>
            drawable.TransformTo(newScale, duration, easing, new TransformScale(drawable));

        public static TransformContinuation<T> ResizeTo<T>(this T drawable, float newSize, double duration = 0, EasingTypes easing = EasingTypes.None) where T : Drawable =>
            drawable.TransformTo(new Vector2(newSize), duration, easing, new TransformSize(drawable));

        public static TransformContinuation<T> ResizeTo<T>(this T drawable, Vector2 newSize, double duration = 0, EasingTypes easing = EasingTypes.None) where T : Drawable =>
            drawable.TransformTo(newSize, duration, easing, new TransformSize(drawable));

        public static TransformContinuation<T> ResizeWidthTo<T>(this T drawable, float newWidth, double duration = 0, EasingTypes easing = EasingTypes.None) where T : Drawable =>
            drawable.TransformTo(newWidth, duration, easing, new TransformWidth(drawable));

        public static TransformContinuation<T> ResizeHeightTo<T>(this T drawable, float newHeight, double duration = 0, EasingTypes easing = EasingTypes.None) where T : Drawable =>
            drawable.TransformTo(newHeight, duration, easing, new TransformHeight(drawable));

        public static TransformContinuation<T> MoveTo<T>(this T drawable, Vector2 newPosition, double duration = 0, EasingTypes easing = EasingTypes.None) where T : Drawable =>
            drawable.TransformTo(newPosition, duration, easing, new TransformPosition(drawable));

        public static TransformContinuation<T> MoveToOffset<T>(this T drawable, Vector2 offset, double duration = 0, EasingTypes easing = EasingTypes.None) where T : Drawable =>
            drawable.MoveTo((drawable.Transforms.LastOrDefault(t => t is TransformPosition) as TransformPosition)?.EndValue ?? drawable.Position + offset, duration, easing);

        public static TransformContinuation<T> FadeColour<T>(this T drawable, Color4 newColour, double duration = 0, EasingTypes easing = EasingTypes.None) where T : Drawable =>
            drawable.TransformTo(newColour, duration, easing, new TransformColour(drawable));

        public static TransformContinuation<T> FlashColour<T>(this T drawable, Color4 flashColour, double duration, EasingTypes easing = EasingTypes.None) where T : Drawable
        {
            Color4 endValue = (drawable.Transforms.LastOrDefault(t => t is TransformColour) as TransformColour)?.EndValue ?? drawable.Colour;

            drawable.Flush(false, typeof(TransformColour));

            drawable.FadeColour(flashColour);
            return drawable.FadeColour(endValue, duration, easing);
        }

        /// <summary>
        /// Helper function for creating and adding a transform that fades the current <see cref="EdgeEffect"/>.
        /// </summary>
        public static TransformContinuation<T> FadeEdgeEffectTo<T>(this T container, float newAlpha, double duration = 0, EasingTypes easing = EasingTypes.None)
            where T : IContainer
        {
            container.Flush(false, typeof(TransformEdgeEffectColour));
            return container.TransformTo(newAlpha, duration, easing, new TransformEdgeEffectAlpha(container));
        }

        /// <summary>
        /// Helper function for creating and adding a transform that fades the current <see cref="EdgeEffect"/>.
        /// </summary>
        public static TransformContinuation<T> FadeEdgeEffectTo<T>(this T container, Color4 newColour, double duration = 0, EasingTypes easing = EasingTypes.None)
            where T : IContainer
        {
            container.Flush(false, typeof(TransformEdgeEffectAlpha));
            return container.TransformTo(newColour, duration, easing, new TransformEdgeEffectColour(container));
        }

        /// <summary>
        /// Tweens the <see cref="Container.RelativeChildSize"/> of a <see cref="Container"/>.
        /// </summary>
        /// <typeparam name="T">The type of the <see cref="Container"/> to be tweened.</typeparam>
        /// <param name="container">The <see cref="Container"/> to be tweened.</param>
        /// <param name="newSize">The child size to tween to.</param>
        /// <param name="duration">The tween duration.</param>
        /// <param name="easing">The tween easing.</param>
        /// <returns>A <see cref="TransformContinuation{T}"/> to which further transforms can be added.</returns>
        public static TransformContinuation<T> TransformRelativeChildSizeTo<T>(this T container, Vector2 newSize, double duration = 0, EasingTypes easing = EasingTypes.None)
            where T : IContainer
            => container.TransformTo(newSize, duration, easing, new TransformRelativeChildSize(container));

        /// <summary>
        /// Tweens the <see cref="Container.RelativeChildOffset"/> of a <see cref="Container"/>.
        /// </summary>
        /// <typeparam name="T">The type of the <see cref="Container"/> to be tweened.</typeparam>
        /// <param name="container">The <see cref="Container"/> to be tweened.</param>
        /// <param name="newOffset">The child offset to tween to.</param>
        /// <param name="duration">The tween duration.</param>
        /// <param name="easing">The tween easing.</param>
        /// <returns>A <see cref="TransformContinuation{T}"/> to which further transforms can be added.</returns>
        public static TransformContinuation<T> TransformRelativeChildOffsetTo<T>(this T container, Vector2 newOffset, double duration = 0, EasingTypes easing = EasingTypes.None)
            where T : IContainer
            => container.TransformTo(newOffset, duration, easing, new TransformRelativeChildOffset(container));

        /// <summary>
        /// Helper function for creating and adding a <see cref="Transform{TValue, T}"/> that blurs a <see cref="BufferedContainer{T}"/>.
        /// </summary>
        public static TransformContinuation<T> BlurTo<T>(this T bufferedContainer, Vector2 newBlurSigma, double duration = 0, EasingTypes easing = EasingTypes.None)
            where T : IBufferedContainer
            => bufferedContainer.TransformTo(newBlurSigma, duration, easing, new TransformBlurSigma(bufferedContainer));

        public static TransformContinuation<T> TransformSpacingTo<T>(this T flowContainer, Vector2 newSpacing, double duration = 0, EasingTypes easing = EasingTypes.None)
            where T : IFillFlowContainer
            => flowContainer.TransformTo(newSpacing, duration, easing, new TransformSpacing(flowContainer));
    }
}
