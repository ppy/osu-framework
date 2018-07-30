// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Allocation;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Lists;
using System;
using System.IO;

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
        public double Duration => decoder.Duration;

        /// <summary>
        /// True if the video has finished playing, false otherwise.
        /// </summary>
        public bool FinishedPlaying => !Loop && PlaybackPosition > Duration;

        /// <summary>
        /// True if the video should loop after finishing its playback, false otherwise.
        /// </summary>
        public bool Loop { get; set; }

        private double? baseTime;
        /// <summary>
        /// The current position of the video playback. The playback position is automatically calculated based on the clock of the VideoSprite. If you want to seek ahead or jump backwards you have to change the <see cref="Timing.IClock.CurrentTime"/> of the clock of this VideoSprite.
        /// </summary>
        public double PlaybackPosition
        {
            get
            {
                if (!baseTime.HasValue)
                    return 0;

                if (Loop)
                    return (Clock.CurrentTime - baseTime.Value) % Duration;
                else
                    return Math.Min(Clock.CurrentTime - baseTime.Value, Duration);
            }
        }

        /// <summary>
        /// The cutoff, in number of frames of video, that the decoder has to be lagging behind to make the VideoSprite stop displaying newly decoded frames until the decoder has caught up. Setting this to null will cause the VideoSprite to always display the decoded frame that is closest to the current playback position, regardless of decoder delay.
        /// </summary>
        public int? HideCutoff;

        /// <summary>
        /// True if during an active <see cref="HideCutoff"/> the last frame that did not fall under the <see cref="HideCutoff"/>-threshold should be displayed until the decoder catches up. Setting this to false will make the VideoSprite clear its display during a <see cref="HideCutoff"/>.
        /// </summary>
        public bool ShowLastFrameDuringHideCutoff;

        /// <summary>
        /// The number of frames that should be preloaded by the decoder. Larger values means higher memory usage, but can avoid falling into <see cref="HideCutoff"/> for small adjustments to the <see cref="PlaybackPosition"/> and can reduce CPU usage on short looping clips.
        /// </summary>
        public int NumberOfPreloadedFrames = 60;

        /// <summary>
        /// True if this VideoSprites decoding process has faulted.
        /// </summary>
        public bool IsFaulted => decoder.IsFaulted;

        private readonly VideoDecoder decoder;

        private readonly SortedList<DecodedFrame> availableFrames;
        private int currentFrameIndex;

        internal int CurrentFrameIndex => currentFrameIndex;

        internal double CurrentFrameTime => currentFrameIndex < availableFrames.Count ? availableFrames[currentFrameIndex].Time : 0;

        internal int AvailableFrames => availableFrames.Count;

        private bool isDisposed;

        private VideoSprite()
        {
            availableFrames = new SortedList<DecodedFrame>((left, right) => left.Time.CompareTo(right.Time));
        }

        public VideoSprite(Stream videoStream)
            : this()
        {
            decoder = new VideoDecoder(videoStream);
        }

        public VideoSprite(string filename)
            : this(File.OpenRead(filename))
        {
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            decoder.StartDecoding();
        }

        protected override void Update()
        {
            base.Update();

            if (!baseTime.HasValue)
                baseTime = Clock.CurrentTime;

            // ensures we do not try to seek with the decoder if the underlying stream does not support seeking (e.g. for network streams)
            if (decoder.CanSeek)
            {
                // commented out, because seamless looping seems to work without this predictive jump. might want to enable this for weaker machines that cant immediately catch up
                // after seeking back to the beginning and need a bit of a grace period.
                /*if (Loop && PlaybackPosition >= Duration - (NumberOfPreloadedFrames / 2) * 1000.0 / AVUtil.av_q2d(stream->avg_frame_rate))
                {
                    decoder.Seek(0);
                }*/
                // we are before the earliest frame of our buffer, and the last frame we decoded is later than our current playback position
                // this means (because lastDecodedFrameTime only ever increments) that we will never normally decode a frame that is useful to our current playback
                // position. Thus, we need to seek to an earlier frame.
                if (availableFrames.Count > 0 && availableFrames[0].Time > PlaybackPosition && decoder.LastDecodedFrameTime > PlaybackPosition)
                {
                    decoder.Seek(PlaybackPosition);
                    availableFrames.Clear();
                }
                // the current playback position demands that we are more than 2 seconds further than the last available frame, so we should seek forward
                else if (availableFrames.Count >= 5 && PlaybackPosition > availableFrames[availableFrames.Count - 1].Time + 2000.0)
                {
                    decoder.Seek(PlaybackPosition);
                    availableFrames.Clear();
                }
            }

            availableFrames.AddRange(decoder.GetDecodedFrames());
            decoder.IsPaused = availableFrames.Count < NumberOfPreloadedFrames;

            var index = availableFrames.BinarySearch(new DecodedFrame { Time = PlaybackPosition });
            if (index < 0)
                index = ~index;
            if (index < availableFrames.Count)
            {
                currentFrameIndex = index;
                var isInHideCutoff = HideCutoff.HasValue && Math.Abs(availableFrames[index].Time - PlaybackPosition) > HideCutoff * 1000.0 / decoder.Framerate;
                if (isInHideCutoff)
                    Texture = ShowLastFrameDuringHideCutoff ? Texture : null;
                else
                    Texture = availableFrames[index].Texture;
            }
            else if (availableFrames.Count > 0)
            {
                currentFrameIndex = availableFrames.Count - 1;
                var isInHideCutoff = HideCutoff.HasValue && Math.Abs(availableFrames[availableFrames.Count - 1].Time - PlaybackPosition) > HideCutoff * 1000.0 / decoder.Framerate;
                if (isInHideCutoff)
                    Texture = ShowLastFrameDuringHideCutoff ? Texture : null;
                else
                    Texture = availableFrames[availableFrames.Count - 1].Texture;
            }
            else
            {
                currentFrameIndex = 0;
                Texture = ShowLastFrameDuringHideCutoff ? Texture : null;
            }

            if (currentFrameIndex > NumberOfPreloadedFrames / 2)
            {
                int nRemovedFrames = Math.Max(1, NumberOfPreloadedFrames / 4);
                availableFrames.RemoveRange(0, nRemovedFrames);
                currentFrameIndex -= nRemovedFrames;
            }
        }

        protected override void Dispose(bool isDisposing)
        {
            if (isDisposed)
                return;

            base.Dispose(isDisposing);

            isDisposed = true;
            decoder.Dispose();

            foreach (var f in availableFrames)
                f.Texture.Dispose();
        }
    }
}
