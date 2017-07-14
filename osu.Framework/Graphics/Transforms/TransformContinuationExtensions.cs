// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using OpenTK;
using OpenTK.Graphics;
using osu.Framework.Graphics.Containers;

namespace osu.Framework.Graphics.Transforms
{
    public static class TransformContinuationExtensions
    {
        public static TransformContinuation<T> Spin<T>(this TransformContinuation<T> t, double revolutionDuration, float startRotation = 0, int numRevolutions = -1) where T : Drawable =>
            t.Loop(0, numRevolutions, d => d.RotateTo(startRotation).RotateTo(startRotation + 360, revolutionDuration));

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

        public static TransformContinuation<T> FadeEdgeEffectTo<T>(this TransformContinuation<T> t, float newAlpha, double duration, EasingTypes easing = EasingTypes.None)
            where T : IContainer
            => t.AddChildGenerator(o => o.FadeEdgeEffectTo(newAlpha, duration, easing));

        public static TransformContinuation<T> FadeEdgeEffectTo<T>(this TransformContinuation<T> t, Color4 newColour, double duration = 0, EasingTypes easing = EasingTypes.None)
            where T : IContainer
            => t.AddChildGenerator(o => o.FadeEdgeEffectTo(newColour, duration, easing));

        public static TransformContinuation<T> TransformRelativeChildSizeTo<T>(this TransformContinuation<T> t, Vector2 newSize, double duration = 0, EasingTypes easing = EasingTypes.None)
            where T : IContainer
            => t.AddChildGenerator(o => o.TransformRelativeChildSizeTo(newSize, duration, easing));

        public static TransformContinuation<T> TransformRelativeChildOffsetTo<T>(this TransformContinuation<T> t, Vector2 newOffset, double duration = 0, EasingTypes easing = EasingTypes.None)
            where T : IContainer
            => t.AddChildGenerator(o => o.TransformRelativeChildOffsetTo(newOffset, duration, easing));

        public static TransformContinuation<T> BlurTo<T>(this TransformContinuation<T> t, Vector2 newBlurSigma, double duration = 0, EasingTypes easing = EasingTypes.None)
            where T : IBufferedContainer
            => t.AddChildGenerator(o => o.BlurTo(newBlurSigma, duration, easing));

        public static TransformContinuation<T> TransformSpacingTo<T>(this TransformContinuation<T> t, Vector2 newSpacing, double duration = 0, EasingTypes easing = EasingTypes.None)
            where T : IFillFlowContainer
            => t.AddChildGenerator(o => o.TransformSpacingTo(newSpacing, duration, easing));
    }
}
