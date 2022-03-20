// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using osu.Framework.Allocation;
using osu.Framework.Audio.Track;
using osu.Framework.Extensions;
using osu.Framework.Graphics.Batches;
using osu.Framework.Graphics.Colour;
using osu.Framework.Graphics.OpenGL.Vertices;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Shaders;
using osu.Framework.Graphics.Textures;
using osuTK;
using osu.Framework.Graphics.OpenGL;
using osu.Framework.Layout;
using osu.Framework.Utils;
using osu.Framework.Threading;
using osuTK.Graphics;
using RectangleF = osu.Framework.Graphics.Primitives.RectangleF;

namespace osu.Framework.Graphics.Audio
{
    /// <summary>
    /// Visualises the waveform for an audio stream.
    /// </summary>
    public class WaveformGraph : Drawable
    {
        private IShader shader;
        private readonly Texture texture;

        public WaveformGraph()
        {
            texture = Texture.WhitePixel;
        }

        [BackgroundDependencyLoader]
        private void load(ShaderManager shaders)
        {
            shader = shaders.Load(VertexShaderDescriptor.TEXTURE_2, FragmentShaderDescriptor.TEXTURE_ROUNDED);
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
                generate();
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
                generate();
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
                generate();
                result = true;
            }

            return result;
        }

        private CancellationTokenSource cancelSource = new CancellationTokenSource();
        private ScheduledDelegate scheduledGenerate;

        private List<Waveform.Point> resampledPoints;
        private int resampledChannels;
        private double resampledMaxHighIntensity;
        private double resampledMaxMidIntensity;
        private double resampledMaxLowIntensity;

        private void generate()
        {
            scheduledGenerate?.Cancel();
            cancelGeneration();

            if (Waveform == null)
                return;

            scheduledGenerate = Schedule(() =>
            {
                cancelSource = new CancellationTokenSource();
                var token = cancelSource.Token;

                Waveform.GenerateResampledAsync((int)Math.Max(0, Math.Ceiling(DrawWidth * Scale.X) * Resolution), token).ContinueWith(task =>
                {
                    var resampled = task.GetResultSafely();

                    var points = resampled.GetPoints();
                    int channels = resampled.GetChannels();
                    double maxHighIntensity = points.Count > 0 ? points.Max(p => p.HighIntensity) : 0;
                    double maxMidIntensity = points.Count > 0 ? points.Max(p => p.MidIntensity) : 0;
                    double maxLowIntensity = points.Count > 0 ? points.Max(p => p.LowIntensity) : 0;

                    Schedule(() =>
                    {
                        resampledPoints = points;
                        resampledChannels = channels;
                        resampledMaxHighIntensity = maxHighIntensity;
                        resampledMaxMidIntensity = maxMidIntensity;
                        resampledMaxLowIntensity = maxLowIntensity;

                        OnWaveformRegenerated(resampled);

                        Invalidate(Invalidation.DrawNode);
                    });
                }, token);
            });
        }

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

                points.Clear();

                if (Source.resampledPoints != null)
                    points.AddRange(Source.resampledPoints);

                channels = Source.resampledChannels;
                highMax = Source.resampledMaxHighIntensity;
                midMax = Source.resampledMaxMidIntensity;
                lowMax = Source.resampledMaxLowIntensity;

                baseColour = Source.baseColour;
                lowColour = Source.lowColour ?? baseColour;
                midColour = Source.midColour ?? baseColour;
                highColour = Source.highColour ?? baseColour;
            }

            private readonly QuadBatch<TexturedVertex2D> vertexBatch = new QuadBatch<TexturedVertex2D>(1000, 10);

            public override void Draw(Action<TexturedVertex2D> vertexAction)
            {
                base.Draw(vertexAction);

                if (texture?.Available != true || points == null || points.Count == 0)
                    return;

                shader.Bind();
                texture.TextureGL.Bind();

                Vector2 localInflationAmount = new Vector2(0, 1) * DrawInfo.MatrixInverse.ExtractScale().Xy;

                // We're dealing with a _large_ number of points, so we need to optimise the quadToDraw * drawInfo.Matrix multiplications below
                // for points that are going to be masked out anyway. This allows for higher resolution graphs at larger scales with virtually no performance loss.
                // Since the points are generated in the local coordinate space, we need to convert the screen space masking quad coordinates into the local coordinate space
                RectangleF localMaskingRectangle = (Quad.FromRectangle(GLWrapper.CurrentMaskingInfo.ScreenSpaceAABB) * DrawInfo.MatrixInverse).AABBFloat;

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
                    DrawQuad(texture, quadToDraw, finalColour, null, vertexBatch.AddAction, Vector2.Divide(localInflationAmount, quadToDraw.Size));
                }

                shader.Unbind();
            }

            protected override void Dispose(bool isDisposing)
            {
                base.Dispose(isDisposing);

                vertexBatch.Dispose();
            }
        }
    }
}
