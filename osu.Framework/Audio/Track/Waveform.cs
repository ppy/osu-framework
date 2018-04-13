// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ManagedBass;

namespace osu.Framework.Audio.Track
{
    /// <summary>
    /// Procsses audio sample data such that it can then be consumed to generate waveform plots of the audio.
    /// </summary>
    public class Waveform : IDisposable
    {
        /// <summary>
        /// <see cref="WaveformPoint"/>s are initially generated to a 1ms resolution to cover most use cases.
        /// </summary>
        private const float resolution = 0.001f;
        /// <summary>
        /// The data stream is iteratively decoded to provide this many points per iteration so as to not exceed BASS's internal buffer size.
        /// </summary>
        private const int points_per_iteration = 100000;
        private const int bytes_per_sample = 4;

        private int channels;
        private List<WaveformPoint> points = new List<WaveformPoint>();

        private readonly CancellationTokenSource cancelSource = new CancellationTokenSource();
        private readonly Task readTask;

        /// <summary>
        /// Constructs a new <see cref="Waveform"/> from provided audio data.
        /// </summary>
        /// <param name="data">The sample data stream. If null, an empty waveform is constructed.</param>
        public Waveform(Stream data = null)
        {
            if (data == null) return;

            readTask = Task.Run(() =>
            {
                var procs = new DataStreamFileProcedures(data);

                int decodeStream = Bass.CreateStream(StreamSystem.NoBuffer, BassFlags.Decode | BassFlags.Float, procs.BassProcedures, IntPtr.Zero);

                Bass.ChannelGetInfo(decodeStream, out ChannelInfo info);

                long length = Bass.ChannelGetLength(decodeStream);

                // Each "point" is generated from a number of samples, each sample contains a number of channels
                int sampleDataPerPoint = (int)(info.Frequency * resolution * info.Channels);
                points.Capacity = (int)(length / sampleDataPerPoint);

                int bytesPerIteration = sampleDataPerPoint * points_per_iteration;
                var dataBuffer = new float[bytesPerIteration / bytes_per_sample];

                while (length > 0)
                {
                    length = Bass.ChannelGetData(decodeStream, dataBuffer, bytesPerIteration);
                    int samplesRead = (int)(length / bytes_per_sample);

                    // Process a sequence of samples for each point
                    for (int i = 0; i < samplesRead; i += sampleDataPerPoint)
                    {
                        // Process each sample in the sequence
                        var point = new WaveformPoint(info.Channels);
                        for (int j = i; j < i + sampleDataPerPoint; j += info.Channels)
                        {
                            // Process each channel in the sample
                            for (int c = 0; c < info.Channels; c++)
                                point.Amplitude[c] = Math.Max(point.Amplitude[c], Math.Abs(dataBuffer[j + c]));
                        }

                        for (int c = 0; c < info.Channels; c++)
                            point.Amplitude[c] = Math.Min(1, point.Amplitude[c]);

                        points.Add(point);
                    }
                }

                channels = info.Channels;
            }, cancelSource.Token);
        }

        /// <summary>
        /// Creates a new <see cref="Waveform"/> containing a specific number of data points by selecting the average value of each sampled group.
        /// </summary>
        /// <param name="pointCount">The number of points the resulting <see cref="Waveform"/> should contain.</param>
        /// <param name="cancellationToken">The token to cancel the task.</param>
        /// <returns>An async task for the generation of the <see cref="Waveform"/>.</returns>
        public async Task<Waveform> GenerateResampledAsync(int pointCount, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (pointCount < 0) throw new ArgumentOutOfRangeException(nameof(pointCount));

            if (readTask == null)
                return new Waveform();

            await readTask;

            return await Task.Run(() =>
            {
                var generatedPoints = new List<WaveformPoint>();
                float pointsPerGeneratedPoint = (float)points.Count / pointCount;

                for (float i = 0; i < points.Count; i += pointsPerGeneratedPoint)
                {
                    int startIndex = (int)i;
                    int endIndex = (int)Math.Min(points.Count, Math.Ceiling(i + pointsPerGeneratedPoint));

                    var point = new WaveformPoint(channels);
                    for (int j = startIndex; j < endIndex; j++)
                    {
                        for (int c = 0; c < channels; c++)
                            point.Amplitude[c] += points[j].Amplitude[c];
                    }

                    // Mean
                    for (int c = 0; c < channels; c++)
                        point.Amplitude[c] /= endIndex - startIndex;

                    generatedPoints.Add(point);
                }

                return new Waveform
                {
                    points = generatedPoints,
                    channels = channels
                };
            }, cancellationToken);
        }

        /// <summary>
        /// Gets all the points represented by this <see cref="Waveform"/>.
        /// </summary>
        public List<WaveformPoint> GetPoints() => GetPointsAsync().Result;

        /// <summary>
        /// Gets all the points represented by this <see cref="Waveform"/>.
        /// </summary>
        public async Task<List<WaveformPoint>> GetPointsAsync()
        {
            if (readTask == null)
                return points;

            await readTask;
            return points;
        }

        /// <summary>
        /// Gets the number of channels represented by each <see cref="WaveformPoint"/>.
        /// </summary>
        public int GetChannels() => GetChannelsAsync().Result;

        /// <summary>
        /// Gets the number of channels represented by each <see cref="WaveformPoint"/>.
        /// </summary>
        public async Task<int> GetChannelsAsync()
        {
            if (readTask == null)
                return channels;

            await readTask;
            return channels;
        }

        #region Disposal

        ~Waveform()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private bool isDisposed;

        protected virtual void Dispose(bool disposing)
        {
            if (isDisposed)
                return;
            isDisposed = true;

            cancelSource?.Cancel();
            cancelSource?.Dispose();
            points = null;
        }

        #endregion
    }

    /// <summary>
    /// Represents a singular point of data in a <see cref="Waveform"/>.
    /// </summary>
    public struct WaveformPoint
    {
        /// <summary>
        /// An array of amplitudes, one for each channel.
        /// </summary>
        public readonly float[] Amplitude;

        /// <summary>
        /// Cconstructs a <see cref="WaveformPoint"/>.
        /// </summary>
        /// <param name="channels">The number of channels that contain data.</param>
        public WaveformPoint(int channels)
        {
            Amplitude = new float[channels];
        }
    }
}
