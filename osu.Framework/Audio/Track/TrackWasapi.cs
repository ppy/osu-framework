using System;
using System.IO;
using System.Threading.Tasks;
using osu.Framework.Audio.EzLatency;
using osu.Framework.Audio.Mixing;
using osu.Framework.Extensions;

namespace osu.Framework.Audio.Track
{
    /// <summary>
    /// Minimal Track implementation for WASAPI backend (prototype).
    /// Does not perform real decoding/playback yet — provides timing semantics via IAudioBackend.
    /// </summary>
    internal sealed class TrackWasapi : Track
    {
        private readonly IAudioBackend backend;

        private double currentTimeMs;
        private bool isRunning;

        // device time at which playback was started
        private double playbackStartDeviceTimeSec;
        // offset into track (ms) when playback started
        private double playbackStartOffsetMs;

        public override bool IsLoaded => true;

        public override double CurrentTime
        {
            get
            {
                if (isRunning && backend != null)
                {
                    double deviceNow = backend.GetDeviceTimeSeconds();
                    return playbackStartOffsetMs + (deviceNow - playbackStartDeviceTimeSec) * 1000.0;
                }

                return currentTimeMs;
            }
        }

        public override int? Bitrate => null;

        internal TrackWasapi(Stream data, string name, IAudioBackend backend, bool quick = false)
            : base(name)
        {
            this.backend = backend ?? throw new ArgumentNullException(nameof(backend));

            // Length detection / decoding is not implemented in this prototype.
            Length = 0;
        }


        public override Task StartAsync() => EnqueueAction(() =>
        {
            if (isRunning) return;

            playbackStartDeviceTimeSec = backend.GetDeviceTimeSeconds();
            playbackStartOffsetMs = currentTimeMs;
            isRunning = true;

            EzLatencyManager.GLOBAL.RecordPlaybackEvent();
        });

        public override void Start() => StartAsync().WaitSafely();

        public override Task StopAsync() => EnqueueAction(() =>
        {
            if (!isRunning) return;

            // update last known position
            currentTimeMs = CurrentTime;
            isRunning = false;
        });

        public override void Stop() => StopAsync().WaitSafely();

        public override bool Seek(double seek) => SeekAsync(seek).GetResultSafely();

        public override async Task<bool> SeekAsync(double seek)
        {
            await EnqueueAction(() =>
            {
                currentTimeMs = Math.Clamp(seek, 0, Length <= 0 ? double.MaxValue : Length);

                if (isRunning)
                    playbackStartDeviceTimeSec = backend.GetDeviceTimeSeconds();
                else
                    playbackStartOffsetMs = currentTimeMs;
            }).ConfigureAwait(false);

            return true;
        }

        public override bool IsRunning => isRunning;
    }
}
