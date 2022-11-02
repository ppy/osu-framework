// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using osu.Framework.Allocation;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Rendering;
using osu.Framework.Statistics;
using osuTK;
using osuTK.Graphics;

namespace osu.Framework.Graphics
{
    public class BufferedDrawNode : TexturedShaderDrawNode
    {
        protected new IBufferedDrawable Source => (IBufferedDrawable)base.Source;

        /// <summary>
        /// The child <see cref="DrawNode"/> which is used to populate the <see cref="IFrameBuffer"/>s with.
        /// </summary>
        protected DrawNode Child { get; private set; }

        /// <summary>
        /// Data shared amongst all <see cref="BufferedDrawNode"/>s, providing storage for <see cref="IFrameBuffer"/>s.
        /// </summary>
        protected readonly BufferedDrawNodeSharedData SharedData;

        /// <summary>
        /// Contains the colour and blending information of this <see cref="DrawNode"/>.
        /// </summary>
        protected new DrawColourInfo DrawColourInfo { get; private set; }

        protected RectangleF DrawRectangle { get; private set; }

        private Color4 backgroundColour;
        private RectangleF screenSpaceDrawRectangle;
        private Vector2 frameBufferScale;
        private Vector2 frameBufferSize;
        private IDrawable rootNodeCached;

        public BufferedDrawNode(IBufferedDrawable source, DrawNode child, BufferedDrawNodeSharedData sharedData)
            : base(source)
        {
            Child = child;
            SharedData = sharedData;
        }

        public override void ApplyState()
        {
            base.ApplyState();

            backgroundColour = Source.BackgroundColour;
            screenSpaceDrawRectangle = Source.ScreenSpaceDrawQuad.AABBFloat;
            DrawColourInfo = Source.FrameBufferDrawColour ?? new DrawColourInfo(Color4.White, base.DrawColourInfo.Blending);
            frameBufferScale = Source.FrameBufferScale;

            clipDrawRectangle();

            frameBufferSize = new Vector2(MathF.Ceiling(screenSpaceDrawRectangle.Width * frameBufferScale.X), MathF.Ceiling(screenSpaceDrawRectangle.Height * frameBufferScale.Y));
            DrawRectangle = SharedData.PixelSnapping
                ? new RectangleF(screenSpaceDrawRectangle.X, screenSpaceDrawRectangle.Y, frameBufferSize.X, frameBufferSize.Y)
                : screenSpaceDrawRectangle;

            Child.ApplyState();
        }

        /// <summary>
        /// Whether this <see cref="BufferedDrawNode"/> should be redrawn.
        /// </summary>
        protected bool RequiresRedraw => GetDrawVersion() > SharedData.DrawVersion;

        /// <summary>
        /// Retrieves the version of the state of this <see cref="DrawNode"/>.
        /// The <see cref="BufferedDrawNode"/> will only re-render if this version is greater than that of the rendered <see cref="IFrameBuffer"/>s.
        /// </summary>
        /// <remarks>
        /// By default, the <see cref="BufferedDrawNode"/> is re-rendered with every <see cref="DrawNode"/> invalidation.
        /// </remarks>
        /// <returns>A version representing this <see cref="DrawNode"/>'s state.</returns>
        protected virtual long GetDrawVersion() => InvalidationID;

        public sealed override void Draw(IRenderer renderer)
        {
            if (!SharedData.IsInitialised)
                SharedData.Initialise(renderer);

            if (RequiresRedraw)
            {
                FrameStatistics.Increment(StatisticsCounterType.FBORedraw);

                SharedData.ResetCurrentEffectBuffer();

                using (establishFrameBufferViewport(renderer))
                {
                    // Fill the frame buffer with drawn children
                    using (BindFrameBuffer(SharedData.MainBuffer))
                    {
                        // We need to draw children as if they were zero-based to the top-left of the texture.
                        // We can do this by adding a translation component to our (orthogonal) projection matrix.
                        renderer.PushOrtho(screenSpaceDrawRectangle);
                        renderer.Clear(new ClearInfo(backgroundColour));

                        Child.Draw(renderer);

                        renderer.PopOrtho();
                    }

                    PopulateContents(renderer);
                }

                SharedData.DrawVersion = GetDrawVersion();
            }

            var shader = GetAppropriateShader(renderer);

            shader.Bind();

            base.Draw(renderer);
            DrawContents(renderer);

            shader.Unbind();
        }

        /// <summary>
        /// Populates the contents of the effect buffers of <see cref="SharedData"/>.
        /// This is invoked after <see cref="Child"/> has been rendered to the main buffer.
        /// </summary>
        /// <param name="renderer"></param>
        protected virtual void PopulateContents(IRenderer renderer)
        {
        }

        /// <summary>
        /// Draws the applicable effect buffers of <see cref="SharedData"/> to the back buffer.
        /// </summary>
        /// <param name="renderer"></param>
        protected virtual void DrawContents(IRenderer renderer)
        {
            renderer.DrawFrameBuffer(SharedData.MainBuffer, DrawRectangle, DrawColourInfo.Colour);
        }

        /// <summary>
        /// Binds and initialises an <see cref="IFrameBuffer"/> if required.
        /// </summary>
        /// <param name="frameBuffer">The <see cref="IFrameBuffer"/> to bind.</param>
        /// <returns>A token that must be disposed upon finishing use of <paramref name="frameBuffer"/>.</returns>
        protected IDisposable BindFrameBuffer(IFrameBuffer frameBuffer)
        {
            // This setter will also take care of allocating a texture of appropriate size within the frame buffer.
            frameBuffer.Size = frameBufferSize;

            frameBuffer.Bind();

            return new ValueInvokeOnDisposal<IFrameBuffer>(frameBuffer, b => b.Unbind());
        }

        private IDisposable establishFrameBufferViewport(IRenderer renderer)
        {
            // Disable masking for generating the frame buffer since masking will be re-applied
            // when actually drawing later on anyways. This allows more information to be captured
            // in the frame buffer and helps with cached buffers being re-used.
            RectangleI screenSpaceMaskingRect = new RectangleI((int)Math.Floor(screenSpaceDrawRectangle.X), (int)Math.Floor(screenSpaceDrawRectangle.Y), (int)frameBufferSize.X + 1,
                (int)frameBufferSize.Y + 1);

            renderer.PushMaskingInfo(new MaskingInfo
            {
                ScreenSpaceAABB = screenSpaceMaskingRect,
                MaskingRect = screenSpaceDrawRectangle,
                ToMaskingSpace = Matrix3.Identity,
                BlendRange = 1,
                AlphaExponent = 1,
            }, true);

            // Match viewport to FrameBuffer such that we don't draw unnecessary pixels.
            renderer.PushViewport(new RectangleI(0, 0, (int)frameBufferSize.X, (int)frameBufferSize.Y));
            renderer.PushScissor(new RectangleI(0, 0, (int)frameBufferSize.X, (int)frameBufferSize.Y));
            renderer.PushScissorOffset(screenSpaceMaskingRect.Location);

            return new ValueInvokeOnDisposal<(BufferedDrawNode node, IRenderer renderer)>((this, renderer), tup => tup.node.returnViewport(tup.renderer));
        }

        private void returnViewport(IRenderer renderer)
        {
            renderer.PopScissorOffset();
            renderer.PopViewport();
            renderer.PopScissor();
            renderer.PopMaskingInfo();
        }

        private void clipDrawRectangle()
        {
            if (!SharedData.ClipToRootNode || Source == null)
                return;

            // Get the root node
            IDrawable rootNode = rootNodeCached;

            if (rootNodeCached == null)
            {
                rootNode = Source;
                while (rootNode.Parent != null)
                    rootNode = rootNode.Parent;
                rootNodeCached = rootNode;
            }

            if (rootNode == null)
                return;

            // Clip the screen space draw rectangle to the bounds of the root node
            RectangleF clipBounds = new RectangleF(rootNode.ScreenSpaceDrawQuad.TopLeft, rootNode.ScreenSpaceDrawQuad.Size);
            screenSpaceDrawRectangle.Intersect(clipBounds);
        }

        protected override void Dispose(bool isDisposing)
        {
            base.Dispose(isDisposing);

            Child?.Dispose();
            Child = null;
        }
    }
}
