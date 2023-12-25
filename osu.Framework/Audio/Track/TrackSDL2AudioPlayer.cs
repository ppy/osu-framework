// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Buffers;
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
        public double GetMsFromIndex(long bytePos) => bytePos * 1000.0d / SrcRate / SrcChannels;

        /// <summary>
        /// Returns a position in milliseconds converted from a byte position with configuration set for this player.
        /// </summary>
        /// <param name="seconds">A position in milliseconds to convert</param>
        /// <returns></returns>
        public long GetIndexFromMs(double seconds) => (long)(seconds / 1000.0d * SrcRate) * SrcChannels;

        /// <summary>
        /// Stores raw audio data.
        /// </summary>
        protected float[]? AudioData;

        protected long AudioDataPosition;

        private bool dataRented;

        public long AudioDataLength { get; private set; }

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

        // To copy data with long offset with one method
        private unsafe void copyData(float[] src, long srcOffset, float[] dst, long dstOffset, long length)
        {
            if (length <= 0)
                return;

            fixed (float* srcPtr = src)
            fixed (float* dstPtr = dst)
            {
                Buffer.MemoryCopy(srcPtr + srcOffset, dstPtr + dstOffset, (dst.LongLength - dstOffset) * sizeof(float), length * sizeof(float));
            }
        }

        private void prepareArray(long wanted)
        {
            if (wanted <= AudioData?.LongLength)
                return;

            float[] temp;
            bool rent;

            if (wanted > int.MaxValue)
            {
                rent = false;
                temp = new float[wanted];
            }
            else
            {
                rent = true;
                temp = ArrayPool<float>.Shared.Rent((int)wanted);
            }

            if (AudioData != null)
                copyData(AudioData, 0, temp, 0, AudioDataLength);

            if (dataRented && AudioData != null)
                ArrayPool<float>.Shared.Return(AudioData);

            AudioData = temp;
            dataRented = rent;
        }

        internal void PrepareStream(long byteLength)
        {
            if (disposedValue)
                return;

            if (AudioData == null)
                prepareArray(byteLength / 4);

            isLoading = true;
        }

        internal void PutSamplesInStream(byte[] next, int length)
        {
            if (disposedValue)
                return;

            if (AudioData == null)
                throw new InvalidOperationException($"Use {nameof(PrepareStream)} before calling this");

            int floatLen = length / sizeof(float);

            if (AudioDataLength + floatLen > AudioData.LongLength)
                prepareArray(AudioDataLength + floatLen);

            unsafe // Most standard functions doesn't support long
            {
                fixed (float* dest = AudioData)
                fixed (void* ptr = next)
                {
                    float* src = (float*)ptr;
                    Buffer.MemoryCopy(src, dest + AudioDataLength, (AudioData.LongLength - AudioDataLength) * sizeof(float), length);
                }
            }

            AudioDataLength += floatLen;
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

        protected override int GetRemainingRawFloats(float[] data, int offset, int needed)
        {
            if (AudioData == null)
                return 0;

            if (AudioDataLength <= 0)
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
                if (AudioDataLength > SaveSeek)
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
                long remain = AudioDataLength - AudioDataPosition;
                read = remain > needed ? needed : (int)remain;

                copyData(AudioData, AudioDataPosition, data, offset, read);
                AudioDataPosition += read;
            }

            if (read < needed && isLoading)
                Logger.Log("Track underrun!");

            if (ReversePlayback ? AudioDataPosition <= 0 : AudioDataPosition >= AudioDataLength && !isLoading)
                done = true;

            return read;
        }

        public bool Peek(float[] data)
        {
            if (AudioData == null)
                return false;

            long remain = AudioDataLength - AudioDataPosition;
            int read = remain > data.Length ? data.Length : (int)remain;

            copyData(AudioData, AudioDataPosition, data, 0, read);
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
        }

        /// <summary>
        /// Returns current position converted into milliseconds.
        /// </summary>
        public double GetCurrentTime()
        {
            if (AudioData == null)
                return 0;

            return !ReversePlayback
                ? GetMsFromIndex(AudioDataPosition) - GetProcessingLatency()
                : GetMsFromIndex(AudioDataPosition) + GetProcessingLatency();
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
            long tmp = GetIndexFromMs(seek);

            if (!isLoaded && tmp > AudioDataLength)
            {
                SaveSeek = tmp;
            }
            else if (AudioData != null)
            {
                SaveSeek = 0;
                AudioDataPosition = Math.Clamp(tmp, 0, AudioDataLength - 1);
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
                    if (dataRented && AudioData != null)
                        ArrayPool<float>.Shared.Return(AudioData);

                    AudioData = null;
                }

                disposedValue = true;
            }
        }

        ~TrackSDL2AudioPlayer()
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
