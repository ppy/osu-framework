// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Collections.Generic;

namespace osu.Framework.Audio.Track
{
    /// <summary>
    /// Stores data about the samples of a <see cref="Track"/> that can be used to generate points for waveform plots of the audio.
    /// </summary>
    public class Waveform
    {
        /// <summary>
        /// Points are initially generated to a 1ms resolution to cover most use cases.
        /// </summary>
        private const float resolution = 0.001f;

        /// <summary>
        /// Maximum number of <see cref="WaveformPoint"/>s that are plottable.
        /// </summary>
        public int TotalPoints => points.Count;

        /// <summary>
        /// The number of channels that are plottable.
        /// </summary>
        public readonly int Channels;

        /// <summary>
        /// List of all plottable points.
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
        /// Generates a set of points that can be used to plot a waveform of the track's audio.
        /// </summary>
        /// <param name="dataPoints">The number of points required. This must be smaller than <see cref="TotalPoints"/>.</param>
        /// <returns>The list of points.</returns>
        public List<WaveformPoint> Generate(int dataPoints)
        {
            if (dataPoints < 0) throw new ArgumentOutOfRangeException(nameof(dataPoints));
            if (dataPoints == 0) return new List<WaveformPoint>();
            if (dataPoints > TotalPoints) throw new ArgumentOutOfRangeException(nameof(dataPoints));

            List<WaveformPoint> generatedPoints = new List<WaveformPoint>();
            int pointsPerGeneratedPoint = (int)Math.Ceiling((float)points.Count / dataPoints);

            for (int i = 0; i < points.Count; i += pointsPerGeneratedPoint)
            {
                int endIndex = (int)Math.Min(points.Count, i + pointsPerGeneratedPoint);

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

    public struct WaveformPoint
    {
        public readonly float[] Amplitude;

        public WaveformPoint(int channels)
        {
            Amplitude = new float[channels];
        }
    }
}
