// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.IO;
using osu.Framework.Logging;

namespace osu.Framework.Audio.Track
{
    /// <summary>
    /// Mainly returns audio data to <see cref="TrackSDL2"/>.
    /// </summary>
    internal class TrackSDL2AudioPlayer : ResamplingPlayer, IDisposable
    {
        private volatile bool isLoaded;
        public bool IsLoaded => isLoaded;

        private volatile bool isLoading;
        public bool IsLoading => isLoading;

        private volatile bool done;
        public virtual bool Done => done;

        /// <summary>
        /// Returns a byte position converted into milliseconds with configuration set for this player.
        /// </summary>
        /// <param name="bytePos">A byte position to convert</param>
        /// <returns></returns>
        public double GetMsFromBytes(long bytePos) => bytePos * 1000.0d / SrcRate / SrcChannels / 4;

        /// <summary>
        /// Returns a position in milliseconds converted from a byte position with configuration set for this player.
        /// </summary>
        /// <param name="seconds">A position in milliseconds to convert</param>
        /// <returns></returns>
        public long GetBytesFromMs(double seconds) => (long)(seconds / 1000.0d * SrcRate) * SrcChannels * 4;

        /// <summary>
        /// Stores raw audio data.
        /// </summary>
        protected MemoryStream? AudioData;

        public long AudioDataLength => AudioData?.Length ?? 0;

        /// <summary>
        /// Play backwards if set to true.
        /// </summary>
        public bool ReversePlayback { get; set; }

        /// <summary>
        /// Creates a new <see cref="TrackSDL2AudioPlayer"/>. Use <see cref="TempoSDL2AudioPlayer"/> if you want to adjust tempo.
        /// </summary>
        /// <param name="rate">Sampling rate of audio</param>
        /// <param name="channels">Channels of audio</param>
        public TrackSDL2AudioPlayer(int rate, byte channels)
            : base(rate, channels)
        {
            isLoading = false;
            isLoaded = false;
        }

        internal void PrepareStream(long byteLength)
        {
            if (disposedValue)
                return;

            if (AudioData == null)
            {
                int len = byteLength > int.MaxValue ? int.MaxValue : (int)byteLength;
                AudioData = new MemoryStream(len);
            }

            isLoading = true;
        }

        internal void PutSamplesInStream(byte[] next, int length)
        {
            if (disposedValue)
                return;

            if (AudioData == null)
                throw new InvalidOperationException($"Use {nameof(PrepareStream)} before calling this");

            long save = AudioData.Position;
            AudioData.Position = AudioData.Length;
            AudioData.Write(next, 0, length);
            AudioData.Position = save;
        }

        internal void DonePutting()
        {
            if (disposedValue)
                return;

            // Saved seek was over data length
            if (SaveSeek > AudioDataLength)
                SaveSeek = 0;

            isLoading = false;
            isLoaded = true;
        }

        protected override int GetRemainingRawBytes(byte[] data)
        {
            if (AudioData == null)
                return 0;

            if (AudioData.Length <= 0)
            {
                done = true;
                return 0;
            }

            if (SaveSeek > 0)
            {
                // set to 0 if position is over saved seek
                if (AudioData.Position > SaveSeek)
                    SaveSeek = 0;

                // player now has audio data to play
                if (AudioData.Length > SaveSeek)
                {
                    AudioData.Position = SaveSeek;
                    SaveSeek = 0;
                }

                // if player didn't reach the position, don't play
                if (SaveSeek > 0)
                    return 0;
            }

            int read = data.Length;

            if (ReversePlayback)
            {
                int frameSize = SrcChannels * 4;

                if (AudioData.Position < read)
                    read = (int)AudioData.Position;

                byte[] temp = new byte[read];

                AudioData.Position -= read;
                read = AudioData.Read(temp, 0, read);
                AudioData.Position -= read;

                for (int e = 0; e < read / frameSize; e++)
                {
                    Buffer.BlockCopy(temp, read - frameSize * (e + 1), data, frameSize * e, frameSize);
                }
            }
            else
            {
                read = AudioData.Read(data, 0, read);
            }

            if (read < data.Length && isLoading)
                Logger.Log("Track underrun!");

            if (ReversePlayback ? AudioData.Position <= 0 : AudioData.Position >= AudioData.Length && !isLoading)
                done = true;

            return read;
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
        }

        /// <summary>
        /// Returns current position converted into milliseconds.
        /// </summary>
        public double GetCurrentTime()
        {
            if (AudioData == null)
                return 0;

            return !ReversePlayback
                ? GetMsFromBytes(AudioData.Position) - GetProcessingLatency()
                : GetMsFromBytes(AudioData.Position) + GetProcessingLatency();
        }

        protected long SaveSeek;

        /// <summary>
        /// Sets the position of this player.
        /// If the given value is over current <see cref="AudioDataLength"/>, it will be saved and pause playback until decoding reaches the position.
        /// However, if the value is still over <see cref="AudioDataLength"/> after the decoding is over, it will be discarded.
        /// </summary>
        /// <param name="seek">Position in milliseconds</param>
        public virtual void Seek(double seek)
        {
            long tmp = GetBytesFromMs(seek);

            if (!isLoaded && tmp > AudioDataLength)
            {
                SaveSeek = tmp;
            }
            else if (AudioData != null)
            {
                SaveSeek = 0;
                AudioData.Position = Math.Clamp(tmp, 0, AudioDataLength - 1);
                Flush();
            }
        }

        private bool disposedValue;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    AudioData?.Dispose();
                    AudioData = null;
                }

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
