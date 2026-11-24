// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using mysoundlib_cs;
using osu.Framework.Audio.Callbacks;
using osu.Framework.Audio.Mixing.SDL3;
using osu.Framework.Extensions;

namespace osu.Framework.Audio.Track
{
    public sealed class TrackSDL3 : Track, ISDL3AudioChannel, SDL3AudioDecoderQueue.IWorker
    {
        public IntPtr Handle { get; private set; }

        public override bool IsDummyDevice => false;

        private volatile bool isLoading;
        private volatile bool isLoaded;

        public override bool IsLoaded => isLoading || isLoaded;

        private double currentTime;
        public override double CurrentTime => currentTime;

        private volatile bool isRunning;
        public override bool IsRunning => isRunning;

        private volatile bool hasCompleted;
        public override bool HasCompleted => hasCompleted;

        private volatile int bitrate;
        public override int? Bitrate => bitrate;

        private Stream? dataStream;
        private SDL3AudioFileCallbacks? fileCallbacks;

        public TrackSDL3(Stream data, string name)
            : base(name)
        {
            // SoundTouch limitation
            const float tempo_minimum_supported = 0.05f;
            AggregateTempo.ValueChanged += t =>
            {
                if (t.NewValue < tempo_minimum_supported)
                    throw new ArgumentException($"{nameof(TrackSDL3)} does not support {nameof(Tempo)} specifications below {tempo_minimum_supported}. Use {nameof(Frequency)} instead.");
            };

            dataStream = data;
            fileCallbacks = new SDL3AudioFileCallbacks(dataStream);
            Handle = MySoundLibrary.mslCreateTrack().ThrowIfNull();

            EnqueueAction(() =>
            {
                // Do first decoding on audio thread to force length population
                if (((SDL3AudioDecoderQueue.IWorker)this).DoWork())
                    SDL3AudioDecoderQueue.INSTANCE.Enqueue(this);
            });
        }

        private IntPtr decoder = IntPtr.Zero;
        private readonly object lockHandle = new object();

        unsafe bool SDL3AudioDecoderQueue.IWorker.DoWork()
        {
            lock (lockHandle)
            {
                if (fileCallbacks == null)
                    return false;

                void cleanup()
                {
                    fixed (IntPtr* ptr = &decoder)
                        MySoundLibrary.mslDestroyAudioDecoder(ptr);

                    fileCallbacks?.Dispose();
                    fileCallbacks = null;

                    dataStream?.Dispose();
                    dataStream = null;
                }

                if (decoder == IntPtr.Zero)
                    decoder = MySoundLibrary.mslCreateTrackDecoder(Handle, fileCallbacks.Handle, fileCallbacks.ReadCallback, fileCallbacks.SeekCallback);

                if (!MySoundLibrary.mslDecoderIsDone(decoder).ToBool())
                {
                    int ret = MySoundLibrary.mslDecoderProcessChunk(decoder);

                    if (ret < 0)
                    {
                        Logging.Logger.Log($"decoder error ({ret})");
                        cleanup();
                        return false;
                    }
                    else if (!isLoading)
                    {
                        isLoading = true;
                        MySoundLibrary.mslDecoderGetMetadata(decoder, out int kbps, out double length, out _);
                        bitrate = kbps;
                        Length = length;
                    }

                    return true;
                }
                else
                {
                    isLoading = false;
                    isLoaded = true;

                    cleanup();

                    Length = MySoundLibrary.mslTrackGetLength(Handle);
                    return false;
                }
            }
        }

        private volatile bool amplitudeRequested;
        private double lastTime;

        private ChannelAmplitudes currentAmplitudes = ChannelAmplitudes.Empty;
        private float[]? fftResult;

        public override ChannelAmplitudes CurrentAmplitudes
        {
            get
            {
                if (!amplitudeRequested)
                {
                    amplitudeRequested = true;
                    return ChannelAmplitudes.Empty;
                }

                return isRunning ? currentAmplitudes : ChannelAmplitudes.Empty;
            }
        }

        private unsafe void updateCurrentAmplitude()
        {
            fftResult ??= new float[ChannelAmplitudes.AMPLITUDES_SIZE];
            Span<float> lrAmp = stackalloc float[2];
            int result;

            fixed (float* ptr = fftResult)
            fixed (float* amp = lrAmp)
                result = MySoundLibrary.mslTrackCalculateCurrent(Handle, 0, 1, ChannelAmplitudes.AMPLITUDES_SIZE * 2, ptr, amp, null);

            currentAmplitudes = result > 0 ? new ChannelAmplitudes(lrAmp[0], lrAmp[1], fftResult) : ChannelAmplitudes.Empty;
        }

        private double restartPoint;

        public override double RestartPoint
        {
            get => restartPoint;
            set => EnqueueAction(() =>
            {
                if (IsDisposed || restartPoint == value)
                    return;

                MySoundLibrary.mslTrackSetRestartPoint(Handle, value);
                restartPoint = value;
            });
        }

        private bool looping;

        public override bool Looping
        {
            get => looping;
            set => EnqueueAction(() =>
            {
                if (IsDisposed || looping == value)
                    return;

                MySoundLibrary.mslTrackSetLoop(Handle, value.ToIntBool());
                looping = value;
            });
        }

        protected override void UpdateState()
        {
            base.UpdateState();

            if (IsDisposed)
                return;

            if (MySoundLibrary.mslTrackIsDone(Handle).ToBool() && isRunning)
            {
                isRunning = false;
                hasCompleted = true;
                RaiseCompleted();
            }

            Interlocked.Exchange(ref currentTime, MySoundLibrary.mslTrackGetPosition(Handle));

            MySoundLibrary.mslTrackUpdate(Handle);

            if (amplitudeRequested && isRunning && Math.Abs(currentTime - lastTime) > 1000.0 / 60.0)
            {
                updateCurrentAmplitude();
                lastTime = currentTime;
            }
        }

        public override bool Seek(double seek) => SeekAsync(seek).GetResultSafely();

        public override async Task<bool> SeekAsync(double seek)
        {
            bool result = false;
            await EnqueueAction(() => result = seekInternal(seek)).ConfigureAwait(false);

            if (!result)
                Logging.Logger.Log("Track failed seeking to " + seek);

            return result;
        }

        private bool seekInternal(double seek)
        {
            if (IsDisposed)
                return false;

            double result = seek;

            if (result >= Length)
            {
                result = Length;

                if (MySoundLibrary.mslTrackIsLoaded(Handle).ToBool())
                    hasCompleted = true;
            }
            else
            {
                hasCompleted = false;
            }

            result = MySoundLibrary.mslTrackSetPosition(Handle, result);

            Interlocked.Exchange(ref currentTime, MySoundLibrary.mslTrackGetPosition(Handle));

            return seek == result;
        }

        public override void Start()
        {
            if (IsDisposed)
                throw new ObjectDisposedException(ToString(), "Can not start disposed tracks.");

            StartAsync().WaitSafely();
        }

        public override Task StartAsync() => EnqueueAction(startInternal);

        private void startInternal()
        {
            if (IsDisposed)
                return;

            InvalidateState();

            MySoundLibrary.mslTrackPlay(Handle);
            isRunning = true;
            hasCompleted = false;
        }

        public override void Stop() => StopAsync().WaitSafely();

        public override Task StopAsync() => EnqueueAction(() =>
        {
            if (IsDisposed)
                return;

            MySoundLibrary.mslTrackPause(Handle);
            isRunning = false;
        });

        internal override void OnStateChanged()
        {
            base.OnStateChanged();

            if (IsDisposed)
                return;

            MySoundLibrary.mslTrackSetVolume(Handle, AggregateVolume.Value, AggregateBalance.Value);
            MySoundLibrary.mslTrackSetFreqTempo(Handle, AggregateFrequency.Value, AggregateTempo.Value);
        }

        public bool IsActive => !IsDisposed;

        ~TrackSDL3()
        {
            Dispose(false);
        }

        protected override void Dispose(bool disposing)
        {
            if (IsDisposed)
                return;

            isRunning = false;

            if (Mixer is SDL3AudioMixer mixer)
            {
                // This one is pedantic because in normal situation, when dispose gets called through deconstructor,
                // track should already be removed from the mixer. so this is just a safe check if mixer is still not null until disposal.
                MySoundLibrary.mslMixerRemoveTrack(mixer.Handle, Handle);
                mixer.RemoveChannel(this);
            }

            base.Dispose(disposing);

            lock (lockHandle)
            {
                unsafe
                {
                    fixed (IntPtr* ptr = &decoder)
                        MySoundLibrary.mslDestroyAudioDecoder(ptr);
                }

                MySoundLibrary.mslDestroyTrack(Handle);
                Handle = IntPtr.Zero;

                fileCallbacks?.Dispose();
                fileCallbacks = null;

                dataStream?.Dispose();
                dataStream = null;
            }
        }
    }
}
