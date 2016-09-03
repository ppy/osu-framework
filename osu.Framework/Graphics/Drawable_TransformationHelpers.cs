//Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
//Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Collections.Generic;
using System.Text;
using OpenTK.Graphics;
using System.Diagnostics;
using OpenTK;
using osu.Framework.Graphics.Transformations;

namespace osu.Framework.Graphics
{
    public partial class Drawable : IDisposable
    {
        private double transformationDelay;

        public void ClearTransformations()
        {
            Transforms.Clear();
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
            Transforms.ForEach(t =>
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
            foreach (ITransform t in Transforms)
                if (t.EndTime > max) max = t.EndTime + 1; //adding 1ms here ensures we can expire on the current frame without issue.
            LifetimeEnd = max;

            if (calculateLifetimeStart)
            {
                double min = double.MaxValue;
                foreach (ITransform t in Transforms)
                    if (t.StartTime < min) min = t.StartTime;
                LifetimeStart = min < Int32.MaxValue ? min : Int32.MinValue;
            }

            return this;
        }

        public void TimeWarp(double change)
        {
            if (change == 0)
                return;

            foreach (ITransform t in Transforms)
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

        public Drawable FadeIn(double duration, EasingTypes easing = EasingTypes.None)
        {
            return FadeTo(1, duration, easing);
        }

        public TransformAlpha FadeInFromZero(double duration)
        {
            if (transformationDelay == 0)
            {
                Alpha = 0;
                Transforms.RemoveAll(t => t is TransformAlpha);
            }

            double startTime = Time + transformationDelay;

            TransformAlpha tr = new TransformAlpha(Clock)
            {
                StartTime = startTime,
                EndTime = startTime + duration,
                StartValue = 0,
                EndValue = 1,
            };
            Transforms.Add(tr);
            return tr;
        }

        public Drawable FadeOut(double duration, EasingTypes easing = EasingTypes.None)
        {
            return FadeTo(0, duration, easing);
        }

        public TransformAlpha FadeOutFromOne(double duration)
        {
            if (transformationDelay == 0)
            {
                Alpha = 1;
                Transforms.RemoveAll(t => t is TransformAlpha);
            }

            double startTime = Time + transformationDelay;

            TransformAlpha tr = new TransformAlpha(Clock)
            {
                StartTime = startTime,
                EndTime = startTime + duration,
                StartValue = 1,
                EndValue = 0,
            };
            Transforms.Add(tr);
            return tr;
        }

        #region Float-based helpers
        private Drawable transformFloatTo(float startValue, float newValue, double duration, EasingTypes easing, TransformFloat transform)
        {
            Type type = transform.GetType();
            if (transformationDelay == 0)
            {
                Transforms.RemoveAll(t => t.GetType() == type);
                if (startValue == newValue)
                    return this;
            }
            else
                startValue = (Transforms.FindLast(t => t.GetType() == type) as TransformFloat)?.EndValue ?? startValue;

            double startTime = Time + transformationDelay;

            transform.StartTime = startTime;
            transform.EndTime = startTime + duration;
            transform.StartValue = startValue;
            transform.EndValue = newValue;
            transform.Easing = easing;

            Transforms.Add(transform);

            return this;
        }

        public Drawable FadeTo(float newAlpha, double duration, EasingTypes easing = EasingTypes.None)
        {
            if (duration == 0)
            {
                Alpha = newAlpha;
                return this;
            }

            return transformFloatTo(Alpha, newAlpha, duration, easing, new TransformAlpha(Clock));
        }

        public Drawable ScaleTo(float newScale, double duration, EasingTypes easing = EasingTypes.None)
        {
            if (duration == 0)
            {
                Scale = newScale;
                return this;
            }

            return transformFloatTo(Scale, newScale, duration, easing, new TransformScale(Clock));
        }

        public Drawable RotateTo(float newRotation, double duration, EasingTypes easing = EasingTypes.None)
        {
            if (duration == 0)
            {
                Rotation = newRotation;
                return this;
            }

            return transformFloatTo(Rotation, newRotation, duration, easing, new TransformRotation(Clock));
        }

        public Drawable MoveToX(float destination, double duration, EasingTypes easing = EasingTypes.None)
        {
            if (duration == 0)
            {
                Position = new Vector2(destination, Position.Y);
                return this;
            }

            return transformFloatTo(Position.X, destination, duration, easing, new TransformPositionX(Clock));
        }

        public Drawable MoveToY(float destination, double duration, EasingTypes easing = EasingTypes.None)
        {
            if (duration == 0)
            {
                Position = new Vector2(Position.X, destination);
                return this;
            }

            return transformFloatTo(Position.Y, destination, duration, easing, new TransformPositionY(Clock));
        }
        #endregion

        #region Vector2-based helpers
        private Drawable transformVectorTo(Vector2 startValue, Vector2 newValue, double duration, EasingTypes easing, TransformVector transform)
        {
            Type type = transform.GetType();
            if (transformationDelay == 0)
            {
                Transforms.RemoveAll(t => t.GetType() == type);
                if (startValue == newValue)
                    return this;
            }
            else
                startValue = (Transforms.FindLast(t => t.GetType() == type) as TransformVector)?.EndValue ?? startValue;

            double startTime = Time + transformationDelay;

            transform.StartTime = startTime;
            transform.EndTime = startTime + duration;
            transform.StartValue = startValue;
            transform.EndValue = newValue;
            transform.Easing = easing;

            Transforms.Add(transform);

            return this;
        }

        public Drawable ScaleTo(Vector2 newScale, double duration, EasingTypes easing = EasingTypes.None)
        {
            if (duration == 0)
            {
                VectorScale = newScale;
                return this;
            }

            return transformVectorTo(VectorScale, newScale, duration, easing, new TransformScaleVector(Clock));
        }

        public Drawable MoveTo(Vector2 newPosition, double duration, EasingTypes easing = EasingTypes.None)
        {
            if (duration == 0)
            {
                Position = newPosition;
                return this;
            }

            return transformVectorTo(Position, newPosition, duration, easing, new TransformPosition(Clock));
        }

        public Drawable MoveToRelative(Vector2 offset, int duration, EasingTypes easing = EasingTypes.None)
        {
            return MoveTo((Transforms.FindLast(t => t is TransformPosition) as TransformPosition)?.EndValue ?? Position + offset, duration, easing);
        }
        #endregion

        #region Color4-based helpers
        public Drawable FadeColour(Color4 newColour, int duration, EasingTypes easing = EasingTypes.None)
        {
            Color4 startValue = Colour;
            if (transformationDelay == 0)
            {
                Transforms.RemoveAll(t => t is TransformColour);
                if (startValue == newColour)
                    return this;
            }
            else
                startValue = (Transforms.FindLast(t => t is TransformColour) as TransformColour)?.EndValue ?? startValue;

            double startTime = Time + transformationDelay;

            Transforms.Add(new TransformColour(Clock)
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

            Color4 startValue = (Transforms.FindLast(t => t is TransformColour) as TransformColour)?.EndValue ?? Colour;
            Transforms.RemoveAll(t => t is TransformColour);

            double startTime = Time + transformationDelay;

            Transforms.Add(new TransformColour(Clock)
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
