// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Framework.Caching;

namespace osu.Framework.Graphics.Animations
{
    /// <summary>
    /// Represents a generic, frame-based animation. Inherit this class if you need custom animations.
    /// </summary>
    /// <typeparam name="T">The type of content in the frames of the animation.</typeparam>
    public abstract class Animation<T> : AnimationClockComposite, IFramedAnimation
    {
        /// <summary>
        /// The duration in milliseconds of a newly added frame, if no duration is explicitly specified when adding the frame.
        /// Defaults to 60fps.
        /// </summary>
        public double DefaultFrameLength = 1000.0 / 60.0;

        private readonly List<FrameData<T>> frameData;

        /// <summary>
        /// The number of frames this animation has.
        /// </summary>
        public int FrameCount => frameData.Count;

        public int CurrentFrameIndex { get; private set; }

        public T CurrentFrame => frameData[CurrentFrameIndex].Content;

        private readonly Cached currentFrameCache = new Cached();

        /// <summary>
        /// Construct a new animation which loops by default.
        /// </summary>
        /// <param name="startAtCurrentTime">Whether the current clock time should be assumed as the 0th animation frame.</param>
        protected Animation(bool startAtCurrentTime = true)
            : base(startAtCurrentTime)
        {
            frameData = new List<FrameData<T>>();
            Loop = true;
        }

        /// <summary>
        /// Displays the frame with the given zero-based frame index.
        /// </summary>
        /// <param name="frameIndex">The zero-based index of the frame to display.</param>
        public void GotoFrame(int frameIndex)
        {
            Seek(frameData[Math.Clamp(frameIndex, 0, frameData.Count)].DisplayStartTime);
        }

        /// <summary>
        /// Adds a new frame with the given content and display duration (in milliseconds) to this animation.
        /// </summary>
        /// <param name="content">The content of the new frame.</param>
        /// <param name="displayDuration">The duration the new frame should be displayed for.</param>
        public void AddFrame(T content, double? displayDuration = null)
        {
            AddFrame(new FrameData<T>
            {
                Duration = displayDuration ?? DefaultFrameLength, // 60 fps by default
                Content = content,
            });
        }

        public void AddFrame(FrameData<T> frame)
        {
            var lastFrame = frameData.LastOrDefault();

            frame.DisplayStartTime = lastFrame.DisplayEndTime;
            Duration += frame.Duration;

            frameData.Add(frame);

            OnFrameAdded(frame.Content, frame.Duration);

            // This handles the case when the first frame is added, especially important after a `ClearFrames` operation.
            if (frameData.Count == 1)
                currentFrameCache.Invalidate();
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

        /// <summary>
        /// Removes all frames from this animation.
        /// </summary>
        public void ClearFrames()
        {
            frameData.Clear();

            Duration = 0;
            CurrentFrameIndex = 0;

            ClearDisplay();
        }

        /// <summary>
        /// Displays the given contents.
        /// </summary>
        /// <remarks>This method will only be called after <see cref="OnFrameAdded(T, double)"/> has been called at least once.</remarks>
        /// <param name="content">The content that will be displayed.</param>
        protected abstract void DisplayFrame(T content);

        /// <summary>
        /// Clear the currently displayed frame.
        /// </summary>
        /// <remarks>Called when resetting the animation, ie. on <see cref="ClearFrames"/>.</remarks>
        protected abstract void ClearDisplay();

        /// <summary>
        /// Called whenever a new frame was added to this animation.
        /// </summary>
        /// <param name="content">The content of the new frame.</param>
        /// <param name="displayDuration">The display duration of the new frame.</param>
        protected virtual void OnFrameAdded(T content, double displayDuration)
        {
        }

        protected override void Update()
        {
            base.Update();

            if (frameData.Count == 0) return;

            updateFrameIndex();

            if (!currentFrameCache.IsValid)
                updateCurrentFrame();
        }

        private void updateFrameIndex()
        {
            switch (PlaybackPosition.CompareTo(frameData[CurrentFrameIndex].DisplayStartTime))
            {
                case -1:
                    while (CurrentFrameIndex > 0 && PlaybackPosition < frameData[CurrentFrameIndex].DisplayStartTime)
                    {
                        CurrentFrameIndex--;
                        currentFrameCache.Invalidate();
                    }

                    break;

                case 1:
                    while (CurrentFrameIndex < frameData.Count - 1 && PlaybackPosition >= frameData[CurrentFrameIndex].DisplayEndTime)
                    {
                        CurrentFrameIndex++;
                        currentFrameCache.Invalidate();
                    }

                    break;
            }
        }

        private void updateCurrentFrame()
        {
            DisplayFrame(CurrentFrame);

            UpdateSizing();

            currentFrameCache.Validate();
        }
    }
}
