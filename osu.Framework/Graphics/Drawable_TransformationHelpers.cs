// Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Diagnostics;
using osu.Framework.Extensions.IEnumerableExtensions;
using osu.Framework.Graphics.Transformations;
using osu.Framework.Threading;
using OpenTK;
using OpenTK.Graphics;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics.Colour;
using osu.Framework.Timing;

namespace osu.Framework.Graphics
{
    public partial class Drawable : IDisposable
    {
        private double transformationDelay;

        public void ClearTransformations()
        {
            DelayReset();
            transforms?.Clear();
        }

        public virtual Drawable Delay(double duration, bool propagateChildren = false)
        {
            if (duration == 0) return this;

            transformationDelay += duration;
            return this;
        }

        public ScheduledDelegate Schedule(Action action) => Scheduler.AddDelayed(action, transformationDelay);

        /// <summary>
        /// Flush specified transformations, using the last available values (ignoring current clock time).
        /// </summary>
        /// <param name="propagateChildren">Whether we also flush down the child tree.</param>
        /// <param name="flushType">An optional type of transform to flush. Null for all types.</param>
        public virtual void Flush(bool propagateChildren = false, Type flushType = null)
        {
            var operateTransforms = flushType == null ? Transforms : Transforms.FindAll(t => t.GetType() == flushType);

            double maxTime = double.MinValue;
            foreach (ITransform t in operateTransforms)
                if (t.EndTime > maxTime)
                    maxTime = t.EndTime;

            FrameTimeInfo maxTimeInfo = new FrameTimeInfo { Current = maxTime };

            foreach (ITransform t in operateTransforms)
            {
                t.UpdateTime(maxTimeInfo);
                t.Apply(this);
            }

            if (flushType == null)
                ClearTransformations();
            else
                Transforms.RemoveAll(t => t.GetType() == flushType);
        }

        public virtual Drawable DelayReset()
        {
            Delay(-transformationDelay);
            return this;
        }

        public void Loop(int delay = 0)
        {
            foreach (var t in Transforms)
                t.Loop(Math.Max(0, transformationDelay + delay - t.Duration));
        }

        /// <summary>
        /// Make this drawable automatically clean itself up after all transformations have finished playing.
        /// Can be delayed using Delay().
        /// </summary>
        public void Expire(bool calculateLifetimeStart = false)
        {
            if (clock == null)
            {
                LifetimeEnd = double.MinValue;
                return;
            }

            //expiry should happen either at the end of the last transformation or using the current sequence delay (whichever is highest).
            double max = Time.Current + transformationDelay;
            foreach (ITransform t in Transforms)
                if (t.EndTime > max) max = t.EndTime + 1; //adding 1ms here ensures we can expire on the current frame without issue.
            LifetimeEnd = max;

            if (calculateLifetimeStart)
            {
                double min = double.MaxValue;
                foreach (ITransform t in Transforms)
                    if (t.StartTime < min) min = t.StartTime;
                LifetimeStart = min < int.MaxValue ? min : int.MinValue;
            }
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

        public void FadeIn(double duration = 0, EasingTypes easing = EasingTypes.None)
        {
            FadeTo(1, duration, easing);
        }

        public void FadeInFromZero(double duration)
        {
            if (transformationDelay == 0)
            {
                Alpha = 0;
                Transforms.RemoveAll(t => t is TransformAlpha);
            }

            double startTime = Time.Current + transformationDelay;

            TransformAlpha tr = new TransformAlpha
            {
                StartTime = startTime,
                EndTime = startTime + duration,
                StartValue = 0,
                EndValue = 1,
            };

            Transforms.Add(tr);
        }

        public void FadeOut(double duration = 0, EasingTypes easing = EasingTypes.None)
        {
            FadeTo(0, duration, easing);
        }

        public void FadeOutFromOne(double duration)
        {
            if (transformationDelay == 0)
            {
                Alpha = 1;
                Transforms.RemoveAll(t => t is TransformAlpha);
            }

            double startTime = Time.Current + transformationDelay;

            TransformAlpha tr = new TransformAlpha
            {
                StartTime = startTime,
                EndTime = startTime + duration,
                StartValue = 1,
                EndValue = 0,
            };

            Transforms.Add(tr);
        }

        #region Float-based helpers

        protected void TransformFloatTo(float startValue, float newValue, double duration, EasingTypes easing, TransformFloat transform)
        {
            Type type = transform.GetType();
            if (transformationDelay == 0)
            {
                Transforms.RemoveAll(t => t.GetType() == type);
                if (startValue == newValue)
                    return;
            }
            else
                startValue = (Transforms.FindLast(t => t.GetType() == type) as TransformFloat)?.EndValue ?? startValue;

            double startTime = Clock != null ? (Time.Current + transformationDelay) : 0;

            transform.StartTime = startTime;
            transform.EndTime = startTime + duration;
            transform.StartValue = startValue;
            transform.EndValue = newValue;
            transform.Easing = easing;

            if (Clock == null)
            {
                transform.UpdateTime(new FrameTimeInfo { Current = transform.EndTime });
                transform.Apply(this);
            }
            else if (duration == 0 && transformationDelay == 0)
            {
                transform.UpdateTime(Time);
                transform.Apply(this);
            }
            else
            {
                Transforms.Add(transform);
            }
        }

        public void FadeTo(float newAlpha, double duration = 0, EasingTypes easing = EasingTypes.None)
        {
            UpdateTransformsOfType(typeof(TransformAlpha));
            TransformFloatTo(Alpha, newAlpha, duration, easing, new TransformAlpha());
        }

        public void RotateTo(float newRotation, double duration = 0, EasingTypes easing = EasingTypes.None)
        {
            UpdateTransformsOfType(typeof(TransformRotation));
            TransformFloatTo(Rotation, newRotation, duration, easing, new TransformRotation());
        }

        public void MoveToX(float destination, double duration = 0, EasingTypes easing = EasingTypes.None)
        {
            UpdateTransformsOfType(typeof(TransformPositionX));
            TransformFloatTo(Position.X, destination, duration, easing, new TransformPositionX());
        }

        public void MoveToY(float destination, double duration = 0, EasingTypes easing = EasingTypes.None)
        {
            UpdateTransformsOfType(typeof(TransformPositionY));
            TransformFloatTo(Position.Y, destination, duration, easing, new TransformPositionY());
        }

        #endregion

        #region Vector2-based helpers

        protected void TransformVectorTo(Vector2 startValue, Vector2 newValue, double duration, EasingTypes easing, TransformVector transform)
        {
            Type type = transform.GetType();
            if (transformationDelay == 0)
            {
                Transforms.RemoveAll(t => t.GetType() == type);

                if (startValue == newValue)
                    return;
            }
            else
                startValue = (Transforms.FindLast(t => t.GetType() == type) as TransformVector)?.EndValue ?? startValue;

            double startTime = Clock != null ? (Time.Current + transformationDelay) : 0;

            transform.StartTime = startTime;
            transform.EndTime = startTime + duration;
            transform.StartValue = startValue;
            transform.EndValue = newValue;
            transform.Easing = easing;

            if (Clock == null)
            {
                transform.UpdateTime(new FrameTimeInfo { Current = transform.EndTime });
                transform.Apply(this);
            }
            else if (duration == 0 && transformationDelay == 0)
            {
                transform.UpdateTime(Time);
                transform.Apply(this);
            }
            else
            {
                Transforms.Add(transform);
            }
        }

        public void ScaleTo(float newScale, double duration = 0, EasingTypes easing = EasingTypes.None)
        {
            UpdateTransformsOfType(typeof(TransformScale));
            TransformVectorTo(Scale, new Vector2(newScale), duration, easing, new TransformScale());
        }

        public void ScaleTo(Vector2 newScale, double duration = 0, EasingTypes easing = EasingTypes.None)
        {
            UpdateTransformsOfType(typeof(TransformScale));
            TransformVectorTo(Scale, newScale, duration, easing, new TransformScale());
        }

        public void ResizeTo(float newSize, double duration = 0, EasingTypes easing = EasingTypes.None)
        {
            UpdateTransformsOfType(typeof(TransformSize));
            TransformVectorTo(Size, new Vector2(newSize), duration, easing, new TransformSize());
        }

        public void ResizeTo(Vector2 newSize, double duration = 0, EasingTypes easing = EasingTypes.None)
        {
            UpdateTransformsOfType(typeof(TransformSize));
            TransformVectorTo(Size, newSize, duration, easing, new TransformSize());
        }

        public void MoveTo(Vector2 newPosition, double duration = 0, EasingTypes easing = EasingTypes.None)
        {
            UpdateTransformsOfType(typeof(TransformPosition));
            TransformVectorTo(Position, newPosition, duration, easing, new TransformPosition());
        }

        public void MoveToOffset(Vector2 offset, int duration = 0, EasingTypes easing = EasingTypes.None)
        {
            UpdateTransformsOfType(typeof(TransformPosition));
            MoveTo((Transforms.FindLast(t => t is TransformPosition) as TransformPosition)?.EndValue ?? Position + offset, duration, easing);
        }

        #endregion

        #region Color4-based helpers

        public void FadeColour(SRGBColour newColour, int duration = 0, EasingTypes easing = EasingTypes.None)
        {
            UpdateTransformsOfType(typeof(TransformColour));

            Color4 startValue = Colour.Linear;
            if (transformationDelay == 0)
            {
                Transforms.RemoveAll(t => t is TransformColour);

                if (startValue == newColour)
                    return;
            }
            else
                startValue = (Transforms.FindLast(t => t is TransformColour) as TransformColour)?.EndValue ?? startValue;

            double startTime = Clock != null ? (Time.Current + transformationDelay) : 0;

            TransformColour transform = new TransformColour
            {
                StartTime = startTime,
                EndTime = startTime + duration,
                StartValue = startValue,
                EndValue = newColour.Linear,
                Easing = easing
            };

            if (Clock == null)
            {
                transform.UpdateTime(new FrameTimeInfo { Current = transform.EndTime });
                transform.Apply(this);
            }
            else if (duration == 0 && transformationDelay == 0)
            {
                transform.UpdateTime(Time);
                transform.Apply(this);
            }
            else
            {
                Transforms.Add(transform);
            }
        }

        public void FlashColour(SRGBColour flashColour, int duration, EasingTypes easing = EasingTypes.None)
        {
            Debug.Assert(transformationDelay == 0, @"FlashColour doesn't support Delay() currently");

            Color4 startValue = (Transforms.FindLast(t => t is TransformColour) as TransformColour)?.EndValue ?? Colour.Linear;
            Transforms.RemoveAll(t => t is TransformColour);

            double startTime = Clock != null ? (Time.Current + transformationDelay) : 0;

            TransformColour transform = new TransformColour
            {
                StartTime = startTime,
                EndTime = startTime + duration,
                StartValue = flashColour.Linear,
                EndValue = startValue,
                Easing = easing
            };

            if (Clock == null)
            {
                transform.UpdateTime(new FrameTimeInfo { Current = transform.EndTime });
                transform.Apply(this);
            }
            else if (duration == 0 && transformationDelay == 0)
            {
                transform.UpdateTime(Time);
                transform.Apply(this);
            }
            else
            {
                Transforms.Add(transform);
            }
        }

        #endregion
    }
}
