// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Buffers;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ManagedBass;
using ManagedBass.Loud;
using osu.Framework.Audio.Callbacks;
using osu.Framework.Extensions;
using osu.Framework.Logging;

namespace osu.Framework.Audio.Track
{
    /// <summary>
    /// Measures loudness of audio samples.
    /// </summary>
    public class TrackLoudness : IDisposable
    {
        private Stream? data;

        private readonly Task readTask;

        private readonly CancellationTokenSource cancelSource = new CancellationTokenSource();

        private float? integratedLoudness;

        /// <summary>
        /// Measures loudness from provided audio data.
        /// </summary>
        /// <param name="data">
        /// The sample data stream.
        /// The <see cref="TrackLoudness"/> will take ownership of this stream and dispose it when done reading track data.
        /// If null, loudness won't get calculated.
        /// </param>
        public TrackLoudness(Stream? data)
        {
            this.data = data;

            readTask = Task.Run(() =>
            {
                if (data == null)
                    return;

                if (Bass.CurrentDevice < 0)
                {
                    Logger.Log("Failed to measure loudness as no bass device is available.", level: LogLevel.Error);
                    return;
                }

                FileCallbacks fileCallbacks = new FileCallbacks(new DataStreamFileProcedures(data));

                int decodeStream = Bass.CreateStream(StreamSystem.NoBuffer, BassFlags.Decode | BassFlags.Float, fileCallbacks.Callbacks, fileCallbacks.Handle);

                if (decodeStream == 0)
                {
                    Logger.Log($"Bass failed to create a stream while trying to measure loudness: {Bass.LastError}", level: LogLevel.Error);
                    fileCallbacks.Dispose();
                    return;
                }

                byte[] buffer = ArrayPool<byte>.Shared.Rent(20000);

                try
                {
                    int loudHandle = BassLoud.Start(decodeStream, BassFlags.BassLoudnessIntegrated | BassFlags.BassLoudnessAutofree, 0);

                    if (loudHandle == 0)
                        throw new InvalidOperationException("Failed to start BassLoud");

                    while (Bass.ChannelGetData(decodeStream, buffer, buffer.Length) >= 0)
                    {
                    }

                    float bassIntegratedLoudness = 1;
                    bool gotLevel = BassLoud.GetLevel(loudHandle, BassFlags.BassLoudnessIntegrated, ref bassIntegratedLoudness);

                    if (!gotLevel)
                        throw new InvalidOperationException("Failed to get loudness level");

                    if (bassIntegratedLoudness < 0)
                        integratedLoudness = bassIntegratedLoudness;
                }
                catch (Exception e)
                {
                    Logger.Error(e, $"{e.Message}: {Bass.LastError}");
                }
                finally
                {
                    Bass.StreamFree(decodeStream);
                    fileCallbacks.Dispose();
                    data.Dispose();

                    ArrayPool<byte>.Shared.Return(buffer);
                }
            }, cancelSource.Token);
        }

        /// <summary>
        /// Returns integrated loudness.
        /// </summary>
        public async Task<float?> GetIntegratedLoudnessAsync()
        {
            await readTask.ConfigureAwait(false);

            return integratedLoudness;
        }

        /// <summary>
        /// Returns integrated loudness.
        /// </summary>
        public float? GetIntegratedLoudness() => GetIntegratedLoudnessAsync().GetResultSafely();

        public static double ConvertToVolumeOffset(int targetLevel, float integratedLoudness) => Math.Pow(10, (targetLevel - (double)integratedLoudness) / 20);

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                cancelSource.Cancel();
                cancelSource.Dispose();
                integratedLoudness = null;

                data?.Dispose();
                data = null;

                disposedValue = true;
            }
        }

        private bool disposedValue;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
