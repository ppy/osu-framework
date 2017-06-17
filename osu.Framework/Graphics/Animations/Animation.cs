// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Graphics.Containers;
using System;
using System.Collections.Generic;

namespace osu.Framework.Graphics.Animations
{
    /// <summary>
    /// Represents a generic, frame-based animation. Inherit this class if you need custom animations.
    /// </summary>
    /// <typeparam name="T">The type of content in the frames of the animation.</typeparam>
    public abstract class Animation<T> : Container, IAnimation
    {
        private struct FrameData
        {
            public T Content { get; set; }
            public double Duration { get; set; }
        }

        private readonly List<FrameData> frameData;
        private int currentFrameIndex;

        private double currentFrameTime;

        /// <summary>
        /// The number of frames this animation has.
        /// </summary>
        public int FrameCount => frameData.Count;

        /// <summary>
        /// True if the animation is playing, false otherwise.
        /// </summary>
        public bool IsPlaying { get; set; }
        /// <summary>
        /// True if the animation should start over from the first frame after finishing. False if it should stop playing and keep displaying the last frame when finishing.
        /// </summary>
        public bool Repeat { get; set; }

        protected Animation()
        {
            AutoSizeAxes = Axes.Both;

            frameData = new List<FrameData>();
            IsPlaying = true;
            Repeat = true;
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

            currentFrameIndex = frameIndex;
            displayFrame(currentFrameIndex);
        }
        /// <summary>
        /// Displays the frame with the given zero-based frame index and stops the animation at that frame.
        /// </summary>
        /// <param name="frameIndex">The zero-based index of the frame to display.</param>
        public void GotoAndStop(int frameIndex)
        {
            GotoFrame(frameIndex);
            IsPlaying = false;
        }
        /// <summary>
        /// Displays the frame with the given zero-based frame index amd plays the animation from that frame.
        /// </summary>
        /// <param name="frameIndex">The zero-based index of the frame to display.</param>
        public void GotoAndPlay(int frameIndex)
        {
            GotoFrame(frameIndex);
            IsPlaying = true;
        }

        /// <summary>
        /// Adds a new frame with the given content and display duration (in milliseconds) to this animation.
        /// </summary>
        /// <param name="content">The content of the new frame.</param>
        /// <param name="displayDuration">The duration the new frame should be displayed for.</param>
        public void AddFrame(T content, double displayDuration = 1000.0 / 60.0)
        {
            var fd = new FrameData()
            {
                Duration = displayDuration, // 60 fps by default
                Content = content
            };
            frameData.Add(fd);
            AddedFrame(content, displayDuration);

            if (frameData.Count == 1)
                displayFrame(0);
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
        public void AddFrames(IEnumerable<KeyValuePair<T, double>> frames)
        {
            foreach (var t in frames)
                AddFrame(t.Key, t.Value);
        }

        private void displayFrame(int index) => DisplayFrame(frameData[index].Content);

        /// <summary>
        /// Displays the given contents.
        /// </summary>
        /// <remarks>This method will only be called after <see cref="AddedFrame(T, double)"/> has been called at least once.</remarks>
        /// <param name="content">The content that will be displayed.</param>
        protected abstract void DisplayFrame(T content);
        /// <summary>
        /// Called whenever a new frame was added to this animation.
        /// </summary>
        /// <param name="content">The content of the new frame.</param>
        /// <param name="displayDuration">The display duration of the new frame.</param>
        protected virtual void AddedFrame(T content, double displayDuration) { }

        protected override void Update()
        {
            base.Update();

            if (IsPlaying && frameData.Count > 0)
            {
                currentFrameTime += Time.Elapsed;
                while(currentFrameTime > frameData[currentFrameIndex].Duration)
                {
                    currentFrameTime -= frameData[currentFrameIndex].Duration;
                    ++currentFrameIndex;
                    if (currentFrameIndex >= frameData.Count)
                    {
                        if (Repeat)
                        {
                            currentFrameIndex = 0;
                        }
                        else
                        {
                            currentFrameIndex = frameData.Count - 1;
                            IsPlaying = false;
                            break;
                        }
                    }
                }
                displayFrame(currentFrameIndex);
            }
        }

        protected override void Dispose(bool isDisposing)
        {
            base.Dispose(isDisposing);

            foreach (var frame in frameData)
                (frame.Content as IDisposable)?.Dispose();

            IsPlaying = false;
            Repeat = false;
            frameData.Clear();
            currentFrameIndex = 0;
            currentFrameTime = 0;
        }
    }
}
