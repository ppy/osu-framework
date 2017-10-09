// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
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
        /// The number of channels represented by each <see cref="WaveformPoint"/> in <see cref="Points"/>.
        /// </summary>
        public int Channels { get; private set; }

        private List<WaveformPoint> points = new List<WaveformPoint>();

        /// <summary>
        /// List of all <see cref="WaveformPoint"/>s.
        /// </summary>
        public IReadOnlyList<WaveformPoint> Points => points;

        private readonly CancellationTokenSource readSource = new CancellationTokenSource();
        private readonly Task readTask;

        /// <summary>
        /// Constructs a new <see cref="Waveform"/>.
        /// </summary>
        /// <param name="data">The sample data stream.</param>
        public Waveform(Stream data)
        {
            readTask = Task.Run(() =>
            {
                var procs = new DataStreamFileProcedures(data);

                int decodeStream = Bass.CreateStream(StreamSystem.NoBuffer, BassFlags.Decode | BassFlags.Float, procs.BassProcedures, IntPtr.Zero);

                ChannelInfo info;
                Bass.ChannelGetInfo(decodeStream, out info);

                long length = Bass.ChannelGetLength(decodeStream);
                var rawData = new float[length / 4];
                Bass.ChannelGetData(decodeStream, rawData, (int)length);

                // Each "point" is generated from a number of samples, each sample contains a number of channels
                int sampleDataPerPoint = (int)(info.Frequency * resolution * info.Channels);
                points.Capacity = rawData.Length / sampleDataPerPoint;

                // Process a sequence of samples for each point
                for (int i = 0; i < rawData.Length; i += sampleDataPerPoint)
                {
                    int endIndex = Math.Min(rawData.Length, i + sampleDataPerPoint);

                    // Process each sample in the sequence
                    var point = new WaveformPoint(info.Channels);
                    for (int j = i; j < endIndex; j += info.Channels)
                    {
                        // Process each channel in the sample
                        for (int c = 0; c < info.Channels; c++)
                            point.Amplitude[c] = Math.Max(point.Amplitude[c], Math.Abs(rawData[j + c]));
                    }

                    points.Add(point);
                }

                Channels = info.Channels;
            }, readSource.Token);
        }

        private Waveform()
        {
        }

        /// <summary>
        /// Reads the data stream to generate the <see cref="WaveformPoint"/>s.
        /// </summary>
        /// <returns>The task.</returns>
        public Task ReadAsync() => readTask;

        private CancellationTokenSource generationSource;

        /// <summary>
        /// Generates a <see cref="Waveform"/> containing a specific number of points.
        /// </summary>
        /// <param name="pointCount">The number of points the resulting <see cref="Waveform"/> should contain.</param>
        /// <returns>An async task for the generation of the <see cref="Waveform"/>.</returns>
        public Task<Waveform> GenerateAsync(int pointCount)
        {
            CancelGenerationAsync();
            generationSource = new CancellationTokenSource();

            return Task.Run(async () =>
            {
                await ReadAsync();

                var generatedPoints = new List<WaveformPoint>();
                float pointsPerGeneratedPoint = (float)points.Count / pointCount;

                for (float i = 0; i < points.Count; i += pointsPerGeneratedPoint)
                {
                    int endIndex = (int)Math.Min(points.Count, Math.Ceiling(i + pointsPerGeneratedPoint));

                    var point = new WaveformPoint(Channels);
                    for (int j = (int)i; j < endIndex; j++)
                    {
                        for (int c = 0; c < Channels; c++)
                            point.Amplitude[c] += points[j].Amplitude[c];
                    }

                    // Mean
                    for (int c = 0; c < Channels; c++)
                        point.Amplitude[c] /= endIndex - i;

                    generatedPoints.Add(point);
                }

                return new Waveform
                {
                    points = generatedPoints,
                    Channels = Channels
                };

            }, generationSource.Token);
        }

        /// <summary>
        /// Cancels a <see cref="GenerateAsync(int)"/> task.
        /// </summary>
        public void CancelGenerationAsync()
        {
            generationSource?.Cancel();
            generationSource?.Dispose();
        }

        public void Dispose()
        {
            readSource?.Cancel();
            readSource?.Dispose();

            CancelGenerationAsync();
        }
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