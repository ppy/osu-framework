// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Graphics.Sprites;
using System;
using System.Collections.Generic;
using System.IO;
using JetBrains.Annotations;
using osu.Framework.Bindables;
using osu.Framework.Logging;
using osu.Framework.Platform;

namespace osu.Framework.Graphics.Video
{
    /// <summary>
    /// Represents a sprite that plays back a video from a stream or a file.
    /// </summary>
    public class VideoSprite : Sprite
    {
        /// <summary>
        /// The duration of the video that is being played. Can only be queried after the decoder has started decoding has loaded. This value may be an estimate by FFmpeg, depending on the video loaded.
        /// </summary>
        public double Duration => decoder?.Duration ?? 0;

        /// <summary>
        /// True if the video has finished playing, false otherwise.
        /// </summary>
        public bool FinishedPlaying => !Loop && PlaybackPosition > Duration;

        /// <summary>
        /// Whether this video is in a buffering state, waiting on decoder or underlying stream.
        /// </summary>
        public bool Buffering { get; private set; }

        /// <summary>
        /// True if the video should loop after finishing its playback, false otherwise.
        /// </summary>
        public bool Loop
        {
            get => loop;
            set
            {
                if (decoder != null)
                    decoder.Looping = value;

                loop = value;
            }
        }

        private bool loop;

        /// <summary>
        /// The current position of the video playback. The playback position is automatically calculated based on the clock of the VideoSprite.
        /// To perform seeks, change the <see cref="Timing.IClock.CurrentTime"/> of the clock of this <see cref="VideoSprite"/>.
        /// </summary>
        public double PlaybackPosition
        {
            get
            {
                if (!startTime.HasValue)
                    return 0;

                if (Loop) return (Clock.CurrentTime - startTime.Value) % Duration;

                return Math.Min(Clock.CurrentTime - startTime.Value, Duration);
            }
        }

        /// <summary>
        /// True if this VideoSprites decoding process has faulted.
        /// </summary>
        public bool IsFaulted => decoder?.IsFaulted ?? false;

        /// <summary>
        /// The current state of the <see cref="VideoDecoder"/>, as a bindable.
        /// </summary>
        public readonly IBindable<VideoDecoder.DecoderState> State = new Bindable<VideoDecoder.DecoderState>();

        internal double CurrentFrameTime => lastFrame?.Time ?? 0;

        internal int AvailableFrames => availableFrames.Count;

        private double? startTime;

        private VideoDecoder decoder;

        private readonly Stream stream;

        private readonly Queue<DecodedFrame> availableFrames = new Queue<DecodedFrame>();

        private DecodedFrame lastFrame;

        /// <summary>
        /// The total number of frames processed by this instance.
        /// </summary>
        public int FramesProcessed { get; private set; }

        /// <summary>
        /// The length in milliseconds that the decoder can be out of sync before a seek is automatically performed.
        /// </summary>
        private const float lenience_before_seek = 2500;

        private bool isDisposed;

        public VideoSprite([NotNull] Stream stream)
        {
            this.stream = stream ?? throw new ArgumentNullException(nameof(stream));
        }

        public VideoSprite(string filename)
            : this(File.OpenRead(filename))
        {
        }

        [BackgroundDependencyLoader]
        private void load(GameHost gameHost)
        {
            decoder = gameHost.CreateVideoDecoder(stream, Scheduler);
            decoder.Looping = Loop;
            State.BindTo(decoder.State);
            decoder.StartDecoding();
        }

        protected override void Update()
        {
            base.Update();

            if (!startTime.HasValue)
                startTime = Clock.CurrentTime;

            var nextFrame = availableFrames.Count > 0 ? availableFrames.Peek() : null;

            if (nextFrame != null)
            {
                bool tooFarBehind = Math.Abs(PlaybackPosition - nextFrame.Time) > lenience_before_seek &&
                                    (!Loop ||
                                     Math.Abs(PlaybackPosition - decoder.Duration - nextFrame.Time) > lenience_before_seek &&
                                     Math.Abs(PlaybackPosition + decoder.Duration - nextFrame.Time) > lenience_before_seek
                                    );

                // we are too far ahead or too far behind
                if (tooFarBehind && decoder.CanSeek)
                {
                    Logger.Log($"Video too far out of sync ({nextFrame.Time}), seeking to {PlaybackPosition}");
                    decoder.Seek(PlaybackPosition);
                    decoder.ReturnFrames(availableFrames);
                    availableFrames.Clear();
                }
            }

            var frameTime = CurrentFrameTime;

            while (availableFrames.Count > 0 && availableFrames.Peek().Time <= PlaybackPosition && Math.Abs(availableFrames.Peek().Time - PlaybackPosition) < lenience_before_seek)
            {
                if (lastFrame != null) decoder.ReturnFrames(new[] { lastFrame });
                lastFrame = availableFrames.Dequeue();
                Texture = lastFrame.Texture;
            }

            if (availableFrames.Count == 0)
                foreach (var f in decoder.GetDecodedFrames())
                    availableFrames.Enqueue(f);

            Buffering = decoder.IsRunning && availableFrames.Count == 0;

            if (frameTime != CurrentFrameTime)
                FramesProcessed++;
        }

        protected override void Dispose(bool isDisposing)
        {
            if (isDisposed)
                return;

            base.Dispose(isDisposing);

            isDisposed = true;
            decoder?.Dispose();

            foreach (var f in availableFrames)
                f.Texture.Dispose();
        }
    }
}
