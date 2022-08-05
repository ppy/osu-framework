// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using osu.Framework.Allocation;
using osu.Framework.Audio.Track;
using osu.Framework.Graphics.Colour;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Rendering;
using osu.Framework.Graphics.Rendering.Vertices;
using osu.Framework.Graphics.Shaders;
using osu.Framework.Graphics.Textures;
using osu.Framework.Layout;
using osu.Framework.Logging;
using osu.Framework.Utils;
using osuTK;
using osuTK.Graphics;

namespace osu.Framework.Graphics.Audio
{
    /// <summary>
    /// Visualises the waveform for an audio stream.
    /// </summary>
    public class WaveformGraph : Drawable
    {
        private IShader shader;
        private Texture texture;

        [BackgroundDependencyLoader]
        private void load(ShaderManager shaders, IRenderer renderer)
        {
            shader = shaders.Load(VertexShaderDescriptor.TEXTURE_2, FragmentShaderDescriptor.TEXTURE_ROUNDED);
            texture = renderer.WhitePixel;
        }

        private float resolution = 1;

        /// <summary>
        /// Gets or sets the amount of <see cref="Framework.Audio.Track.Waveform.Point"/>'s displayed relative to <see cref="Drawable.DrawWidth">DrawWidth</see>.
        /// </summary>
        public float Resolution
        {
            get => resolution;
            set
            {
                if (value < 0)
                    throw new ArgumentOutOfRangeException(nameof(value));

                if (resolution == value)
                    return;

                resolution = value;
                resampledPointCount = null;
                queueRegeneration();
            }
        }

        private Waveform waveform;

        /// <summary>
        /// The <see cref="Framework.Audio.Track.Waveform"/> to display.
        /// </summary>
        public Waveform Waveform
        {
            get => waveform;
            set
            {
                if (waveform == value)
                    return;

                waveform = value;
                resampledPointCount = null;
                queueRegeneration();
            }
        }

        private Color4 baseColour = Color4.White;

        /// <summary>
        /// The base colour of the graph for frequencies that don't fall into the predefined low/mid/high buckets.
        /// Also serves as the default value of <see cref="LowColour"/>, <see cref="MidColour"/>, and <see cref="HighColour"/>.
        /// </summary>
        public Color4 BaseColour
        {
            get => baseColour;
            set
            {
                if (baseColour == value)
                    return;

                baseColour = value;

                Invalidate(Invalidation.DrawNode);
            }
        }

        private Color4? lowColour;

        /// <summary>
        /// The colour which low-range frequencies should be colourised with.
        /// May be null for this frequency range to not be colourised.
        /// </summary>
        public Color4? LowColour
        {
            get => lowColour;
            set
            {
                if (lowColour == value)
                    return;

                lowColour = value;

                Invalidate(Invalidation.DrawNode);
            }
        }

        private Color4? midColour;

        /// <summary>
        /// The colour which mid-range frequencies should be colourised with.
        /// May be null for this frequency range to not be colourised.
        /// </summary>
        public Color4? MidColour
        {
            get => midColour;
            set
            {
                if (midColour == value)
                    return;

                midColour = value;

                Invalidate(Invalidation.DrawNode);
            }
        }

        private Color4? highColour;

        /// <summary>
        /// The colour which high-range frequencies should be colourised with.
        /// May be null for this frequency range to not be colourised.
        /// </summary>
        public Color4? HighColour
        {
            get => highColour;
            set
            {
                if (highColour == value)
                    return;

                highColour = value;

                Invalidate(Invalidation.DrawNode);
            }
        }

        protected override bool OnInvalidate(Invalidation invalidation, InvalidationSource source)
        {
            bool result = base.OnInvalidate(invalidation, source);

            if ((invalidation & Invalidation.RequiredParentSizeToFit) > 0)
            {
                // We should regenerate when `Scale` changed, but not `Position`.
                // Unfortunately both of these are grouped together in `MiscGeometry`.
                queueRegeneration();
            }

            return result;
        }

        private CancellationTokenSource cancelSource = new CancellationTokenSource();

        private long resampledVersion;
        private List<Waveform.Point> resampledPoints;
        private int? resampledPointCount;
        private int resampledChannels;
        private double resampledMaxHighIntensity;
        private double resampledMaxMidIntensity;
        private double resampledMaxLowIntensity;

        private void queueRegeneration() => Scheduler.AddOnce(() =>
        {
            int requiredPointCount = (int)Math.Max(0, Math.Ceiling(DrawWidth * Scale.X) * Resolution);
            if (requiredPointCount == resampledPointCount && !cancelSource.IsCancellationRequested)
                return;

            cancelGeneration();

            var originalWaveform = Waveform;

            if (originalWaveform == null)
                return;

            // This should be set before the operation is run.
            // It will stop unnecessary task churn if invalidation is occuring often.
            resampledPointCount = requiredPointCount;

            cancelSource = new CancellationTokenSource();
            var token = cancelSource.Token;

            Task.Run(async () =>
            {
                var resampled = await originalWaveform.GenerateResampledAsync(requiredPointCount, token).ConfigureAwait(false);

                int originalPointCount = (await originalWaveform.GetPointsAsync().ConfigureAwait(false)).Count;

                Logger.Log($"Waveform resampled with {requiredPointCount:N0} points (original {originalPointCount:N0})...");

                var points = await resampled.GetPointsAsync().ConfigureAwait(false);
                int channels = await resampled.GetChannelsAsync().ConfigureAwait(false);

                double maxHighIntensity = points.Count > 0 ? points.Max(p => p.HighIntensity) : 0;
                double maxMidIntensity = points.Count > 0 ? points.Max(p => p.MidIntensity) : 0;
                double maxLowIntensity = points.Count > 0 ? points.Max(p => p.LowIntensity) : 0;

                Schedule(() =>
                {
                    if (token.IsCancellationRequested)
                        return;

                    resampledPoints = points;
                    resampledChannels = channels;
                    resampledMaxHighIntensity = maxHighIntensity;
                    resampledMaxMidIntensity = maxMidIntensity;
                    resampledMaxLowIntensity = maxLowIntensity;
                    resampledVersion = InvalidationID;

                    OnWaveformRegenerated(resampled);
                    Invalidate(Invalidation.DrawNode);
                });
            }, token);
        });

        private void cancelGeneration()
        {
            cancelSource?.Cancel();
            cancelSource?.Dispose();
            cancelSource = null;
        }

        /// <summary>
        /// Invoked when the waveform has been regenerated.
        /// </summary>
        /// <param name="waveform">The new <see cref="Waveform"/> to be displayed.</param>
        protected virtual void OnWaveformRegenerated(Waveform waveform)
        {
        }

        protected override DrawNode CreateDrawNode() => new WaveformDrawNode(this);

        protected override void Dispose(bool isDisposing)
        {
            base.Dispose(isDisposing);
            cancelGeneration();
        }

        private class WaveformDrawNode : DrawNode
        {
            private IShader shader;
            private Texture texture;

            private readonly List<Waveform.Point> points = new List<Waveform.Point>();

            private Vector2 drawSize;
            private int channels;

            private long version;

            private Color4 baseColour;
            private Color4 lowColour;
            private Color4 midColour;
            private Color4 highColour;

            private double highMax;
            private double midMax;
            private double lowMax;

            protected new WaveformGraph Source => (WaveformGraph)base.Source;

            public WaveformDrawNode(WaveformGraph source)
                : base(source)
            {
            }

            public override void ApplyState()
            {
                base.ApplyState();

                shader = Source.shader;
                texture = Source.texture;
                drawSize = Source.DrawSize;

                baseColour = Source.baseColour;

                lowColour = Source.lowColour ?? baseColour;
                midColour = Source.midColour ?? baseColour;
                highColour = Source.highColour ?? baseColour;

                if (Source.resampledVersion != version)
                {
                    points.Clear();

                    if (Source.resampledPoints != null)
                        points.AddRange(Source.resampledPoints);

                    channels = Source.resampledChannels;

                    highMax = Source.resampledMaxHighIntensity;
                    midMax = Source.resampledMaxMidIntensity;
                    lowMax = Source.resampledMaxLowIntensity;

                    version = Source.resampledVersion;
                }
            }

            private IVertexBatch<TexturedVertex2D> vertexBatch;

            public override void Draw(IRenderer renderer)
            {
                base.Draw(renderer);

                if (texture?.Available != true || points == null || points.Count == 0)
                    return;

                vertexBatch ??= renderer.CreateQuadBatch<TexturedVertex2D>(1000, 10);

                shader.Bind();
                texture.Bind();

                Vector2 localInflationAmount = new Vector2(0, 1) * DrawInfo.MatrixInverse.ExtractScale().Xy;

                // We're dealing with a _large_ number of points, so we need to optimise the quadToDraw * drawInfo.Matrix multiplications below
                // for points that are going to be masked out anyway. This allows for higher resolution graphs at larger scales with virtually no performance loss.
                // Since the points are generated in the local coordinate space, we need to convert the screen space masking quad coordinates into the local coordinate space
                RectangleF localMaskingRectangle = (Quad.FromRectangle(renderer.CurrentMaskingInfo.ScreenSpaceAABB) * DrawInfo.MatrixInverse).AABBFloat;

                float separation = drawSize.X / (points.Count - 1);

                for (int i = 0; i < points.Count - 1; i++)
                {
                    float leftX = i * separation;
                    float rightX = (i + 1) * separation;

                    if (rightX < localMaskingRectangle.Left)
                        continue;

                    if (leftX > localMaskingRectangle.Right)
                        break; // X is always increasing

                    Color4 frequencyColour = baseColour;

                    // colouring is applied in the order of interest to a viewer.
                    frequencyColour = Interpolation.ValueAt(points[i].MidIntensity / midMax, frequencyColour, midColour, 0, 1);
                    // high end (cymbal) can help find beat, so give it priority over mids.
                    frequencyColour = Interpolation.ValueAt(points[i].HighIntensity / highMax, frequencyColour, highColour, 0, 1);
                    // low end (bass drum) is generally the best visual aid for beat matching, so give it priority over high/mid.
                    frequencyColour = Interpolation.ValueAt(points[i].LowIntensity / lowMax, frequencyColour, lowColour, 0, 1);

                    ColourInfo finalColour = DrawColourInfo.Colour;
                    finalColour.ApplyChild(frequencyColour);

                    Quad quadToDraw;

                    switch (channels)
                    {
                        default:
                        case 2:
                        {
                            float height = drawSize.Y / 2;
                            quadToDraw = new Quad(
                                new Vector2(leftX, height - points[i].Amplitude[0] * height),
                                new Vector2(rightX, height - points[i + 1].Amplitude[0] * height),
                                new Vector2(leftX, height + points[i].Amplitude[1] * height),
                                new Vector2(rightX, height + points[i + 1].Amplitude[1] * height)
                            );
                            break;
                        }

                        case 1:
                        {
                            quadToDraw = new Quad(
                                new Vector2(leftX, drawSize.Y - points[i].Amplitude[0] * drawSize.Y),
                                new Vector2(rightX, drawSize.Y - points[i + 1].Amplitude[0] * drawSize.Y),
                                new Vector2(leftX, drawSize.Y),
                                new Vector2(rightX, drawSize.Y)
                            );
                            break;
                        }
                    }

                    quadToDraw *= DrawInfo.Matrix;

                    if (quadToDraw.Size.X != 0 && quadToDraw.Size.Y != 0)
                        renderer.DrawQuad(texture, quadToDraw, finalColour, null, vertexBatch.AddAction, Vector2.Divide(localInflationAmount, quadToDraw.Size));
                }

                shader.Unbind();
            }

            protected override void Dispose(bool isDisposing)
            {
                base.Dispose(isDisposing);

                vertexBatch?.Dispose();
            }
        }
    }
}
