// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using osu.Framework.Graphics.OpenGL;
using osu.Framework.Graphics.OpenGL.Buffers;
using osuTK;
using osuTK.Graphics.ES30;
using osuTK.Graphics;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Allocation;
using osu.Framework.Graphics.Shaders;
using System;
using osu.Framework.Graphics.Colour;
using osu.Framework.Graphics.OpenGL.Vertices;
using System.Diagnostics;
using osu.Framework.MathUtils;
using Vector2 = System.Numerics.Vector2;

namespace osu.Framework.Graphics.Containers
{
    public partial class BufferedContainer<T>
    {
        private class BufferedContainerDrawNode : CompositeDrawableDrawNode
        {
            protected new BufferedContainer<T> Source => (BufferedContainer<T>)base.Source;

            private bool drawOriginal;
            private Color4 backgroundColour;
            private ColourInfo effectColour;
            private BlendingParameters effectBlending;
            private EffectPlacement effectPlacement;

            private Vector2 blurSigma;
            private Vector2I blurRadius;
            private float blurRotation;

            private long updateVersion;

            private RectangleF screenSpaceDrawRectangle;
            private All filteringMode;

            private readonly List<RenderbufferInternalFormat> formats = new List<RenderbufferInternalFormat>();

            private IShader blurShader;

            private readonly BufferedContainerDrawNodeSharedData sharedData;

            public BufferedContainerDrawNode(BufferedContainer<T> source, BufferedContainerDrawNodeSharedData sharedData)
                : base(source)
            {
                this.sharedData = sharedData;
            }

            public override void ApplyState()
            {
                base.ApplyState();

                screenSpaceDrawRectangle = Source.ScreenSpaceDrawQuad.AABBFloat;
                filteringMode = Source.PixelSnapping ? All.Nearest : All.Linear;

                updateVersion = Source.updateVersion;
                backgroundColour = Source.BackgroundColour;

                BlendingParameters localEffectBlending = Source.EffectBlending;
                if (localEffectBlending.Mode == BlendingMode.Inherit)
                    localEffectBlending.Mode = Source.Blending.Mode;

                if (localEffectBlending.RGBEquation == BlendingEquation.Inherit)
                    localEffectBlending.RGBEquation = Source.Blending.RGBEquation;

                if (localEffectBlending.AlphaEquation == BlendingEquation.Inherit)
                    localEffectBlending.AlphaEquation = Source.Blending.AlphaEquation;

                effectColour = Source.EffectColour;
                effectBlending = localEffectBlending;
                effectPlacement = Source.EffectPlacement;

                drawOriginal = Source.DrawOriginal;
                blurSigma = Source.BlurSigma;
                blurRadius = new Vector2I(Blur.KernelSize(blurSigma.X), Blur.KernelSize(blurSigma.Y));
                blurRotation = Source.BlurRotation;

                formats.Clear();
                formats.AddRange(Source.attachedFormats);

                blurShader = Source.blurShader;

                // BufferedContainer overrides DrawColourInfo for children, but needs to be reset to draw ourselves
                DrawColourInfo = Source.baseDrawColourInfo;
            }

            public override bool AddChildDrawNodes => RequiresRedraw;

            /// <summary>
            /// Whether this <see cref="BufferedContainerDrawNode"/> should have its children re-drawn.
            /// </summary>
            public bool RequiresRedraw => updateVersion > sharedData.DrawVersion;

            private ValueInvokeOnDisposal establishFrameBufferViewport(Vector2 roundedSize)
            {
                // Disable masking for generating the frame buffer since masking will be re-applied
                // when actually drawing later on anyways. This allows more information to be captured
                // in the frame buffer and helps with cached buffers being re-used.
                RectangleI screenSpaceMaskingRect = new RectangleI((int)Math.Floor(screenSpaceDrawRectangle.X), (int)Math.Floor(screenSpaceDrawRectangle.Y), (int)roundedSize.X + 1,
                    (int)roundedSize.Y + 1);

                GLWrapper.PushMaskingInfo(new MaskingInfo
                {
                    ScreenSpaceAABB = screenSpaceMaskingRect,
                    MaskingRect = screenSpaceDrawRectangle,
                    ToMaskingSpace = Matrix3.Identity,
                    BlendRange = 1,
                    AlphaExponent = 1,
                }, true);

                // Match viewport to FrameBuffer such that we don't draw unnecessary pixels.
                GLWrapper.PushViewport(new RectangleI(0, 0, (int)roundedSize.X, (int)roundedSize.Y));

                return new ValueInvokeOnDisposal(returnViewport);
            }

            private void returnViewport()
            {
                GLWrapper.PopViewport();
                GLWrapper.PopMaskingInfo();
            }

            private ValueInvokeOnDisposal bindFrameBuffer(FrameBuffer frameBuffer, Vector2 requestedSize)
            {
                if (!frameBuffer.IsInitialized)
                    frameBuffer.Initialize(true, filteringMode);

                // These additional render buffers are only required if e.g. depth
                // or stencil information needs to also be stored somewhere.
                foreach (var f in formats)
                    frameBuffer.Attach(f);

                // This setter will also take care of allocating a texture of appropriate size within the framebuffer.
                frameBuffer.Size = requestedSize;

                frameBuffer.Bind();

                return new ValueInvokeOnDisposal(frameBuffer.Unbind);
            }

            private void drawFrameBufferToBackBuffer(FrameBuffer frameBuffer, RectangleF drawRectangle, ColourInfo colourInfo)
            {
                // The strange Y coordinate and Height are a result of OpenGL coordinate systems having Y grow upwards and not downwards.
                RectangleF textureRect = new RectangleF(0, frameBuffer.Texture.Height, frameBuffer.Texture.Width, -frameBuffer.Texture.Height);
                if (frameBuffer.Texture.Bind())
                    // Color was already applied by base.Draw(); no need to re-apply. Thus we use White here.
                    frameBuffer.Texture.DrawQuad(drawRectangle, textureRect, colourInfo);
            }

            private void drawChildren(Action<TexturedVertex2D> vertexAction, Vector2 frameBufferSize)
            {
                // Fill the frame buffer with drawn children
                using (bindFrameBuffer(currentFrameBuffer, frameBufferSize))
                {
                    // We need to draw children as if they were zero-based to the top-left of the texture.
                    // We can do this by adding a translation component to our (orthogonal) projection matrix.
                    GLWrapper.PushOrtho(screenSpaceDrawRectangle);

                    GLWrapper.Clear(new ClearInfo(backgroundColour));
                    base.Draw(vertexAction);

                    GLWrapper.PopOrtho();
                }
            }

            private void drawBlurredFrameBuffer(int kernelRadius, float sigma, float blurRotation)
            {
                FrameBuffer source = currentFrameBuffer;
                FrameBuffer target = advanceFrameBuffer();

                GLWrapper.SetBlend(new BlendingInfo(BlendingMode.None));

                using (bindFrameBuffer(target, source.Size))
                {
                    blurShader.GetUniform<int>(@"g_Radius").UpdateValue(ref kernelRadius);
                    blurShader.GetUniform<float>(@"g_Sigma").UpdateValue(ref sigma);

                    Vector2 size = source.Size;
                    blurShader.GetUniform<Vector2>(@"g_TexSize").UpdateValue(ref size);

                    float radians = -MathHelper.DegreesToRadians(blurRotation);
                    Vector2 blur = new Vector2((float)Math.Cos(radians), (float)Math.Sin(radians));
                    blurShader.GetUniform<Vector2>(@"g_BlurDirection").UpdateValue(ref blur);

                    blurShader.Bind();
                    drawFrameBufferToBackBuffer(source, new RectangleF(0, 0, source.Texture.Width, source.Texture.Height), ColourInfo.SingleColour(Color4.White));
                    blurShader.Unbind();
                }
            }

            private int currentFrameBufferIndex;
            private FrameBuffer currentFrameBuffer => sharedData.FrameBuffers[currentFrameBufferIndex];
            private FrameBuffer advanceFrameBuffer() => sharedData.FrameBuffers[currentFrameBufferIndex = (currentFrameBufferIndex + 1) % 2];

            /// <summary>
            /// Makes sure the first frame buffer is always the one we want to draw from.
            /// This saves us the need to sync the draw indices across draw node trees
            /// since the SharedData.FrameBuffers array is already shared.
            /// </summary>
            private void finalizeFrameBuffer()
            {
                if (currentFrameBufferIndex != 0)
                {
                    Trace.Assert(currentFrameBufferIndex == 1,
                        $"Only the first two framebuffers should be the last to be written to at the end of {nameof(Draw)}.");

                    FrameBuffer temp = sharedData.FrameBuffers[0];
                    sharedData.FrameBuffers[0] = sharedData.FrameBuffers[1];
                    sharedData.FrameBuffers[1] = temp;

                    currentFrameBufferIndex = 0;
                }
            }

            // Our effects will be drawn into framebuffers 0 and 1. If we want to preserve the originally
            // drawn children we need to put them in a separate buffer; in this case buffer 2. Otherwise,
            // we do not want to allocate a third buffer for nothing and hence we start with 0.
            private int originalIndex => drawOriginal && (blurRadius.X > 0 || blurRadius.Y > 0) ? 2 : 0;

            public override void Draw(Action<TexturedVertex2D> vertexAction)
            {
                currentFrameBufferIndex = originalIndex;

                Vector2 frameBufferSize = new Vector2((float)Math.Ceiling(screenSpaceDrawRectangle.Width), (float)Math.Ceiling(screenSpaceDrawRectangle.Height));

                if (RequiresRedraw)
                {
                    sharedData.DrawVersion = updateVersion;

                    using (establishFrameBufferViewport(frameBufferSize))
                    {
                        drawChildren(vertexAction, frameBufferSize);

                        // Blur post-processing in case a blur radius is defined.
                        if (blurRadius.X > 0 || blurRadius.Y > 0)
                        {
                            GL.Disable(EnableCap.ScissorTest);

                            if (blurRadius.X > 0) drawBlurredFrameBuffer(blurRadius.X, blurSigma.X, blurRotation);
                            if (blurRadius.Y > 0) drawBlurredFrameBuffer(blurRadius.Y, blurSigma.Y, blurRotation + 90);

                            GL.Enable(EnableCap.ScissorTest);
                        }
                    }

                    finalizeFrameBuffer();
                }

                RectangleF drawRectangle = filteringMode == All.Nearest
                    ? new RectangleF(screenSpaceDrawRectangle.X, screenSpaceDrawRectangle.Y, frameBufferSize.X, frameBufferSize.Y)
                    : screenSpaceDrawRectangle;

                Shader.Bind();

                if (drawOriginal && effectPlacement == EffectPlacement.InFront)
                {
                    GLWrapper.SetBlend(DrawColourInfo.Blending);
                    drawFrameBufferToBackBuffer(sharedData.FrameBuffers[originalIndex], drawRectangle, DrawColourInfo.Colour);
                }

                // Blit the final framebuffer to screen.
                GLWrapper.SetBlend(new BlendingInfo(effectBlending));

                ColourInfo finalEffectColour = DrawColourInfo.Colour;
                finalEffectColour.ApplyChild(effectColour);
                drawFrameBufferToBackBuffer(sharedData.FrameBuffers[0], drawRectangle, finalEffectColour);

                if (drawOriginal && effectPlacement == EffectPlacement.Behind)
                {
                    GLWrapper.SetBlend(DrawColourInfo.Blending);
                    drawFrameBufferToBackBuffer(sharedData.FrameBuffers[originalIndex], drawRectangle, DrawColourInfo.Colour);
                }

                Shader.Unbind();
            }
        }

        private class BufferedContainerDrawNodeSharedData : IDisposable
        {
            /// <summary>
            /// The <see cref="FrameBuffer"/>s to render to.
            /// These are used in a ping-pong manner to render effects <see cref="BufferedContainerDrawNode"/>.
            /// </summary>
            public readonly FrameBuffer[] FrameBuffers = new FrameBuffer[3];

            /// <summary>
            /// The version of drawn contents currently present in <see cref="FrameBuffers"/>.
            /// This should only be modified by <see cref="BufferedContainerDrawNode"/>.
            /// </summary>
            public long DrawVersion = -1;

            public BufferedContainerDrawNodeSharedData()
            {
                for (int i = 0; i < FrameBuffers.Length; i++)
                    FrameBuffers[i] = new FrameBuffer();
            }

            ~BufferedContainerDrawNodeSharedData()
            {
                dispose();
            }

            public void Dispose()
            {
                dispose();
                GC.SuppressFinalize(this);
            }

            private void dispose()
            {
                for (int i = 0; i < FrameBuffers.Length; i++)
                    FrameBuffers[i].Dispose();
            }
        }
    }
}
