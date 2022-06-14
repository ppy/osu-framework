// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using osu.Framework.Allocation;
using osu.Framework.Graphics.Containers;
using osu.Framework.Timing;

namespace osu.Framework.Graphics.Animations
{
    public abstract class AnimationClockComposite : CustomisableSizeCompositeDrawable, IAnimation
    {
        private readonly bool startAtCurrentTime;

        private bool hasSeeked;

        private readonly ManualClock manualClock = new ManualClock();

        /// <summary>
        /// Construct a new animation.
        /// </summary>
        /// <param name="startAtCurrentTime">Whether the current clock time should be assumed as the 0th animation frame.</param>
        protected AnimationClockComposite(bool startAtCurrentTime = true)
        {
            this.startAtCurrentTime = startAtCurrentTime;
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            base.AddInternal(new Container
            {
                RelativeSizeAxes = Axes.Both,
                Clock = new FramedClock(manualClock),
                Child = CreateContent()
            });
        }

        public override IFrameBasedClock Clock
        {
            get => base.Clock;
            set
            {
                base.Clock = value;
                consumeClockTime();
            }
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            // always consume to zero out elapsed for update loop.
            double elapsed = consumeClockTime();

            if (!startAtCurrentTime && !hasSeeked)
                manualClock.CurrentTime += elapsed;
        }

        protected internal override void AddInternal(Drawable drawable) => throw new InvalidOperationException($"Use {nameof(CreateContent)} instead.");

        /// <summary>
        /// Create the content container for animation display.
        /// </summary>
        /// <returns>The container providing the content to be added into this <see cref="AnimationClockComposite"/>'s hierarchy.</returns>
        public abstract Drawable CreateContent();

        private double lastConsumedTime;

        protected override void Update()
        {
            base.Update();

            double consumedTime = consumeClockTime();
            if (IsPlaying)
                manualClock.CurrentTime += consumedTime;
        }

        /// <summary>
        /// The current playback position of the animation, in milliseconds.
        /// </summary>
        public double PlaybackPosition
        {
            get
            {
                double current = manualClock.CurrentTime;

                if (Loop) current %= Duration;

                return Math.Clamp(current, 0, Duration);
            }
            set
            {
                hasSeeked = true;
                manualClock.CurrentTime = value;

                // consume current clock to avoid additional jumps on top of the seek due to time naturally elapsing.
                // there's no need to do this before we're loaded - LoadComplete() will also consume clock initially,
                // and Time might not even be initialised yet during load
                if (IsLoaded)
                    consumeClockTime();
            }
        }

        public double Duration { get; protected set; }

        public bool IsPlaying { get; set; } = true;

        public virtual bool Loop { get; set; }

        public void Seek(double time) => PlaybackPosition = time;

        private double consumeClockTime()
        {
            double elapsed = Time.Current - lastConsumedTime;
            lastConsumedTime = Time.Current;
            return elapsed;
        }
    }
}
