// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.IO;
using System.Runtime.InteropServices;
using ManagedBass;
using ManagedBass.Fx;
using OpenTK;
using osu.Framework.IO;
using System.Diagnostics;

namespace osu.Framework.Audio.Track
{
    public class AudioTrackBass : AudioTrack, IBassAudio
    {
        private float initialFrequency;

        private int audioStreamPrefilter;

        private AsyncBufferStream dataStream;

        /// <summary>
        /// Should this track only be used for preview purposes? This suggests it has not yet been fully loaded.
        /// </summary>
        public bool Preview { get; private set; }

        /// <summary>
        /// The handle for this track, if there is one.
        /// </summary>
        private int activeStream;

        //must keep a reference to this else it will be garbage collected early.
        private DataStreamFileProcedures procs;

        /// <summary>
        /// This marks if the track is paused, or stopped to the end.
        /// </summary>
        private bool isPlayed;

        public AudioTrackBass(Stream data, bool quick = false)
        {
            PendingActions.Enqueue(() =>
            {
                Preview = quick;

                if (data == null)
                    throw new ArgumentNullException(nameof(data));
                //encapsulate incoming stream with async buffer if it isn't already.
                dataStream = data as AsyncBufferStream ?? new AsyncBufferStream(data, quick ? 8 : -1);

                procs = new DataStreamFileProcedures(dataStream);

                BassFlags flags = Preview ? 0 : (BassFlags.Decode | BassFlags.Prescan);
                audioStreamPrefilter = Bass.CreateStream(StreamSystem.NoBuffer, flags, procs.BassProcedures, IntPtr.Zero);

                if (Preview)
                    activeStream = audioStreamPrefilter;
                else
                {
                    // We assign the BassFlags.Decode streams to the device "bass_nodevice" to prevent them from getting
                    // cleaned up during a Bass.Free call. This is necessary for seamless switching between audio devices.
                    // Further, we provide the flag BassFlags.FxFreeSource such that freeing the activeStream also frees
                    // all parent decoding streams.
                    const int bass_nodevice = 0x20000;

                    Bass.ChannelSetDevice(audioStreamPrefilter, bass_nodevice);
                    activeStream = BassFx.TempoCreate(audioStreamPrefilter, BassFlags.Decode | BassFlags.FxFreeSource);
                    Bass.ChannelSetDevice(activeStream, bass_nodevice);
                    activeStream = BassFx.ReverseCreate(activeStream, 5f, BassFlags.Default | BassFlags.FxFreeSource);

                    Bass.ChannelSetAttribute(activeStream, ChannelAttribute.TempoUseQuickAlgorithm, 1);
                    Bass.ChannelSetAttribute(activeStream, ChannelAttribute.TempoOverlapMilliseconds, 4);
                    Bass.ChannelSetAttribute(activeStream, ChannelAttribute.TempoSequenceMilliseconds, 30);
                }

                Length = (Bass.ChannelBytes2Seconds(activeStream, Bass.ChannelGetLength(activeStream)) * 1000);
                Bass.ChannelGetAttribute(activeStream, ChannelAttribute.Frequency, out initialFrequency);
            });

            InvalidateState();
        }

        void IBassAudio.UpdateDevice(int deviceIndex)
        {
            Bass.ChannelSetDevice(activeStream, deviceIndex);
            Debug.Assert(Bass.LastError == Errors.OK);
        }

        public override void Reset()
        {
            Stop();
            Seek(0);
            Volume.Value = 1;
            base.Reset();
        }

        protected override void Dispose(bool disposing)
        {
            PendingActions.Enqueue(() =>
            {
                if (activeStream != 0) Bass.ChannelStop(activeStream);

                if (audioStreamPrefilter != 0) Bass.StreamFree(audioStreamPrefilter);

                activeStream = 0;
                audioStreamPrefilter = 0;

                dataStream?.Dispose();
                dataStream = null;
            });

            base.Dispose(disposing);
        }

        public override bool IsDummyDevice => false;

        public override void Stop()
        {
            isPlayed = false;
            PendingActions.Enqueue(() =>
            {
                if (IsRunning)
                    Bass.ChannelPause(activeStream);
            });
        }

        private int direction;

        private void setDirection(bool reverse)
        {
            direction = reverse ? -1 : 1;
            Bass.ChannelSetAttribute(activeStream, ChannelAttribute.ReverseDirection, direction);
        }

        public override void Start()
        {
            isPlayed = true;
            PendingActions.Enqueue(() =>
            {
                Bass.ChannelPlay(activeStream);
            });
        }

        public override bool Seek(double seek)
        {
            // At this point the track may not yet be loaded which is indicated by a 0 length.
            // In that case we still want to return true, hence the conservative length.
            double conservativeLength = Length == 0 ? double.MaxValue : Length;
            double conservativeClamped = MathHelper.Clamp(seek, 0, conservativeLength);

            PendingActions.Enqueue(() =>
            {
                double clamped = MathHelper.Clamp(seek, 0, Length);

                if (clamped != CurrentTime)
                {
                    long pos = Bass.ChannelSeconds2Bytes(activeStream, clamped / 1000d);
                    Bass.ChannelSetPosition(activeStream, pos);
                }
            });

            return conservativeClamped == seek;
        }

        public override double CurrentTime
        {
            get
            {
                double value = Bass.ChannelBytes2Seconds(activeStream, Bass.ChannelGetPosition(activeStream)) * 1000;
                if (value == Length && !isPlayed) return 0;
                else return value;
            }
        }

        public override bool IsRunning => Bass.ChannelIsActive(activeStream) == PlaybackState.Playing;

        protected override void OnStateChanged(object sender, EventArgs e)
        {
            base.OnStateChanged(sender, e);

            setDirection(FrequencyCalculated.Value < 0);

            Bass.ChannelSetAttribute(activeStream, ChannelAttribute.Volume, VolumeCalculated);
            Bass.ChannelSetAttribute(activeStream, ChannelAttribute.Pan, BalanceCalculated);
            Bass.ChannelSetAttribute(activeStream, ChannelAttribute.Frequency, bassFreq);
            Bass.ChannelSetAttribute(activeStream, ChannelAttribute.Tempo, (Math.Abs(Tempo) - 1) * 100);
        }

        int bassFreq => (int)MathHelper.Clamp(Math.Abs(initialFrequency * FrequencyCalculated), 100, 100000);

        public override double Rate => bassFreq / initialFrequency * Tempo * direction;

        public override int? Bitrate => (int)Bass.ChannelGetAttribute(activeStream, ChannelAttribute.Bitrate);

        public override bool HasCompleted => base.HasCompleted || (!IsRunning && CurrentTime >= Length);

        private class DataStreamFileProcedures
        {
            private byte[] readBuffer = new byte[32768];

            private AsyncBufferStream dataStream;

            public FileProcedures BassProcedures => new FileProcedures
            {
                Close = ac_Close,
                Length = ac_Length,
                Read = ac_Read,
                Seek = ac_Seek
            };

            public DataStreamFileProcedures(AsyncBufferStream data)
            {
                dataStream = data;
            }

            private void ac_Close(IntPtr user)
            {
                //manually handle closing of stream
            }

            private long ac_Length(IntPtr user)
            {
                if (dataStream == null) return 0;

                try
                {
                    return dataStream.Length;
                }
                catch
                {
                }

                return 0;
            }

            private int ac_Read(IntPtr buffer, int length, IntPtr user)
            {
                if (dataStream == null) return 0;

                try
                {
                    if (length > readBuffer.Length)
                        readBuffer = new byte[length];

                    if (!dataStream.CanRead)
                        return 0;

                    int readBytes = dataStream.Read(readBuffer, 0, length);
                    Marshal.Copy(readBuffer, 0, buffer, readBytes);
                    return readBytes;
                }
                catch
                {
                }

                return 0;
            }

            private bool ac_Seek(long offset, IntPtr user)
            {
                if (dataStream == null) return false;

                try
                {
                    return dataStream.Seek(offset, SeekOrigin.Begin) == offset;
                }
                catch
                {
                }
                return false;
            }
        }
    }
}
