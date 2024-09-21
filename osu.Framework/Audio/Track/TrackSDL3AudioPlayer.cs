// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Threading;

namespace osu.Framework.Audio.Track
{
    /// <summary>
    /// Mainly returns audio data to <see cref="TrackSDL3"/>.
    /// </summary>
    internal class TrackSDL3AudioPlayer : ResamplingPlayer, IDisposable
    {
        private volatile bool isLoaded;
        public bool IsLoaded => isLoaded;

        private volatile bool isLoading;
        public bool IsLoading => isLoading;

        private volatile bool done;
        public virtual bool Done => done;

        /// <summary>
        /// Returns a data position converted into milliseconds with configuration set for this player.
        /// </summary>
        /// <param name="pos">Position to convert</param>
        /// <returns></returns>
        public double GetMsFromIndex(long pos) => pos * 1000.0d / SrcRate / SrcChannels;

        /// <summary>
        /// Returns a position in milliseconds converted from a byte position with configuration set for this player.
        /// </summary>
        /// <param name="seconds">A position in milliseconds to convert</param>
        /// <returns></returns>
        public long GetIndexFromMs(double seconds) => (long)Math.Ceiling(seconds / 1000.0d * SrcRate) * SrcChannels;

        /// <summary>
        /// Stores raw audio data.
        /// </summary>
        protected float[]? AudioData;

        protected long AudioDataPosition;

        private long audioDataLength;

        public double AudioLength => GetMsFromIndex(audioDataLength);

        /// <summary>
        /// Play backwards if set to true.
        /// </summary>
        public bool ReversePlayback { get; set; }

        /// <summary>
        /// Creates a new <see cref="TrackSDL3AudioPlayer"/>. Use <see cref="TempoSDL3AudioPlayer"/> if you want to adjust tempo.
        /// </summary>
        /// <param name="rate">Sampling rate of audio</param>
        /// <param name="channels">Channels of audio</param>
        public TrackSDL3AudioPlayer(int rate, int channels)
            : base(rate, channels)
        {
            isLoading = false;
            isLoaded = false;
        }

        private void prepareArray(long wanted)
        {
            if (wanted <= AudioData?.LongLength)
                return;

            float[] temp = new float[wanted];

            if (AudioData != null)
                Array.Copy(AudioData, 0, temp, 0, audioDataLength);

            AudioData = temp;
        }

        internal void PrepareStream(long byteLength = 3 * 60 * 44100 * 2 * 4)
        {
            if (isDisposed)
                return;

            if (AudioData == null)
                prepareArray(byteLength / 4);

            isLoading = true;
        }

        internal void PutSamplesInStream(byte[] next, int length)
        {
            if (isDisposed)
                return;

            if (AudioData == null)
                throw new InvalidOperationException($"Use {nameof(PrepareStream)} before calling this");

            int floatLen = length / sizeof(float);

            if (audioDataLength + floatLen > AudioData.LongLength)
                prepareArray(audioDataLength + floatLen);

            for (int i = 0; i < floatLen; i++)
            {
                float src = BitConverter.ToSingle(next, i * sizeof(float));
                AudioData[audioDataLength++] = src;
            }
        }

        internal void DonePutting()
        {
            if (isDisposed)
                return;

            // Saved seek was over data length
            if (SaveSeek > audioDataLength)
                SaveSeek = 0;

            isLoading = false;
            isLoaded = true;
        }

        protected override int GetRemainingRawFloats(float[] data, int offset, int needed)
        {
            if (AudioData == null)
                return 0;

            if (audioDataLength <= 0)
            {
                done = true;
                return 0;
            }

            if (SaveSeek > 0)
            {
                // set to 0 if position is over saved seek
                if (AudioDataPosition > SaveSeek)
                    SaveSeek = 0;

                // player now has audio data to play
                if (audioDataLength > SaveSeek)
                {
                    AudioDataPosition = SaveSeek;
                    SaveSeek = 0;
                }

                // if player didn't reach the position, don't play
                if (SaveSeek > 0)
                    return 0;
            }

            int read;

            if (ReversePlayback)
            {
                for (read = 0; read < needed; read += 2)
                {
                    if (AudioDataPosition < 0)
                    {
                        AudioDataPosition = 0;
                        break;
                    }

                    // swap stereo channel
                    data[read + 1 + offset] = AudioData[AudioDataPosition--];
                    data[read + offset] = AudioData[AudioDataPosition--];
                }
            }
            else
            {
                long remain = audioDataLength - AudioDataPosition;
                read = (int)Math.Min(needed, remain);

                Array.Copy(AudioData, AudioDataPosition, data, offset, read);
                AudioDataPosition += read;
            }

            if (ReversePlayback ? AudioDataPosition <= 0 : AudioDataPosition >= audioDataLength && !isLoading)
                done = true;

            return read;
        }

        /// <summary>
        /// Puts recently played audio samples into data. Mostly used to calculate amplitude of a track.
        /// </summary>
        /// <param name="data">A float array to put data in</param>
        /// <param name="posMs"></param>
        /// <returns>True if succeeded</returns>
        public bool Peek(float[] data, double posMs)
        {
            if (AudioData == null)
                return false;

            long pos = GetIndexFromMs(posMs);
            long len = Interlocked.Read(ref audioDataLength);

            long start = Math.Clamp(pos, 0, len);
            long remain = len - start;

            Array.Copy(AudioData, start, data, 0, Math.Min(data.Length, remain));
            return true;
        }

        /// <summary>
        /// Clears 'done' status.
        /// </summary>
        /// <param name="resetPosition">Goes back to the start if set to true.</param>
        public virtual void Reset(bool resetPosition = true)
        {
            done = false;

            if (resetPosition)
            {
                SaveSeek = 0;
                Seek(0);
            }

            Clear();
        }

        /// <summary>
        /// Returns current position converted into milliseconds.
        /// </summary>
        public double GetCurrentTime()
        {
            if (SaveSeek > 0)
                return GetMsFromIndex(SaveSeek);

            if (AudioData == null)
                return 0;

            return !ReversePlayback
                ? GetMsFromIndex(AudioDataPosition) - GetProcessingLatency()
                : GetMsFromIndex(AudioDataPosition) + GetProcessingLatency();
        }

        protected long SaveSeek;

        /// <summary>
        /// Sets the position of this player.
        /// If the given value is over current <see cref="audioDataLength"/>, it will be saved and pause playback until decoding reaches the position.
        /// However, if the value is still over <see cref="audioDataLength"/> after the decoding is over, it will be discarded.
        /// </summary>
        /// <param name="seek">Position in milliseconds</param>
        public virtual void Seek(double seek)
        {
            long tmp = GetIndexFromMs(seek);

            if (!isLoaded && tmp > audioDataLength)
            {
                SaveSeek = tmp;
            }
            else if (AudioData != null)
            {
                SaveSeek = 0;
                AudioDataPosition = Math.Clamp(tmp, 0, Math.Max(0, audioDataLength - 1));
                Clear();
            }
        }

        private volatile bool isDisposed;

        protected virtual void Dispose(bool disposing)
        {
            if (!isDisposed)
            {
                AudioData = null;
                isDisposed = true;
            }
        }

        ~TrackSDL3AudioPlayer()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
