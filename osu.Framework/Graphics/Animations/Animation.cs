// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics.Containers;
using System.Collections.Generic;
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
        /// </summary>
        public double DefaultFrameLength = 1000.0 / 60.0;

        private readonly List<FrameData<T>> frameData;
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
            frameData = new List<FrameData<T>>();
            IsPlaying = true;
            Repeat = true;
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

            currentFrameIndex = frameIndex;
            displayFrame(currentFrameIndex);
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
                Content = content
            });
        }

        public void AddFrame(FrameData<T> frame)
        {
            frameData.Add(frame);
            OnFrameAdded(frame.Content, frame.Duration);

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
        public void AddFrames(IEnumerable<FrameData<T>> frames)
        {
            foreach (var t in frames)
                AddFrame(t.Content, t.Duration);
        }

        private void displayFrame(int index)
        {
            var frame = frameData[index];

            if (RelativeSizeAxes != Axes.Both)
            {
                var frameSize = GetFrameSize(frame.Content);

                if ((RelativeSizeAxes & Axes.X) == 0 && !hasCustomWidth)
                    base.Width = frameSize.X;

                if ((RelativeSizeAxes & Axes.Y) == 0 && !hasCustomHeight)
                    base.Height = frameSize.Y;
            }

            DisplayFrame(frameData[index].Content);
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

            if (IsPlaying && frameData.Count > 0)
            {
                currentFrameTime += Time.Elapsed;

                while (currentFrameTime > frameData[currentFrameIndex].Duration)
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
    }
}
