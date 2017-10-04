// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Collections.Generic;

namespace osu.Framework.Audio.Track
{
    /// <summary>
    /// Procsses audio sample data such that it can then be consumed to generate waveform plots of the audio.
    /// </summary>
    public class Waveform
    {
        /// <summary>
        /// <see cref="WaveformPoint"/>s are initially generated to a 1ms resolution to cover most use cases.
        /// </summary>
        private const float resolution = 0.001f;

        /// <summary>
        /// Maximum number of <see cref="WaveformPoint"/>s that can be generated through <see cref="Generate(int)"/>.
        /// </summary>
        public int MaximumPoints => points.Count;

        /// <summary>
        /// The number of channels stored by <see cref="WaveformPoint"/>s.
        /// </summary>
        public readonly int Channels;

        /// <summary>
        /// List of all <see cref="WaveformPoint"/>s.
        /// </summary>
        private readonly List<WaveformPoint> points = new List<WaveformPoint>();

        public Waveform(float[] sampleData, int frequency, int channels)
        {
            if (sampleData == null)
                return;

            if (frequency <= 0) throw new ArgumentOutOfRangeException(nameof(frequency));
            if (channels < 1) throw new ArgumentOutOfRangeException(nameof(channels));

            Channels = channels;

            // Each "point" is generated from a number of samples, each sample contains a number of channels
            int sampleDataPerPoint = (int)(frequency * resolution * channels);
            points.Capacity = sampleData.Length / sampleDataPerPoint;

            // Process a sequence of samples for each point
            for (int i = 0; i < sampleData.Length; i += sampleDataPerPoint)
            {
                int endIndex = Math.Min(sampleData.Length, i + sampleDataPerPoint);

                // Process each sample in the sequence
                var point = new WaveformPoint(channels);
                for (int j = i; j < endIndex; j += channels)
                {
                    // Process each channel in the sample
                    for (int c = 0; c < channels; c++)
                        point.Amplitude[c] = Math.Max(point.Amplitude[c], Math.Abs(sampleData[j + c]));
                }

                points.Add(point);
            }
        }

        /// <summary>
        /// Generates a set of points that can be used to plot the waveform.
        /// </summary>
        /// <param name="dataPoints">The number of points required. This must be smaller than <see cref="MaximumPoints"/>.</param>
        /// <returns>The list of points which approximate the waveform.</returns>
        public List<WaveformPoint> Generate(int dataPoints)
        {
            if (dataPoints < 0) throw new ArgumentOutOfRangeException(nameof(dataPoints));
            if (dataPoints == 0) return new List<WaveformPoint>();
            if (dataPoints > MaximumPoints) throw new ArgumentOutOfRangeException(nameof(dataPoints));

            var generatedPoints = new List<WaveformPoint>();
            int pointsPerGeneratedPoint = (int)Math.Ceiling((float)points.Count / dataPoints);

            for (int i = 0; i < points.Count; i += pointsPerGeneratedPoint)
            {
                int endIndex = Math.Min(points.Count, i + pointsPerGeneratedPoint);

                var point = new WaveformPoint(Channels);
                for (int j = i; j < endIndex; j++)
                {
                    for (int c = 0; c < Channels; c++)
                        point.Amplitude[c] += points[j].Amplitude[c];
                }

                // Mean
                for (int c = 0; c < Channels; c++)
                    point.Amplitude[c] /= endIndex - i;

                generatedPoints.Add(point);
            }

            return generatedPoints;
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
