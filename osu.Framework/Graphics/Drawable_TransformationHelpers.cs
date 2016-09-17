//Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
//Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Collections.Generic;
using System.Text;
using OpenTK.Graphics;
using System.Diagnostics;
using OpenTK;
using osu.Framework.Graphics.Transformations;
using osu.Framework.Extensions.IEnumerableExtensions;

namespace osu.Framework.Graphics
{
    public partial class Drawable : IDisposable
    {
        private double transformationDelay;

        public void ClearTransformations()
        {
            transforms.Clear();
            DelayReset();
        }

        public Drawable Delay(double duration, bool propagateChildren = false)
        {
            if (duration == 0) return this;

            transformationDelay += duration;
            if (propagateChildren)
                Children.ForEach(c => c.Delay(duration, propagateChildren));
            return this;
        }

        public Drawable DelayReset()
        {
            Delay(-transformationDelay);
            Children.ForEach(c => c.DelayReset());
            return this;
        }

        public void Loop(int delay = 0)
        {
            transforms.ForEach(t =>
            {
                t.Loop(Math.Max(0, transformationDelay + delay - t.Duration));
            });
        }

        /// <summary>
        /// Make this drawable automatically clean itself up after all transformations have finished playing.
        /// Can be delayed using Delay().
        /// </summary>
        public Drawable Expire(bool calculateLifetimeStart = false)
        {
            //expiry should happen either at the end of the last transformation or using the current sequence delay (whichever is highest).
            double max = Time + transformationDelay;
            foreach (ITransform t in transforms)
                if (t.EndTime > max) max = t.EndTime + 1; //adding 1ms here ensures we can expire on the current frame without issue.
            LifetimeEnd = max;

            if (calculateLifetimeStart)
            {
                double min = double.MaxValue;
                foreach (ITransform t in transforms)
                    if (t.StartTime < min) min = t.StartTime;
                LifetimeStart = min < Int32.MaxValue ? min : Int32.MinValue;
            }

            return this;
        }

        public void TimeWarp(double change)
        {
            if (change == 0)
                return;

            foreach (ITransform t in transforms)
            {
                t.StartTime += change;
                t.EndTime += change;
            }
        }

        /// <summary>
        /// Hide sprite instantly.
        /// </summary>
        /// <returns></returns>
        public virtual void Hide()
        {
            FadeOut(0);
        }

        /// <summary>
        /// Show sprite instantly.
        /// </summary>
        public virtual void Show()
        {
            FadeIn(0);
        }

        public Drawable FadeIn(double duration = 0, EasingTypes easing = EasingTypes.None)
        {
            return FadeTo(1, duration, easing);
        }

        public TransformAlpha FadeInFromZero(double duration)
        {
            if (transformationDelay == 0)
            {
                Alpha = 0;
                transforms.RemoveAll(t => t is TransformAlpha);
            }

            double startTime = Time + transformationDelay;

            TransformAlpha tr = new TransformAlpha(Clock)
            {
                StartTime = startTime,
                EndTime = startTime + duration,
                StartValue = 0,
                EndValue = 1,
            };
            transforms.Add(tr);
            return tr;
        }

        public Drawable FadeOut(double duration = 0, EasingTypes easing = EasingTypes.None)
        {
            return FadeTo(0, duration, easing);
        }

        public TransformAlpha FadeOutFromOne(double duration)
        {
            if (transformationDelay == 0)
            {
                Alpha = 1;
                transforms.RemoveAll(t => t is TransformAlpha);
            }

            double startTime = Time + transformationDelay;

            TransformAlpha tr = new TransformAlpha(Clock)
            {
                StartTime = startTime,
                EndTime = startTime + duration,
                StartValue = 1,
                EndValue = 0,
            };
            transforms.Add(tr);
            return tr;
        }

        #region Float-based helpers
        private Drawable transformFloatTo(float startValue, float newValue, double duration, EasingTypes easing, TransformFloat transform)
        {
            Type type = transform.GetType();
            if (transformationDelay == 0)
            {
                updateTransformsOfType(type);
                transforms.RemoveAll(t => t.GetType() == type);
                if (startValue == newValue)
                    return this;
            }
            else
                startValue = (transforms.FindLast(t => t.GetType() == type) as TransformFloat)?.EndValue ?? startValue;

            double startTime = Time + transformationDelay;

            transform.StartTime = startTime;
            transform.EndTime = startTime + duration;
            transform.StartValue = startValue;
            transform.EndValue = newValue;
            transform.Easing = easing;

            if (duration == 0)
                transform.Apply(this);
            else
                transforms.Add(transform);

            return this;
        }

        public Drawable FadeTo(float newAlpha, double duration = 0, EasingTypes easing = EasingTypes.None)
        {
            return transformFloatTo(Alpha, newAlpha, duration, easing, new TransformAlpha(Clock));
        }

        public Drawable RotateTo(float newRotation, double duration = 0, EasingTypes easing = EasingTypes.None)
        {
            return transformFloatTo(Rotation, newRotation, duration, easing, new TransformRotation(Clock));
        }

        public Drawable MoveToX(float destination, double duration = 0, EasingTypes easing = EasingTypes.None)
        {
            return transformFloatTo(Position.X, destination, duration, easing, new TransformPositionX(Clock));
        }

        public Drawable MoveToY(float destination, double duration = 0, EasingTypes easing = EasingTypes.None)
        {
            return transformFloatTo(Position.Y, destination, duration, easing, new TransformPositionY(Clock));
        }
        #endregion

        #region Vector2-based helpers
        private Drawable transformVectorTo(Vector2 startValue, Vector2 newValue, double duration, EasingTypes easing, TransformVector transform)
        {
            Type type = transform.GetType();
            if (transformationDelay == 0)
            {
                transforms.RemoveAll(t => t.GetType() == type);

                if (startValue == newValue)
                    return this;
            }
            else
                startValue = (transforms.FindLast(t => t.GetType() == type) as TransformVector)?.EndValue ?? startValue;

            double startTime = Time + transformationDelay;

            transform.StartTime = startTime;
            transform.EndTime = startTime + duration;
            transform.StartValue = startValue;
            transform.EndValue = newValue;
            transform.Easing = easing;

            if (duration == 0)
                transform.Apply(this);
            else
                transforms.Add(transform);

            return this;
        }

        public Drawable ScaleTo(float newScale, double duration = 0, EasingTypes easing = EasingTypes.None)
        {
            return transformVectorTo(Scale, new Vector2(newScale), duration, easing, new TransformScaleVector(Clock));
        }

        public Drawable ScaleTo(Vector2 newScale, double duration = 0, EasingTypes easing = EasingTypes.None)
        {
            return transformVectorTo(Scale, newScale, duration, easing, new TransformScaleVector(Clock));
        }

        public Drawable MoveTo(Vector2 newPosition, double duration = 0, EasingTypes easing = EasingTypes.None)
        {
            return transformVectorTo(Position, newPosition, duration, easing, new TransformPosition(Clock));
        }

        public Drawable MoveToRelative(Vector2 offset, int duration = 0, EasingTypes easing = EasingTypes.None)
        {
            return MoveTo((transforms.FindLast(t => t is TransformPosition) as TransformPosition)?.EndValue ?? Position + offset, duration, easing);
        }
        #endregion

        #region Color4-based helpers
        public Drawable FadeColour(Color4 newColour, int duration, EasingTypes easing = EasingTypes.None)
        {
            Color4 startValue = Colour;
            if (transformationDelay == 0)
            {
                transforms.RemoveAll(t => t is TransformColour);
                if (startValue == newColour)
                    return this;
            }
            else
                startValue = (transforms.FindLast(t => t is TransformColour) as TransformColour)?.EndValue ?? startValue;

            double startTime = Time + transformationDelay;

            transforms.Add(new TransformColour(Clock)
            {
                StartTime = startTime,
                EndTime = startTime + duration,
                StartValue = startValue,
                EndValue = newColour,
                Easing = easing,
            });

            return this;
        }

        public Drawable FlashColour(Color4 flashColour, int duration)
        {
            Debug.Assert(transformationDelay == 0, @"FlashColour doesn't support Delay() currently");

            Color4 startValue = (transforms.FindLast(t => t is TransformColour) as TransformColour)?.EndValue ?? Colour;
            transforms.RemoveAll(t => t is TransformColour);

            double startTime = Time + transformationDelay;

            transforms.Add(new TransformColour(Clock)
            {
                StartTime = startTime,
                EndTime = startTime + duration,
                StartValue = flashColour,
                EndValue = startValue,
            });

            return this;
        }
        #endregion
    }
}
