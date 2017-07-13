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
    public static class TransformHelpers
    {
        #region Drawable

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

        #endregion

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
        /// Tweens the <see cref="RelativeChildSize"/> of this <see cref="Container"/>.
        /// </summary>
        /// <param name="newSize">The coordinate space to tween to.</param>
        /// <param name="duration">The tween duration.</param>
        /// <param name="easing">The tween easing.</param>
        public static TransformContinuation<T> TransformRelativeChildSizeTo<T>(this T container, Vector2 newSize, double duration = 0, EasingTypes easing = EasingTypes.None)
            where T : Transformable, IContainer
            => container.TransformTo(newSize, duration, easing, new TransformRelativeChildSize(container));

        /// <summary>
        /// Tweens the <see cref="RelativeChildOffset"/> of this <see cref="Container"/>.
        /// </summary>
        /// <param name="newOffset">The coordinate space to tween to.</param>
        /// <param name="duration">The tween duration.</param>
        /// <param name="easing">The tween easing.</param>
        public static TransformContinuation<T> TransformRelativeChildOffsetTo<T>(this T container, Vector2 newOffset, double duration = 0, EasingTypes easing = EasingTypes.None)
            where T : Transformable, IContainer
            => container.TransformTo(newOffset, duration, easing, new TransformRelativeChildOffset(container));

        /// <summary>
        /// Helper function for creating and adding a <see cref="Transform{TValue, T}"/> that blurs the buffered container.
        /// </summary>
        public static TransformContinuation<T> BlurTo<T>(this T bufferedContainer, Vector2 newBlurSigma, double duration = 0, EasingTypes easing = EasingTypes.None)
            where T : Transformable, IBufferedContainer
            => bufferedContainer.TransformTo(newBlurSigma, duration, easing, new TransformBlurSigma(bufferedContainer));

        public static TransformContinuation<T> TransformSpacingTo<T>(this T flowContainer, Vector2 newSpacing, double duration = 0, EasingTypes easing = EasingTypes.None)
            where T : Transformable, IFillFlowContainer
            => flowContainer.TransformTo(newSpacing, duration, easing, new TransformSpacing(flowContainer));
    }

    public static class TransformContinuationExtensions
    {
        #region Drawable

        public static TransformContinuation<T> FadeIn<T>(this TransformContinuation<T> t, double duration = 0, EasingTypes easing = EasingTypes.None) where T : Drawable =>
            t.AddChildGenerator(o => o.FadeIn(duration, easing));

        public static TransformContinuation<T> FadeInFromZero<T>(this TransformContinuation<T> t, double duration = 0, EasingTypes easing = EasingTypes.None) where T : Drawable =>
            t.AddChildGenerator(o => o.FadeInFromZero(duration, easing));

        public static TransformContinuation<T> FadeOut<T>(this TransformContinuation<T> t, double duration = 0, EasingTypes easing = EasingTypes.None) where T : Drawable =>
            t.AddChildGenerator(o => o.FadeOut(duration, easing));

        public static TransformContinuation<T> FadeOutFromOne<T>(this TransformContinuation<T> t, double duration = 0, EasingTypes easing = EasingTypes.None) where T : Drawable =>
            t.AddChildGenerator(o => o.FadeOutFromOne(duration, easing));

        public static TransformContinuation<T> FadeTo<T>(this TransformContinuation<T> t, float newAlpha, double duration = 0, EasingTypes easing = EasingTypes.None) where T : Drawable =>
            t.AddChildGenerator(o => o.FadeTo(newAlpha, duration, easing));

        public static TransformContinuation<T> RotateTo<T>(this TransformContinuation<T> t, float newRotation, double duration = 0, EasingTypes easing = EasingTypes.None) where T : Drawable =>
            t.AddChildGenerator(o => o.RotateTo(newRotation, duration, easing));

        public static TransformContinuation<T> MoveTo<T>(this TransformContinuation<T> t, Direction direction, float destination, double duration = 0, EasingTypes easing = EasingTypes.None) where T : Drawable =>
            t.AddChildGenerator(o => o.MoveTo(direction, destination, duration, easing));

        public static TransformContinuation<T> MoveToX<T>(this TransformContinuation<T> t, float destination, double duration = 0, EasingTypes easing = EasingTypes.None) where T : Drawable =>
            t.AddChildGenerator(o => o.MoveToX(destination, duration, easing));

        public static TransformContinuation<T> MoveToY<T>(this TransformContinuation<T> t, float destination, double duration = 0, EasingTypes easing = EasingTypes.None) where T : Drawable =>
            t.AddChildGenerator(o => o.MoveToY(destination, duration, easing));

        public static TransformContinuation<T> ScaleTo<T>(this TransformContinuation<T> t, float newScale, double duration = 0, EasingTypes easing = EasingTypes.None) where T : Drawable =>
            t.AddChildGenerator(o => o.ScaleTo(newScale, duration, easing));

        public static TransformContinuation<T> ScaleTo<T>(this TransformContinuation<T> t, Vector2 newScale, double duration = 0, EasingTypes easing = EasingTypes.None) where T : Drawable =>
            t.AddChildGenerator(o => o.ScaleTo(newScale, duration, easing));

        public static TransformContinuation<T> ResizeTo<T>(this TransformContinuation<T> t, float newSize, double duration = 0, EasingTypes easing = EasingTypes.None) where T : Drawable =>
            t.AddChildGenerator(o => o.ResizeTo(newSize, duration, easing));

        public static TransformContinuation<T> ResizeTo<T>(this TransformContinuation<T> t, Vector2 newSize, double duration = 0, EasingTypes easing = EasingTypes.None) where T : Drawable =>
            t.AddChildGenerator(o => o.ResizeTo(newSize, duration, easing));

        public static TransformContinuation<T> ResizeWidthTo<T>(this TransformContinuation<T> t, float newWidth, double duration = 0, EasingTypes easing = EasingTypes.None) where T : Drawable =>
            t.AddChildGenerator(o => o.ResizeWidthTo(newWidth, duration, easing));

        public static TransformContinuation<T> ResizeHeightTo<T>(this TransformContinuation<T> t, float newHeight, double duration = 0, EasingTypes easing = EasingTypes.None) where T : Drawable =>
            t.AddChildGenerator(o => o.ResizeHeightTo(newHeight, duration, easing));

        public static TransformContinuation<T> MoveTo<T>(this TransformContinuation<T> t, Vector2 newPosition, double duration = 0, EasingTypes easing = EasingTypes.None) where T : Drawable =>
            t.AddChildGenerator(o => o.MoveTo(newPosition, duration, easing));

        public static TransformContinuation<T> MoveToOffset<T>(this TransformContinuation<T> t, Vector2 offset, double duration = 0, EasingTypes easing = EasingTypes.None) where T : Drawable =>
            t.AddChildGenerator(o => o.MoveToOffset(offset, duration, easing));

        public static TransformContinuation<T> FadeColour<T>(this TransformContinuation<T> t, Color4 newColour, double duration = 0, EasingTypes easing = EasingTypes.None) where T : Drawable =>
            t.AddChildGenerator(o => o.FadeColour(newColour, duration, easing));

        public static TransformContinuation<T> FlashColour<T>(this TransformContinuation<T> t, Color4 flashColour, double duration, EasingTypes easing = EasingTypes.None) where T : Drawable =>
            t.AddChildGenerator(o => o.FlashColour(flashColour, duration, easing));

        #endregion

        public static TransformContinuation<T> FadeEdgeEffectTo<T>(this TransformContinuation<T> t, float newAlpha, double duration, EasingTypes easing = EasingTypes.None)
            where T : IContainer
            => t.AddChildGenerator(o => o.FadeEdgeEffectTo(newAlpha, duration, easing));

        public static TransformContinuation<T> FadeEdgeEffectTo<T>(this TransformContinuation<T> t, Color4 newColour, double duration = 0, EasingTypes easing = EasingTypes.None)
            where T : IContainer
            => t.AddChildGenerator(o => o.FadeEdgeEffectTo(newColour, duration, easing));

        public static TransformContinuation<T> TransformRelativeChildSizeTo<T>(this TransformContinuation<T> t, Vector2 newSize, double duration = 0, EasingTypes easing = EasingTypes.None)
            where T : Transformable, IContainer
            => t.AddChildGenerator(o => o.TransformRelativeChildSizeTo(newSize, duration, easing));

        public static TransformContinuation<T> TransformRelativeChildOffsetTo<T>(this TransformContinuation<T> t, Vector2 newOffset, double duration = 0, EasingTypes easing = EasingTypes.None)
            where T : Transformable, IContainer
            => t.AddChildGenerator(o => o.TransformRelativeChildOffsetTo(newOffset, duration, easing));

        public static TransformContinuation<T> BlurTo<T>(this TransformContinuation<T> t, Vector2 newBlurSigma, double duration = 0, EasingTypes easing = EasingTypes.None)
            where T : Transformable, IBufferedContainer
            => t.AddChildGenerator(o => o.BlurTo(newBlurSigma, duration, easing));

        public static TransformContinuation<T> TransformSpacingTo<T>(this TransformContinuation<T> t, Vector2 newSpacing, double duration = 0, EasingTypes easing = EasingTypes.None)
            where T : Transformable, IFillFlowContainer
            => t.AddChildGenerator(o => o.TransformSpacingTo(newSpacing, duration, easing));
    }
}
