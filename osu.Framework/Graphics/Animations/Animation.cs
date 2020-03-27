// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Graphics.Containers;
using System.Collections.Generic;
using System.Linq;
using osu.Framework.Timing;
using osuTK;

namespace osu.Framework.Graphics.Animations
{
    /// <summary>
    /// Represents a generic, frame-based animation. Inherit this class if you need custom animations.
    /// </summary>
    /// <typeparam name="T">The type of content in the frames of the animation.</typeparam>
    public abstract class Animation<T> : CompositeDrawable, IAnimation
    {
        /// <summary>
        /// The duration in milliseconds of a newly added frame, if no duration is explicitly specified when adding the frame.
        /// Defaults to 60fps.
        /// </summary>
        public double DefaultFrameLength = 1000.0 / 60.0;

        /// <summary>
        /// The current playback position of the animation, in milliseconds.
        /// </summary>
        public double PlaybackPosition
        {
            get
            {
                if (Repeat)
                    return Clock.CurrentTime % Duration;

                return Math.Min(Clock.CurrentTime, Duration);
            }
        }

        public double Duration { get; private set; }

        private readonly List<FrameData<T>> frameData;

        private readonly bool startAtCurrentTime;

        /// <summary>
        /// The number of frames this animation has.
        /// </summary>
        public int FrameCount => frameData.Count;

        public int CurrentFrameIndex { get; private set; }

        /// <summary>
        /// True if the animation is playing, false otherwise.
        /// </summary>
        public bool IsPlaying { get; set; }

        /// <summary>
        /// True if the animation should start over from the first frame after finishing. False if it should stop playing and keep displaying the last frame when finishing.
        /// </summary>
        public bool Repeat { get; set; }

        public T CurrentFrame => frameData[CurrentFrameIndex].Content;

        /// <summary>
        /// Construct a new animation.
        /// </summary>
        /// <param name="startAtCurrentTime">Whether the current clock time should be assumed as the 0th video frame.</param>
        protected Animation(bool startAtCurrentTime = true)
        {
            this.startAtCurrentTime = startAtCurrentTime;

            frameData = new List<FrameData<T>>();
            IsPlaying = true;
            Repeat = true;
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            if (startAtCurrentTime)
                base.Clock = new FramedOffsetClock(Clock) { Offset = Clock.CurrentTime };
        }

        public override IFrameBasedClock Clock
        {
            get => base.Clock;
            set
            {
                if (startAtCurrentTime)
                    throw new InvalidOperationException($"A {nameof(Animation<T>)} with {startAtCurrentTime} = true cannot receive a custom {nameof(Clock)}.");

                base.Clock = value;
            }
        }

        private bool hasCustomWidth;

        public override float Width
        {
            set
            {
                base.Width = value;
                hasCustomWidth = true;
            }
        }

        private bool hasCustomHeight;

        public override float Height
        {
            set
            {
                base.Height = value;
                hasCustomHeight = true;
            }
        }

        public override Vector2 Size
        {
            set
            {
                Width = value.X;
                Height = value.Y;
            }
        }

        /// <summary>
        /// Displays the frame with the given zero-based frame index.
        /// </summary>
        /// <param name="frameIndex">The zero-based index of the frame to display.</param>
        public void GotoFrame(int frameIndex)
        {
            if (frameIndex < 0)
                frameIndex = 0;
            else if (frameIndex >= frameData.Count)
                frameIndex = frameData.Count - 1;

            CurrentFrameIndex = frameIndex;
            updateCurrentFrame();
        }

        /// <summary>
        /// Adds a new frame with the given content and display duration (in milliseconds) to this animation.
        /// </summary>
        /// <param name="content">The content of the new frame.</param>
        /// <param name="displayDuration">The duration the new frame should be displayed for.</param>
        public void AddFrame(T content, double? displayDuration = null)
        {
            var lastFrame = frameData.LastOrDefault();

            var frame = new FrameData<T>
            {
                Duration = displayDuration ?? DefaultFrameLength, // 60 fps by default
                Content = content,
                Position = lastFrame.Position + lastFrame.Duration
            };

            AddFrame(frame);

            Duration += frame.Duration;
        }

        public void AddFrame(FrameData<T> frame)
        {
            frameData.Add(frame);
            OnFrameAdded(frame.Content, frame.Duration);

            if (frameData.Count == 1)
                updateCurrentFrame();
        }

        /// <summary>
        /// Adds a new frame for each element in the given enumerable. Every frame will be displayed for 1/60th of a second.
        /// </summary>
        /// <param name="contents">The contents to use for creating new frames.</param>
        public void AddFrames(IEnumerable<T> contents)
        {
            foreach (var t in contents)
                AddFrame(t);
        }

        /// <summary>
        /// Adds a new frame for each element in the given enumerable. Every frame will be displayed for the given number of milliseconds.
        /// </summary>
        /// <param name="frames">The contents and display durations to use for creating new frames.</param>
        public void AddFrames(IEnumerable<FrameData<T>> frames)
        {
            foreach (var t in frames)
                AddFrame(t.Content, t.Duration);
        }

        private void updateCurrentFrame()
        {
            var frame = CurrentFrame;

            if (RelativeSizeAxes != Axes.Both)
            {
                var frameSize = GetFrameSize(frame);

                if ((RelativeSizeAxes & Axes.X) == 0 && !hasCustomWidth)
                    base.Width = frameSize.X;

                if ((RelativeSizeAxes & Axes.Y) == 0 && !hasCustomHeight)
                    base.Height = frameSize.Y;
            }

            DisplayFrame(frame);
        }

        /// <summary>
        /// Displays the given contents.
        /// </summary>
        /// <remarks>This method will only be called after <see cref="OnFrameAdded(T, double)"/> has been called at least once.</remarks>
        /// <param name="content">The content that will be displayed.</param>
        protected abstract void DisplayFrame(T content);

        /// <summary>
        /// Called whenever a new frame was added to this animation.
        /// </summary>
        /// <param name="content">The content of the new frame.</param>
        /// <param name="displayDuration">The display duration of the new frame.</param>
        protected virtual void OnFrameAdded(T content, double displayDuration)
        {
        }

        /// <summary>
        /// Retrieves the size of a given frame.
        /// </summary>
        /// <param name="content">The frame to retrieve the size of.</param>
        /// <returns>The size of <paramref name="content"/>.</returns>
        protected abstract Vector2 GetFrameSize(T content);

        protected override void Update()
        {
            base.Update();

            if (!IsPlaying || frameData.Count <= 0) return;

            while (CurrentFrameIndex < frameData.Count && PlaybackPosition > frameData[CurrentFrameIndex].Position + frameData[CurrentFrameIndex].Duration)
                CurrentFrameIndex++;

            while (CurrentFrameIndex > 0 && PlaybackPosition < frameData[CurrentFrameIndex].Position)
                CurrentFrameIndex--;

            updateCurrentFrame();
        }
    }
}
