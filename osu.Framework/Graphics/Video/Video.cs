// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using osu.Framework.Allocation;
using System;
using System.Collections.Generic;
using System.IO;
using JetBrains.Annotations;
using osu.Framework.Configuration;
using osu.Framework.Graphics.Animations;
using osu.Framework.Logging;
using osu.Framework.Platform;
using osuTK;

namespace osu.Framework.Graphics.Video
{
    /// <summary>
    /// Represents a composite that displays a video played back from a stream or a file.
    /// </summary>
    public class Video : AnimationClockComposite
    {
        /// <summary>
        /// Whether this video is in a buffering state, waiting on decoder or underlying stream.
        /// </summary>
        public bool Buffering { get; private set; }

        /// <summary>
        /// True if the video should loop after finishing its playback, false otherwise.
        /// </summary>
        public override bool Loop
        {
            get => base.Loop;
            set
            {
                if (decoder != null)
                    decoder.Looping = value;

                base.Loop = value;
            }
        }

        /// <summary>
        /// Whether the video decoding process has faulted.
        /// </summary>
        public bool IsFaulted => decoder?.IsFaulted ?? false;

        /// <summary>
        /// The current state of the decoding process.
        /// </summary>
        public VideoDecoder.DecoderState State => decoder?.State ?? VideoDecoder.DecoderState.Ready;

        internal double CurrentFrameTime => lastFrame?.Time ?? 0;

        internal int AvailableFrames => availableFrames.Count;

        private VideoDecoder decoder;

        private readonly Stream stream;

        private readonly Queue<DecodedFrame> availableFrames = new Queue<DecodedFrame>();

        private DecodedFrame lastFrame;
        private bool lastFrameShown;

        /// <summary>
        /// The total number of frames processed by this instance.
        /// </summary>
        public int FramesProcessed { get; private set; }

        /// <summary>
        /// The length in milliseconds that the decoder can be out of sync before a seek is automatically performed.
        /// </summary>
        private const float lenience_before_seek = 2500;

        private bool isDisposed;

        internal VideoSprite Sprite;

        /// <summary>
        /// YUV->RGB conversion matrix based on the video colorspace
        /// </summary>
        public Matrix3 ConversionMatrix => decoder.GetConversionMatrix();

        /// <summary>
        /// Creates a new <see cref="Video"/>.
        /// </summary>
        /// <param name="filename">The video file.</param>
        /// <param name="startAtCurrentTime">Whether the current clock time should be assumed as the 0th video frame.</param>
        public Video(string filename, bool startAtCurrentTime = true)
            : this(File.OpenRead(filename), startAtCurrentTime)
        {
        }

        public override Drawable CreateContent() => Sprite = new VideoSprite(this) { RelativeSizeAxes = Axes.Both };

        /// <summary>
        /// Creates a new <see cref="Video"/>.
        /// </summary>
        /// <param name="stream">The video file stream.</param>
        /// <param name="startAtCurrentTime">Whether the current clock time should be assumed as the 0th video frame.</param>
        public Video([NotNull] Stream stream, bool startAtCurrentTime = true)
            : base(startAtCurrentTime)
        {
            this.stream = stream ?? throw new ArgumentNullException(nameof(stream));
        }

        [BackgroundDependencyLoader]
        private void load(GameHost gameHost, FrameworkConfigManager config)
        {
            decoder = gameHost.CreateVideoDecoder(stream);
            decoder.Looping = Loop;

            config.BindWith(FrameworkSetting.HardwareVideoDecoder, decoder.TargetHardwareVideoDecoders);

            decoder.StartDecoding();

            Duration = decoder.Duration;
        }

        protected override void Update()
        {
            base.Update();

            if (decoder.State == VideoDecoder.DecoderState.EndOfStream && availableFrames.Count == 0)
            {
                // if at the end of the stream but our playback enters a valid time region again, a seek operation is required to get the decoder back on track.
                if (PlaybackPosition < decoder.LastDecodedFrameTime)
                {
                    seekIntoSync();
                }
            }

            var peekFrame = availableFrames.Count > 0 ? availableFrames.Peek() : null;
            bool outOfSync = false;

            if (peekFrame != null)
            {
                outOfSync = Math.Abs(PlaybackPosition - peekFrame.Time) > lenience_before_seek;

                if (Loop)
                {
                    // handle looping bounds (as we could be in the roll-over process between loops).
                    outOfSync &= Math.Abs(PlaybackPosition - decoder.Duration - peekFrame.Time) > lenience_before_seek &&
                                 Math.Abs(PlaybackPosition + decoder.Duration - peekFrame.Time) > lenience_before_seek;
                }
            }

            // we are too far ahead or too far behind
            if (outOfSync && decoder.CanSeek)
            {
                Logger.Log($"Video too far out of sync ({peekFrame.Time}), seeking to {PlaybackPosition}");
                seekIntoSync();
            }

            double frameTime = CurrentFrameTime;

            while (availableFrames.Count > 0 && checkNextFrameValid(availableFrames.Peek()))
            {
                if (lastFrame != null) decoder.ReturnFrames(new[] { lastFrame });
                lastFrame = availableFrames.Dequeue();
                lastFrameShown = false;
            }

            if (!lastFrameShown && lastFrame != null)
            {
                var tex = lastFrame.Texture;

                // Check if the new frame has been uploaded so we don't display an old frame
                if (tex?.UploadComplete ?? false)
                {
                    Sprite.Texture = tex;
                    UpdateSizing();
                    lastFrameShown = true;
                }
            }

            if (availableFrames.Count == 0)
            {
                foreach (var f in decoder.GetDecodedFrames())
                    availableFrames.Enqueue(f);
            }

            Buffering = decoder.IsRunning && availableFrames.Count == 0;

            if (frameTime != CurrentFrameTime)
                FramesProcessed++;

            void seekIntoSync()
            {
                decoder.Seek(PlaybackPosition);
                decoder.ReturnFrames(availableFrames);
                availableFrames.Clear();
            }
        }

        private bool checkNextFrameValid(DecodedFrame frame)
        {
            // in the case of looping, we may start a seek back to the beginning but still receive some lingering frames from the end of the last loop. these should be allowed to continue playing.
            if (Loop && Math.Abs((frame.Time - Duration) - PlaybackPosition) < lenience_before_seek)
                return true;

            return frame.Time <= PlaybackPosition && Math.Abs(frame.Time - PlaybackPosition) < lenience_before_seek;
        }

        protected override void Dispose(bool isDisposing)
        {
            if (isDisposed)
                return;

            base.Dispose(isDisposing);

            isDisposed = true;

            if (decoder != null)
            {
                decoder.ReturnFrames(availableFrames);
                availableFrames.Clear();

                decoder.Dispose();
            }
            else
            {
                foreach (var f in availableFrames)
                    f.Texture.Dispose();
            }
        }

        protected override float GetFillAspectRatio() => Sprite.FillAspectRatio;

        protected override Vector2 GetCurrentDisplaySize() =>
            new Vector2(Sprite.Texture?.DisplayWidth ?? 0, Sprite.Texture?.DisplayHeight ?? 0);
    }
}
