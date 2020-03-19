// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Graphics.Sprites;
using osuTK;
using System;
using System.Collections.Generic;
using System.IO;
using JetBrains.Annotations;
using osu.Framework.Bindables;
using osu.Framework.Logging;
using osu.Framework.Platform;
using osu.Framework.Graphics.Shaders;
using osu.Framework.Timing;

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
                if (Loop)
                    return Clock.CurrentTime % Duration;

                return Math.Min(Clock.CurrentTime, Duration);
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

        private VideoDecoder decoder;

        private readonly Stream stream;
        private readonly bool startAtCurrentTime;

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

        /// <summary>
        /// YUV->RGB conversion matrix based on the video colorspace
        /// </summary>
        public Matrix3 ConversionMatrix => decoder.GetConversionMatrix();

        protected override DrawNode CreateDrawNode() => new VideoSpriteDrawNode(this);

        /// <summary>
        /// Creates a new <see cref="VideoSprite"/>.
        /// </summary>
        /// <param name="filename">The video file.</param>
        /// <param name="startAtCurrentTime">Whether the current clock time should be assumed as the 0th video frame.<br />
        /// If <code>true</code>, the current clock time will be assumed as the 0th video frame. A custom <see cref="Clock"/> cannot be set.<br />
        /// If <code>false</code>, a current clock time of 0 will be assumed as the 0th video frame. A custom <see cref="Clock"/> can be set.</param>
        public VideoSprite(string filename, bool startAtCurrentTime = true)
            : this(File.OpenRead(filename), startAtCurrentTime)
        {
        }

        /// <summary>
        /// Creates a new <see cref="VideoSprite"/>.
        /// </summary>
        /// <param name="stream">The video file stream.</param>
        /// <param name="startAtCurrentTime">Whether the current clock time should be assumed as the 0th video frame.<br />
        /// If <code>true</code>, the current clock time will be assumed as the 0th video frame. A custom <see cref="Clock"/> cannot be set.<br />
        /// If <code>false</code>, a current clock time of 0 will be assumed as the 0th video frame. A custom <see cref="Clock"/> can be set.</param>
        public VideoSprite([NotNull] Stream stream, bool startAtCurrentTime = true)
        {
            this.stream = stream ?? throw new ArgumentNullException(nameof(stream));
            this.startAtCurrentTime = startAtCurrentTime;
        }

        [BackgroundDependencyLoader]
        private void load(GameHost gameHost, ShaderManager shaders)
        {
            TextureShader = shaders.Load(VertexShaderDescriptor.TEXTURE_2, FragmentShaderDescriptor.VIDEO);
            RoundedTextureShader = shaders.Load(VertexShaderDescriptor.TEXTURE_2, FragmentShaderDescriptor.VIDEO_ROUNDED);
            decoder = gameHost.CreateVideoDecoder(stream, Scheduler);
            decoder.Looping = Loop;
            State.BindTo(decoder.State);
            decoder.StartDecoding();
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
                    throw new InvalidOperationException($"A {nameof(VideoSprite)} with {startAtCurrentTime} = true cannot receive a custom {nameof(Clock)}.");

                base.Clock = value;
            }
        }

        protected override void Update()
        {
            base.Update();

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

                var tex = lastFrame.Texture;

                // Check if the new frame has been uploaded so we don't display an old frame
                if ((tex?.TextureGL as VideoTexture)?.UploadComplete ?? false)
                    Texture = tex;
            }

            if (availableFrames.Count == 0)
            {
                foreach (var f in decoder.GetDecodedFrames())
                    availableFrames.Enqueue(f);
            }

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
