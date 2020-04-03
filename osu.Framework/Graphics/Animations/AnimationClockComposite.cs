// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Allocation;
using osu.Framework.Graphics.Containers;
using osu.Framework.Timing;

namespace osu.Framework.Graphics.Animations
{
    public abstract class AnimationClockComposite : CompositeDrawable, IAnimation
    {
        private readonly bool startAtCurrentTime;

        private ManualClock manualClock;

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
                Clock = new FramedClock(manualClock = new ManualClock()),
                Child = CreateContent()
            });
        }

        public override IFrameBasedClock Clock
        {
            get => base.Clock;
            set
            {
                base.Clock = value;

                manualClock.CurrentTime = !startAtCurrentTime ? Clock.CurrentTime : 0;
            }
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            if (!startAtCurrentTime)
                manualClock.CurrentTime = Clock.CurrentTime;
        }

        protected internal override void AddInternal(Drawable drawable) => throw new InvalidOperationException($"Use {nameof(CreateContent)} instead.");

        /// <summary>
        /// Create the content container for animation display.
        /// </summary>
        /// <returns></returns>
        public abstract Drawable CreateContent();

        protected override void Update()
        {
            base.Update();

            if (IsPlaying)
                manualClock.CurrentTime += Time.Elapsed;
        }

        /// <summary>
        /// The current playback position of the animation, in milliseconds.
        /// </summary>
        public double PlaybackPosition
        {
            get
            {
                if (Loop)
                    return manualClock.CurrentTime % Duration;

                return Math.Min(manualClock.CurrentTime, Duration);
            }
        }

        public double Duration { get; protected set; }

        public bool IsPlaying { get; set; } = true;

        public bool Loop { get; set; }

        public void Seek(double time) => manualClock.CurrentTime = time;
    }
}
