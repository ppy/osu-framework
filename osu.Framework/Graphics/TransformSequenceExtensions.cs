// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using OpenTK;
using OpenTK.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Transforms;

namespace osu.Framework.Graphics
{
    public static class TransformSequenceExtensions
    {
        public static TransformSequence<T> TransformTo<T, TValue>(this TransformSequence<T> t, Transform transform)
            where T : ITransformable
            => t.Append(o => o.TransformTo(transform));

        public static TransformSequence<T> TransformTo<T, TValue>(this TransformSequence<T> t, string propertyOrFieldName, TValue newValue, double duration = 0, EasingTypes easing = EasingTypes.None)
            where T : ITransformable
            => t.Append(o => o.TransformTo(propertyOrFieldName, newValue, duration, easing));

        public static TransformSequence<T> Spin<T>(this TransformSequence<T> t, double revolutionDuration, float startRotation = 0) where T : Drawable =>
            t.Loop(0, d => d.RotateTo(startRotation).RotateTo(startRotation + 360, revolutionDuration));

        public static TransformSequence<T> Spin<T>(this TransformSequence<T> t, double revolutionDuration, float startRotation, int numRevolutions) where T : Drawable =>
            t.Loop(numRevolutions, 0, d => d.RotateTo(startRotation).RotateTo(startRotation + 360, revolutionDuration));

        public static TransformSequence<T> FadeIn<T>(this TransformSequence<T> t, double duration = 0, EasingTypes easing = EasingTypes.None) where T : Drawable =>
            t.Append(o => o.FadeIn(duration, easing));

        public static TransformSequence<T> FadeInFromZero<T>(this TransformSequence<T> t, double duration = 0, EasingTypes easing = EasingTypes.None) where T : Drawable =>
            t.Append(o => o.FadeInFromZero(duration, easing));

        public static TransformSequence<T> FadeOut<T>(this TransformSequence<T> t, double duration = 0, EasingTypes easing = EasingTypes.None) where T : Drawable =>
            t.Append(o => o.FadeOut(duration, easing));

        public static TransformSequence<T> FadeOutFromOne<T>(this TransformSequence<T> t, double duration = 0, EasingTypes easing = EasingTypes.None) where T : Drawable =>
            t.Append(o => o.FadeOutFromOne(duration, easing));

        public static TransformSequence<T> FadeTo<T>(this TransformSequence<T> t, float newAlpha, double duration = 0, EasingTypes easing = EasingTypes.None) where T : Drawable =>
            t.Append(o => o.FadeTo(newAlpha, duration, easing));

        public static TransformSequence<T> RotateTo<T>(this TransformSequence<T> t, float newRotation, double duration = 0, EasingTypes easing = EasingTypes.None) where T : Drawable =>
            t.Append(o => o.RotateTo(newRotation, duration, easing));

        public static TransformSequence<T> MoveTo<T>(this TransformSequence<T> t, Direction direction, float destination, double duration = 0, EasingTypes easing = EasingTypes.None) where T : Drawable =>
            t.Append(o => o.MoveTo(direction, destination, duration, easing));

        public static TransformSequence<T> MoveToX<T>(this TransformSequence<T> t, float destination, double duration = 0, EasingTypes easing = EasingTypes.None) where T : Drawable =>
            t.Append(o => o.MoveToX(destination, duration, easing));

        public static TransformSequence<T> MoveToY<T>(this TransformSequence<T> t, float destination, double duration = 0, EasingTypes easing = EasingTypes.None) where T : Drawable =>
            t.Append(o => o.MoveToY(destination, duration, easing));

        public static TransformSequence<T> ScaleTo<T>(this TransformSequence<T> t, float newScale, double duration = 0, EasingTypes easing = EasingTypes.None) where T : Drawable =>
            t.Append(o => o.ScaleTo(newScale, duration, easing));

        public static TransformSequence<T> ScaleTo<T>(this TransformSequence<T> t, Vector2 newScale, double duration = 0, EasingTypes easing = EasingTypes.None) where T : Drawable =>
            t.Append(o => o.ScaleTo(newScale, duration, easing));

        public static TransformSequence<T> ResizeTo<T>(this TransformSequence<T> t, float newSize, double duration = 0, EasingTypes easing = EasingTypes.None) where T : Drawable =>
            t.Append(o => o.ResizeTo(newSize, duration, easing));

        public static TransformSequence<T> ResizeTo<T>(this TransformSequence<T> t, Vector2 newSize, double duration = 0, EasingTypes easing = EasingTypes.None) where T : Drawable =>
            t.Append(o => o.ResizeTo(newSize, duration, easing));

        public static TransformSequence<T> ResizeWidthTo<T>(this TransformSequence<T> t, float newWidth, double duration = 0, EasingTypes easing = EasingTypes.None) where T : Drawable =>
            t.Append(o => o.ResizeWidthTo(newWidth, duration, easing));

        public static TransformSequence<T> ResizeHeightTo<T>(this TransformSequence<T> t, float newHeight, double duration = 0, EasingTypes easing = EasingTypes.None) where T : Drawable =>
            t.Append(o => o.ResizeHeightTo(newHeight, duration, easing));

        public static TransformSequence<T> MoveTo<T>(this TransformSequence<T> t, Vector2 newPosition, double duration = 0, EasingTypes easing = EasingTypes.None) where T : Drawable =>
            t.Append(o => o.MoveTo(newPosition, duration, easing));

        public static TransformSequence<T> MoveToOffset<T>(this TransformSequence<T> t, Vector2 offset, double duration = 0, EasingTypes easing = EasingTypes.None)
            where T : Drawable
            => t.Append(o => o.MoveToOffset(offset, duration, easing));

        public static TransformSequence<T> FadeColour<T>(this TransformSequence<T> t, Color4 newColour, double duration = 0, EasingTypes easing = EasingTypes.None)
            where T : Drawable
            => t.Append(o => o.FadeColour(newColour, duration, easing));

        public static TransformSequence<T> FlashColour<T>(this TransformSequence<T> t, Color4 flashColour, double duration, EasingTypes easing = EasingTypes.None)
            where T : Drawable
            => t.Append(o => o.FlashColour(flashColour, duration, easing));

        public static TransformSequence<T> FadeEdgeEffectTo<T>(this TransformSequence<T> t, float newAlpha, double duration, EasingTypes easing = EasingTypes.None)
            where T : IContainer
            => t.Append(o => o.FadeEdgeEffectTo(newAlpha, duration, easing));

        public static TransformSequence<T> FadeEdgeEffectTo<T>(this TransformSequence<T> t, Color4 newColour, double duration = 0, EasingTypes easing = EasingTypes.None)
            where T : IContainer
            => t.Append(o => o.FadeEdgeEffectTo(newColour, duration, easing));

        public static TransformSequence<T> TransformRelativeChildSizeTo<T>(this TransformSequence<T> t, Vector2 newSize, double duration = 0, EasingTypes easing = EasingTypes.None)
            where T : IContainer
            => t.Append(o => o.TransformRelativeChildSizeTo(newSize, duration, easing));

        public static TransformSequence<T> TransformRelativeChildOffsetTo<T>(this TransformSequence<T> t, Vector2 newOffset, double duration = 0, EasingTypes easing = EasingTypes.None)
            where T : IContainer
            => t.Append(o => o.TransformRelativeChildOffsetTo(newOffset, duration, easing));

        public static TransformSequence<T> BlurTo<T>(this TransformSequence<T> t, Vector2 newBlurSigma, double duration = 0, EasingTypes easing = EasingTypes.None)
            where T : IBufferedContainer
            => t.Append(o => o.BlurTo(newBlurSigma, duration, easing));

        public static TransformSequence<T> TransformSpacingTo<T>(this TransformSequence<T> t, Vector2 newSpacing, double duration = 0, EasingTypes easing = EasingTypes.None)
            where T : IFillFlowContainer
            => t.Append(o => o.TransformSpacingTo(newSpacing, duration, easing));
    }
}
