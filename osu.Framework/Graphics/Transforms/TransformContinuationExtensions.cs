using OpenTK;
using OpenTK.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace osu.Framework.Graphics.Transforms
{
    public static class TransformContinuationExtensions
    {
        public static TransformContinuation<Drawable> FadeIn(this TransformContinuation<Drawable> t, double duration = 0, EasingTypes easing = EasingTypes.None) =>
            t.AddPrecondition(() => t.Origin.FadeIn(duration, easing));

        public static TransformContinuation<Drawable> FadeInFromZero(this TransformContinuation<Drawable> t, double duration = 0, EasingTypes easing = EasingTypes.None) =>
            t.AddPrecondition(() => t.Origin.FadeInFromZero(duration, easing));

        public static TransformContinuation<Drawable> FadeOut(this TransformContinuation<Drawable> t, double duration = 0, EasingTypes easing = EasingTypes.None) =>
            t.AddPrecondition(() => t.Origin.FadeOut(duration, easing));

        public static TransformContinuation<Drawable> FadeOutFromOne(this TransformContinuation<Drawable> t, double duration = 0, EasingTypes easing = EasingTypes.None) =>
            t.AddPrecondition(() => t.Origin.FadeOutFromOne(duration, easing));

        public static TransformContinuation<Drawable> FadeTo(this TransformContinuation<Drawable> t, float newAlpha, double duration = 0, EasingTypes easing = EasingTypes.None) =>
            t.AddPrecondition(() => t.Origin.FadeTo(newAlpha, duration, easing));

        public static TransformContinuation<Drawable> RotateTo(this TransformContinuation<Drawable> t, float newRotation, double duration = 0, EasingTypes easing = EasingTypes.None) =>
            t.AddPrecondition(() => t.Origin.RotateTo(newRotation, duration, easing));

        public static TransformContinuation<Drawable> MoveTo(this TransformContinuation<Drawable> t, Direction direction, float destination, double duration = 0, EasingTypes easing = EasingTypes.None) =>
            t.AddPrecondition(() => t.Origin.MoveTo(direction, destination, duration, easing));

        public static TransformContinuation<Drawable> MoveToX(this TransformContinuation<Drawable> t, float destination, double duration = 0, EasingTypes easing = EasingTypes.None) =>
            t.AddPrecondition(() => t.Origin.MoveToX(destination, duration, easing));

        public static TransformContinuation<Drawable> MoveToY(this TransformContinuation<Drawable> t, float destination, double duration = 0, EasingTypes easing = EasingTypes.None) =>
            t.AddPrecondition(() => t.Origin.MoveToY(destination, duration, easing));

        public static TransformContinuation<Drawable> ScaleTo(this TransformContinuation<Drawable> t, float newScale, double duration = 0, EasingTypes easing = EasingTypes.None) =>
            t.AddPrecondition(() => t.Origin.ScaleTo(newScale, duration, easing));

        public static TransformContinuation<Drawable> ScaleTo(this TransformContinuation<Drawable> t, Vector2 newScale, double duration = 0, EasingTypes easing = EasingTypes.None) =>
            t.AddPrecondition(() => t.Origin.ScaleTo(newScale, duration, easing));

        public static TransformContinuation<Drawable> ResizeTo(this TransformContinuation<Drawable> t, float newSize, double duration = 0, EasingTypes easing = EasingTypes.None) =>
            t.AddPrecondition(() => t.Origin.ResizeTo(newSize, duration, easing));

        public static TransformContinuation<Drawable> ResizeTo(this TransformContinuation<Drawable> t, Vector2 newSize, double duration = 0, EasingTypes easing = EasingTypes.None) =>
            t.AddPrecondition(() => t.Origin.ResizeTo(newSize, duration, easing));

        public static TransformContinuation<Drawable> ResizeWidthTo(this TransformContinuation<Drawable> t, float newWidth, double duration = 0, EasingTypes easing = EasingTypes.None) =>
            t.AddPrecondition(() => t.Origin.ResizeWidthTo(newWidth, duration, easing));

        public static TransformContinuation<Drawable> ResizeHeightTo(this TransformContinuation<Drawable> t, float newHeight, double duration = 0, EasingTypes easing = EasingTypes.None) =>
            t.AddPrecondition(() => t.Origin.ResizeHeightTo(newHeight, duration, easing));

        public static TransformContinuation<Drawable> MoveTo(this TransformContinuation<Drawable> t, Vector2 newPosition, double duration = 0, EasingTypes easing = EasingTypes.None) =>
            t.AddPrecondition(() => t.Origin.MoveTo(newPosition, duration, easing));

        public static TransformContinuation<Drawable> MoveToOffset(this TransformContinuation<Drawable> t, Vector2 offset, double duration = 0, EasingTypes easing = EasingTypes.None) =>
            t.AddPrecondition(() => t.Origin.MoveToOffset(offset, duration, easing));

        public static TransformContinuation<Drawable> FadeColour(this TransformContinuation<Drawable> t, Color4 newColour, double duration = 0, EasingTypes easing = EasingTypes.None) =>
            t.AddPrecondition(() => t.Origin.FadeColour(newColour, duration, easing));

        public static TransformContinuation<Drawable> FlashColour(this TransformContinuation<Drawable> t, Color4 flashColour, double duration, EasingTypes easing = EasingTypes.None) =>
            t.AddPrecondition(() => t.Origin.FlashColour(flashColour, duration, easing));
    }
}
