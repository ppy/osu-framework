// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System.Collections.Generic;
using osuTK;
using osuTK.Graphics;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Shaders;
using System;
using System.Runtime.InteropServices;
using osu.Framework.Graphics.Colour;
using osu.Framework.Graphics.Rendering;
using osu.Framework.Graphics.Shaders.Types;
using osu.Framework.Utils;

namespace osu.Framework.Graphics.Containers
{
    public partial class BufferedContainer<T>
    {
        private class BufferedContainerDrawNode : BufferedDrawNode, ICompositeDrawNode
        {
            protected new BufferedContainer<T> Source => (BufferedContainer<T>)base.Source;

            protected new CompositeDrawableDrawNode Child => (CompositeDrawableDrawNode)base.Child;

            private bool drawOriginal;
            private ColourInfo effectColour;
            private BlendingParameters effectBlending;
            private EffectPlacement effectPlacement;

            private Vector2 blurSigma;
            private Vector2I blurRadius;
            private float blurRotation;
            private float grayscaleStrength;
            private float verticalPerspective;

            private Vector2 perspectiveScale;
            private float perspectiveVerticalOffset;

            private long updateVersion;
            private IShader blurShader;
            private IShader grayscaleShader;
            private IShader perspectiveShader;

            // Reusable structs to avoid per-frame allocations
            private BlurParameters blurParameters;
            private GrayscaleParameters grayscaleParameters;
            private PerspectiveParameters perspectiveParameters;

            public BufferedContainerDrawNode(BufferedContainer<T> source, BufferedContainerDrawNodeSharedData sharedData)
                : base(source, new CompositeDrawableDrawNode(source), sharedData)
            {
            }

            public override void ApplyState()
            {
                base.ApplyState();

                updateVersion = Source.updateVersion;

                effectColour = Source.EffectColour;
                effectBlending = Source.DrawEffectBlending;
                effectPlacement = Source.EffectPlacement;

                drawOriginal = Source.DrawOriginal;
                blurSigma = Source.BlurSigma;
                blurRadius = new Vector2I(Blur.KernelSize(blurSigma.X), Blur.KernelSize(blurSigma.Y));
                blurRotation = Source.BlurRotation;
                grayscaleStrength = Source.GrayscaleStrength;
                verticalPerspective = Source.VerticalPerspective;

                perspectiveScale = Source.PerspectiveScale;
                perspectiveVerticalOffset = Source.PerspectiveVerticalOffset;

                blurShader = Source.blurShader;
                grayscaleShader = Source.grayscaleShader;
                perspectiveShader = Source.perspectiveShader;
            }

            protected override long GetDrawVersion() => updateVersion;

            protected override void PopulateContents(IRenderer renderer)
            {
                base.PopulateContents(renderer);

                if (blurRadius.X > 0 || blurRadius.Y > 0 || grayscaleStrength > 0)
                {
                    renderer.PushScissorState(false);

                    if (blurRadius.X > 0) drawBlurredFrameBuffer(renderer, blurRadius.X, blurSigma.X, blurRotation);
                    if (blurRadius.Y > 0) drawBlurredFrameBuffer(renderer, blurRadius.Y, blurSigma.Y, blurRotation + 90);
                    if (grayscaleStrength > 0) drawGrayscaleFrameBuffer(renderer, grayscaleStrength);

                    renderer.PopScissorState();
                }
            }

            protected override void DrawContents(IRenderer renderer)
            {
                if (drawOriginal && effectPlacement == EffectPlacement.InFront)
                    base.DrawContents(renderer);

                renderer.SetBlend(effectBlending);

                ColourInfo finalEffectColour = DrawColourInfo.Colour;
                finalEffectColour.ApplyChild(effectColour);

                drawPerspectiveFrameBuffer(renderer, SharedData.CurrentEffectBuffer, finalEffectColour);

                if (drawOriginal && effectPlacement == EffectPlacement.Behind)
                    base.DrawContents(renderer);
            }

            private void drawPerspectiveFrameBuffer(IRenderer renderer, IFrameBuffer frameBuffer, ColourInfo drawColour)
            {
                float strength = Math.Clamp(verticalPerspective, 0f, 0.95f);

                if (strength <= 0 || perspectiveShader == null)
                {
                    renderer.DrawFrameBuffer(frameBuffer, DrawRectangle, drawColour);
                    return;
                }

                using (var perspectiveParametersBuffer = renderer.CreateUniformBuffer<PerspectiveParameters>())
                {
                    perspectiveParameters.Scale = perspectiveScale;
                    perspectiveParameters.VerticalOffset = perspectiveVerticalOffset;
                    perspectiveParametersBuffer.Data = perspectiveParameters;

                    perspectiveShader.BindUniformBlock("m_PerspectiveParameters", perspectiveParametersBuffer);
                    perspectiveShader.Bind();
                    BindUniformResources(perspectiveShader, renderer);

                    renderer.DrawFrameBuffer(frameBuffer, DrawRectangle, drawColour);

                    perspectiveShader.Unbind();
                    Source.TextureShader?.Bind();
                    if (Source.TextureShader != null)
                        BindUniformResources(Source.TextureShader, renderer);
                }
            }

            private void drawBlurredFrameBuffer(IRenderer renderer, int kernelRadius, float sigma, float blurRotation)
            {
                IFrameBuffer current = SharedData.CurrentEffectBuffer;
                IFrameBuffer target = SharedData.GetNextEffectBuffer();

                renderer.SetBlend(BlendingParameters.None);

                using (BindFrameBuffer(target))
                {
                    float radians = float.DegreesToRadians(blurRotation);

                    using (var blurParametersBuffer = renderer.CreateUniformBuffer<BlurParameters>())
                    {
                        // Update reusable struct instead of allocating new one
                        blurParameters.Radius = kernelRadius;
                        blurParameters.Sigma = sigma;
                        blurParameters.TexSize = current.Size;
                        blurParameters.Direction = new Vector2(MathF.Cos(radians), MathF.Sin(radians));

                        blurParametersBuffer.Data = blurParameters;

                        blurShader.BindUniformBlock("m_BlurParameters", blurParametersBuffer);
                        blurShader.Bind();
                        renderer.DrawFrameBuffer(current, new RectangleF(0, 0, current.Texture.Width, current.Texture.Height), ColourInfo.SingleColour(Color4.White));
                        blurShader.Unbind();
                    }
                }
            }

            private void drawGrayscaleFrameBuffer(IRenderer renderer, float strength)
            {
                IFrameBuffer current = SharedData.CurrentEffectBuffer;
                IFrameBuffer target = SharedData.GetNextEffectBuffer();

                renderer.SetBlend(BlendingParameters.None);

                using (BindFrameBuffer(target))
                {
                    using (var grayscaleParametersBuffer = renderer.CreateUniformBuffer<GrayscaleParameters>())
                    {
                        // Update reusable struct instead of allocating new one
                        grayscaleParameters.Strength = strength;
                        grayscaleParametersBuffer.Data = grayscaleParameters;

                        grayscaleShader.BindUniformBlock("m_GrayscaleParameters", grayscaleParametersBuffer);
                        grayscaleShader.Bind();
                        renderer.DrawFrameBuffer(current, new RectangleF(0, 0, current.Texture.Width, current.Texture.Height), ColourInfo.SingleColour(Color4.White));
                        grayscaleShader.Unbind();
                    }
                }
            }

            public List<DrawNode> Children
            {
                get => Child.Children;
                set => Child.Children = value;
            }

            public bool AddChildDrawNodes => RequiresRedraw;

            [StructLayout(LayoutKind.Sequential, Pack = 1)]
            private record struct BlurParameters
            {
                public UniformVector2 TexSize;
                public UniformInt Radius;
                public UniformFloat Sigma;
                public UniformVector2 Direction;
                private readonly UniformPadding8 pad1;
            }

            [StructLayout(LayoutKind.Sequential, Pack = 1)]
            private record struct GrayscaleParameters
            {
                public UniformFloat Strength;
                private readonly UniformPadding12 pad1;
            }

            [StructLayout(LayoutKind.Sequential, Pack = 1)]
            private record struct PerspectiveParameters
            {
                public UniformVector2 Scale;       // x = TopHeightScale, y = TopWidthScale
                public UniformFloat VerticalOffset;
                private readonly UniformFloat pad;
            }
        }

        private class BufferedContainerDrawNodeSharedData : BufferedDrawNodeSharedData
        {
            public BufferedContainerDrawNodeSharedData(RenderBufferFormat[] mainBufferFormats, bool pixelSnapping, bool clipToRootNode)
                : base(2, mainBufferFormats, pixelSnapping, clipToRootNode)
            {
            }
        }
    }
}
