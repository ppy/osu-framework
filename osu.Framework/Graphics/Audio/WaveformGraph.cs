// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Collections.Generic;
using System.Threading;
using osu.Framework.Allocation;
using osu.Framework.Audio.Track;
using osu.Framework.Graphics.Batches;
using osu.Framework.Graphics.Colour;
using osu.Framework.Graphics.OpenGL.Vertices;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Shaders;
using osu.Framework.Graphics.Textures;
using OpenTK;
using osu.Framework.Graphics.OpenGL;

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
            get { return resolution; }
            set
            {
                if (value < 0)
                    throw new ArgumentOutOfRangeException(nameof(value));

                if (resolution == value)
                    return;
                resolution = value;

                cancelGeneration();
            }
        }

        private Waveform waveform;
        /// <summary>
        /// The <see cref="Framework.Audio.Track.Waveform"/> to display.
        /// </summary>
        public Waveform Waveform
        {
            get { return waveform; }
            set
            {
                if (waveform == value)
                    return;

                waveform = value;

                cancelGeneration();
            }
        }

        public override bool Invalidate(Invalidation invalidation = Invalidation.All, Drawable source = null, bool shallPropagate = true)
        {
            var result = base.Invalidate(invalidation, source, shallPropagate);

            if ((invalidation & Invalidation.RequiredParentSizeToFit) > 0)
                cancelGeneration();

            return result;
        }

        protected override void Update()
        {
            base.Update();

            if (cancelSource == null)
                generate();
        }

        private CancellationTokenSource cancelSource = new CancellationTokenSource();

        private Waveform generatedWaveform;

        private void generate()
        {
            if (Waveform == null)
                return;

            cancelSource = new CancellationTokenSource();

            Waveform.GenerateResampledAsync((int)Math.Max(0, Math.Ceiling(DrawWidth * Scale.X) * Resolution), cancelSource.Token).ContinueWith(w =>
            {
                generatedWaveform = w.Result;
                Schedule(() => Invalidate(Invalidation.DrawNode));
            }, cancelSource.Token);
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

            public IReadOnlyList<WaveformPoint> Points;
            public Vector2 DrawSize;
            public int Channels;

            public override void Draw(Action<TexturedVertex2D> vertexAction)
            {
                base.Draw(vertexAction);

                if (Points == null || Points.Count == 0)
                    return;

                Shader.Bind();
                Texture.TextureGL.Bind();

                Vector2 localInflationAmount = new Vector2(0, 1) * DrawInfo.MatrixInverse.ExtractScale().Xy;

                // We're dealing with a _large_ number of points, so we need to optimise the quadToDraw * drawInfo.Matrix multiplications below
                // for points that are going to be masked out anyway. This allows for higher resolution graphs at larger scales with virtually no performance loss.
                // Since the points are generated in the local coordinate space, we need to convert the screen space masking quad coordinates into the local coordinate space
                RectangleF localMaskingRectangle = (Quad.FromRectangle(GLWrapper.CurrentMaskingInfo.ScreenSpaceAABB) * DrawInfo.MatrixInverse).AABBFloat;

                float separation = DrawSize.X / (Points.Count - 1);

                for (int i = 0; i < Points.Count - 1; i++)
                {
                    float leftX = i * separation;
                    float rightX = (i + 1) * separation;

                    if (rightX < localMaskingRectangle.Left)
                        continue;
                    if (leftX > localMaskingRectangle.Right)
                        break; // X is always increasing

                    ColourInfo colour = DrawInfo.Colour;
                    Quad quadToDraw;

                    switch (Channels)
                    {
                        default:
                        case 2:
                            {
                                float height = DrawSize.Y / 2;
                                quadToDraw = new Quad(
                                    new Vector2(leftX, height - Points[i].Amplitude[0] * height),
                                    new Vector2(rightX, height - Points[i + 1].Amplitude[0] * height),
                                    new Vector2(leftX, height + Points[i].Amplitude[1] * height),
                                    new Vector2(rightX, height + Points[i + 1].Amplitude[1] * height)
                                );
                            }
                            break;
                        case 1:
                            {
                                quadToDraw = new Quad(
                                    new Vector2(leftX, DrawSize.Y - Points[i].Amplitude[0] * DrawSize.Y),
                                    new Vector2(rightX, DrawSize.Y - Points[i + 1].Amplitude[0] * DrawSize.Y),
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
