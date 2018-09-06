// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using osu.Framework.Allocation;
using osu.Framework.Audio.Track;
using osu.Framework.Graphics.Batches;
using osu.Framework.Graphics.OpenGL.Vertices;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Shaders;
using osu.Framework.Graphics.Textures;
using OpenTK;
using osu.Framework.Graphics.OpenGL;
using osu.Framework.MathUtils;
using osu.Framework.Threading;
using OpenTK.Graphics;
using RectangleF = osu.Framework.Graphics.Primitives.RectangleF;

namespace osu.Framework.Graphics.Audio
{
    /// <summary>
    /// Visualises the waveform for an audio stream.
    /// </summary>
    public class WaveformGraph : Drawable
    {
        private Shader shader;
        private readonly Texture texture;

        public WaveformGraph()
        {
            texture = Texture.WhitePixel;
        }

        [BackgroundDependencyLoader]
        private void load(ShaderManager shaders)
        {
            shader = shaders?.Load(VertexShaderDescriptor.TEXTURE_2, FragmentShaderDescriptor.TEXTURE_ROUNDED);
        }

        private float resolution = 1;

        /// <summary>
        /// Gets or sets the amount of <see cref="WaveformPoint"/>'s displayed relative to <see cref="WaveformGraph.DrawWidth"/>.
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

        public override bool Invalidate(Invalidation invalidation = Invalidation.All, Drawable source = null, bool shallPropagate = true)
        {
            var result = base.Invalidate(invalidation, source, shallPropagate);

            if ((invalidation & Invalidation.RequiredParentSizeToFit) > 0)
                generate();

            return result;
        }

        private CancellationTokenSource cancelSource = new CancellationTokenSource();
        private ScheduledDelegate scheduledGenerate;

        private Waveform generatedWaveform;

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

                Waveform.GenerateResampledAsync((int)Math.Max(0, Math.Ceiling(DrawWidth * Scale.X) * Resolution), token).ContinueWith(w =>
                {
                    generatedWaveform = w.Result;
                    Schedule(() => Invalidate(Invalidation.DrawNode));
                }, token);
            });
        }

        private void cancelGeneration()
        {
            cancelSource?.Cancel();
            cancelSource?.Dispose();
            cancelSource = null;
        }

        private readonly WaveformDrawNodeSharedData sharedData = new WaveformDrawNodeSharedData();
        protected override DrawNode CreateDrawNode() => new WaveformDrawNode();

        protected override void ApplyDrawNode(DrawNode node)
        {
            var n = (WaveformDrawNode)node;

            n.Shader = shader;
            n.Texture = texture;
            n.DrawSize = DrawSize;
            n.Shared = sharedData;
            n.Points = generatedWaveform?.GetPoints();
            n.Channels = generatedWaveform?.GetChannels() ?? 0;
            n.LowColour = lowColour ?? DrawColourInfo.Colour;
            n.MidColour = midColour ?? DrawColourInfo.Colour;
            n.HighColour = highColour ?? DrawColourInfo.Colour;

            base.ApplyDrawNode(node);
        }

        protected override void Dispose(bool isDisposing)
        {
            base.Dispose(isDisposing);
            cancelGeneration();
        }

        private class WaveformDrawNodeSharedData
        {
            public readonly QuadBatch<TexturedVertex2D> VertexBatch = new QuadBatch<TexturedVertex2D>(1000, 10);
        }

        private class WaveformDrawNode : DrawNode
        {
            public Shader Shader;
            public Texture Texture;

            public WaveformDrawNodeSharedData Shared;

            public Vector2 DrawSize;
            public int Channels;

            public Color4 LowColour;
            public Color4 MidColour;
            public Color4 HighColour;

            private IReadOnlyList<WaveformPoint> points;

            private double highMax;
            private double midMax;
            private double lowMax;

            public IReadOnlyList<WaveformPoint> Points
            {
                get { return points; }
                set
                {
                    points = value;

                    if (points?.Any() == true)
                    {
                        highMax = points.Max(p => p.HighIntensity);
                        midMax = points.Max(p => p.MidIntensity);
                        lowMax = points.Max(p => p.LowIntensity);
                    }
                }
            }

            public override void Draw(Action<TexturedVertex2D> vertexAction)
            {
                base.Draw(vertexAction);

                if (points == null || points.Count == 0)
                    return;

                Shader.Bind();
                Texture.TextureGL.Bind();

                Vector2 localInflationAmount = new Vector2(0, 1) * DrawInfo.MatrixInverse.ExtractScale().Xy;

                // We're dealing with a _large_ number of points, so we need to optimise the quadToDraw * drawInfo.Matrix multiplications below
                // for points that are going to be masked out anyway. This allows for higher resolution graphs at larger scales with virtually no performance loss.
                // Since the points are generated in the local coordinate space, we need to convert the screen space masking quad coordinates into the local coordinate space
                RectangleF localMaskingRectangle = (Quad.FromRectangle(GLWrapper.CurrentMaskingInfo.ScreenSpaceAABB) * DrawInfo.MatrixInverse).AABBFloat;

                float separation = DrawSize.X / (points.Count - 1);

                for (int i = 0; i < points.Count - 1; i++)
                {
                    float leftX = i * separation;
                    float rightX = (i + 1) * separation;

                    if (rightX < localMaskingRectangle.Left)
                        continue;
                    if (leftX > localMaskingRectangle.Right)
                        break; // X is always increasing

                    Color4 colour = DrawColourInfo.Colour;

                    // colouring is applied in the order of interest to a viewer.
                    colour = Interpolation.ValueAt(points[i].MidIntensity / midMax, colour, MidColour, 0, 1);
                    // high end (cymbal) can help find beat, so give it priority over mids.
                    colour = Interpolation.ValueAt(points[i].HighIntensity / highMax, colour, HighColour, 0, 1);
                    // low end (bass drum) is generally the best visual aid for beat matching, so give it priority over high/mid.
                    colour = Interpolation.ValueAt(points[i].LowIntensity / lowMax, colour, LowColour, 0, 1);

                    Quad quadToDraw;

                    switch (Channels)
                    {
                        default:
                        case 2:
                        {
                            float height = DrawSize.Y / 2;
                            quadToDraw = new Quad(
                                new Vector2(leftX, height - points[i].Amplitude[0] * height),
                                new Vector2(rightX, height - points[i + 1].Amplitude[0] * height),
                                new Vector2(leftX, height + points[i].Amplitude[1] * height),
                                new Vector2(rightX, height + points[i + 1].Amplitude[1] * height)
                            );
                        }
                            break;
                        case 1:
                        {
                            quadToDraw = new Quad(
                                new Vector2(leftX, DrawSize.Y - points[i].Amplitude[0] * DrawSize.Y),
                                new Vector2(rightX, DrawSize.Y - points[i + 1].Amplitude[0] * DrawSize.Y),
                                new Vector2(leftX, DrawSize.Y),
                                new Vector2(rightX, DrawSize.Y)
                            );
                            break;
                        }
                    }

                    quadToDraw *= DrawInfo.Matrix;
                    Texture.DrawQuad(quadToDraw, colour, null, Shared.VertexBatch.AddAction, Vector2.Divide(localInflationAmount, quadToDraw.Size));
                }

                Shader.Unbind();
            }
        }
    }
}
