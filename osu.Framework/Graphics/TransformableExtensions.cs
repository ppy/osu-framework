// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using OpenTK;
using OpenTK.Graphics;
using osu.Framework.Graphics.Colour;
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
            var result = new TransformSequence<TThis>(t);
            result.Append(transform);
            t.AddTransform(transform);
            return result;
        }

        public static Transform<TValue, TBase> PopulateTransform<TThis, TValue, TBase>(
            this TThis t, Transform<TValue, TBase> transform, TValue newValue, double duration, EasingTypes easing)
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

        public static Transform<TValue, TThis> MakeTransform<TThis, TValue>(this TThis t, string propertyOrFieldName, TValue newValue, double duration, EasingTypes easing) where TThis : ITransformable =>
            t.PopulateTransform(new TransformCustom<TValue, TThis>(propertyOrFieldName), newValue, duration, easing);

        public static TransformSequence<T> Delayed<T>(this T transformable, double delay) where T : ITransformable =>
            new TransformSequence<T>(transformable).Then(delay);

        public static TransformSequence<T> Loop<T>(this T transformable, int numIters, double pause, TransformSequence<T>.Generator firstChildGenerator, params TransformSequence<T>.Generator[] childGenerators)
            where T : ITransformable =>
            transformable.Delayed(0).Loop(numIters, pause, firstChildGenerator, childGenerators);

        public static TransformSequence<T> Loop<T>(this T transformable, double pause, TransformSequence<T>.Generator firstChildGenerator, params TransformSequence<T>.Generator[] childGenerators)
            where T : ITransformable =>
            transformable.Loop(-1, pause, firstChildGenerator, childGenerators);

        public static TransformSequence<T> Loop<T>(this T transformable, TransformSequence<T>.Generator firstChildGenerator, params TransformSequence<T>.Generator[] childGenerators)
            where T : ITransformable =>
            transformable.Loop(-1, 0, firstChildGenerator, childGenerators);

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
            drawable.TransformTo(drawable.MakeTransform(nameof(drawable.Alpha), newAlpha, duration, easing));

        public static TransformSequence<T> RotateTo<T>(this T drawable, float newRotation, double duration = 0, EasingTypes easing = EasingTypes.None) where T : Drawable =>
            drawable.TransformTo(drawable.MakeTransform(nameof(drawable.Rotation), newRotation, duration, easing));

        public static TransformSequence<T> Spin<T>(this T drawable, double revolutionDuration, float startRotation = 0) where T : Drawable =>
            drawable.Delayed(0).Spin(revolutionDuration, startRotation);

        public static TransformSequence<T> Spin<T>(this T drawable, double revolutionDuration, float startRotation, int numRevolutions) where T : Drawable =>
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
            drawable.TransformTo(drawable.MakeTransform(nameof(drawable.X), destination, duration, easing));

        public static TransformSequence<T> MoveToY<T>(this T drawable, float destination, double duration = 0, EasingTypes easing = EasingTypes.None) where T : Drawable =>
            drawable.TransformTo(drawable.MakeTransform(nameof(drawable.Y), destination, duration, easing));

        public static TransformSequence<T> ScaleTo<T>(this T drawable, float newScale, double duration = 0, EasingTypes easing = EasingTypes.None) where T : Drawable =>
            drawable.ScaleTo(new Vector2(newScale), duration, easing);

        public static TransformSequence<T> ScaleTo<T>(this T drawable, Vector2 newScale, double duration = 0, EasingTypes easing = EasingTypes.None) where T : Drawable =>
            drawable.TransformTo(drawable.MakeTransform(nameof(drawable.Scale), newScale, duration, easing));

        public static TransformSequence<T> ResizeTo<T>(this T drawable, float newSize, double duration = 0, EasingTypes easing = EasingTypes.None) where T : Drawable =>
            drawable.ResizeTo(new Vector2(newSize), duration, easing);

        public static TransformSequence<T> ResizeTo<T>(this T drawable, Vector2 newSize, double duration = 0, EasingTypes easing = EasingTypes.None) where T : Drawable =>
            drawable.TransformTo(drawable.MakeTransform(nameof(drawable.Size), newSize, duration, easing));

        public static TransformSequence<T> ResizeWidthTo<T>(this T drawable, float newWidth, double duration = 0, EasingTypes easing = EasingTypes.None) where T : Drawable =>
            drawable.TransformTo(drawable.MakeTransform(nameof(drawable.Width), newWidth, duration, easing));

        public static TransformSequence<T> ResizeHeightTo<T>(this T drawable, float newHeight, double duration = 0, EasingTypes easing = EasingTypes.None) where T : Drawable =>
            drawable.TransformTo(drawable.MakeTransform(nameof(drawable.Height), newHeight, duration, easing));

        public static TransformSequence<T> MoveTo<T>(this T drawable, Vector2 newPosition, double duration = 0, EasingTypes easing = EasingTypes.None) where T : Drawable =>
            drawable.TransformTo(drawable.MakeTransform(nameof(drawable.Position), newPosition, duration, easing));

        //public static TransformSequence<T> MoveToOffset<T>(this T drawable, Vector2 offset, double duration = 0, EasingTypes easing = EasingTypes.None) where T : Drawable =>
        //    drawable.MoveTo((drawable.Transforms.LastOrDefault(t => t is TransformPosition) as TransformPosition)?.EndValue ?? drawable.Position + offset, duration, easing);

        public static TransformSequence<T> FadeColour<T>(this T drawable, Color4 newColour, double duration = 0, EasingTypes easing = EasingTypes.None) where T : Drawable =>
            drawable.TransformTo(drawable.PopulateTransform(new TransformCustom<SRGBColour, Drawable>(nameof(drawable.Colour)), newColour, duration, easing));

        public static TransformSequence<T> FlashColour<T>(this T drawable, Color4 flashColour, double duration, EasingTypes easing = EasingTypes.None) where T : Drawable
        {
            //Color4 endValue = (drawable.Transforms.LastOrDefault(t => t is TransformColour) as TransformColour)?.EndValue ?? drawable.Colour;

            //drawable.Flush(false, typeof(TransformColour));

            Color4 endValue = drawable.Colour;
            drawable.FadeColour(flashColour);
            return drawable.FadeColour(endValue, duration, easing);
        }

        /// <summary>
        /// Helper function for creating and adding a transform that fades the current <see cref="IContainer.EdgeEffect"/>.
        /// </summary>
        public static TransformSequence<T> FadeEdgeEffectTo<T>(this T container, float newAlpha, double duration = 0, EasingTypes easing = EasingTypes.None)
            where T : IContainer
        {
            Color4 targetColour = container.EdgeEffect.Colour;
            targetColour.A = newAlpha;
            return container.FadeEdgeEffectTo(targetColour, duration, easing);
        }

        /// <summary>
        /// Helper function for creating and adding a transform that fades the current <see cref="IContainer.EdgeEffect"/>.
        /// </summary>
        public static TransformSequence<T> FadeEdgeEffectTo<T>(this T container, Color4 newColour, double duration = 0, EasingTypes easing = EasingTypes.None) where T : IContainer =>
            container.TransformTo(container.PopulateTransform(new TransformEdgeEffectColour(), newColour, duration, easing));

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
            => container.TransformTo(container.MakeTransform(nameof(container.RelativeChildSize), newSize, duration, easing));

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
            => container.TransformTo(container.MakeTransform(nameof(container.RelativeChildOffset), newOffset, duration, easing));

        /// <summary>
        /// Helper function for creating and adding a <see cref="Transform{TValue, T}"/> that blurs a <see cref="BufferedContainer{T}"/>.
        /// </summary>
        public static TransformSequence<T> BlurTo<T>(this T bufferedContainer, Vector2 newBlurSigma, double duration = 0, EasingTypes easing = EasingTypes.None)
            where T : IBufferedContainer
            => bufferedContainer.TransformTo(bufferedContainer.MakeTransform(nameof(bufferedContainer.BlurSigma), newBlurSigma, duration, easing));

        public static TransformSequence<T> TransformSpacingTo<T>(this T flowContainer, Vector2 newSpacing, double duration = 0, EasingTypes easing = EasingTypes.None)
            where T : IFillFlowContainer
            => flowContainer.TransformTo(flowContainer.MakeTransform(nameof(flowContainer.Spacing), newSpacing, duration, easing));
    }
}
