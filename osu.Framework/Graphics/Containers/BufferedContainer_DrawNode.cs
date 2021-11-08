// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using osu.Framework.Graphics.OpenGL;
using osu.Framework.Graphics.OpenGL.Buffers;
using osuTK;
using osuTK.Graphics;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Shaders;
using System;
using osu.Framework.Graphics.Colour;
using osu.Framework.Utils;
using osuTK.Graphics.ES30;

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

            private long updateVersion;

            private IShader blurShader;

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

                blurShader = Source.blurShader;
            }

            protected override long GetDrawVersion() => updateVersion;

            protected override void PopulateContents()
            {
                base.PopulateContents();

                if (blurRadius.X > 0 || blurRadius.Y > 0)
                {
                    GLWrapper.PushScissorState(false);

                    if (blurRadius.X > 0) drawBlurredFrameBuffer(blurRadius.X, blurSigma.X, blurRotation);
                    if (blurRadius.Y > 0) drawBlurredFrameBuffer(blurRadius.Y, blurSigma.Y, blurRotation + 90);

                    GLWrapper.PopScissorState();
                }
            }

            protected override void DrawContents()
            {
                if (drawOriginal && effectPlacement == EffectPlacement.InFront)
                    base.DrawContents();

                GLWrapper.SetBlend(effectBlending);

                ColourInfo finalEffectColour = DrawColourInfo.Colour;
                finalEffectColour.ApplyChild(effectColour);

                DrawFrameBuffer(SharedData.CurrentEffectBuffer, DrawRectangle, finalEffectColour);

                if (drawOriginal && effectPlacement == EffectPlacement.Behind)
                    base.DrawContents();
            }

            private void drawBlurredFrameBuffer(int kernelRadius, float sigma, float blurRotation)
            {
                FrameBuffer current = SharedData.CurrentEffectBuffer;
                FrameBuffer target = SharedData.GetNextEffectBuffer();

                GLWrapper.SetBlend(BlendingParameters.None);

                using (BindFrameBuffer(target))
                {
                    blurShader.GetUniform<int>(@"g_Radius").UpdateValue(ref kernelRadius);
                    blurShader.GetUniform<float>(@"g_Sigma").UpdateValue(ref sigma);

                    Vector2 size = current.Size;
                    blurShader.GetUniform<Vector2>(@"g_TexSize").UpdateValue(ref size);

                    float radians = -MathUtils.DegreesToRadians(blurRotation);
                    Vector2 blur = new Vector2(MathF.Cos(radians), MathF.Sin(radians));
                    blurShader.GetUniform<Vector2>(@"g_BlurDirection").UpdateValue(ref blur);

                    blurShader.Bind();
                    DrawFrameBuffer(current, new RectangleF(0, 0, current.Texture.Width, current.Texture.Height), ColourInfo.SingleColour(Color4.White));
                    blurShader.Unbind();
                }
            }

            public List<DrawNode> Children
            {
                get => Child.Children;
                set => Child.Children = value;
            }

            public bool AddChildDrawNodes => RequiresRedraw;
        }

        private class BufferedContainerDrawNodeSharedData : BufferedDrawNodeSharedData
        {
            public BufferedContainerDrawNodeSharedData(RenderbufferInternalFormat[] formats, bool pixelSnapping, bool clipToRootNode)
                : base(2, formats, pixelSnapping, clipToRootNode)
            {
            }
        }
    }
}
