// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics.OpenGL;
using System;
using System.Runtime.CompilerServices;
using osu.Framework.Graphics.Batches;
using osu.Framework.Graphics.Colour;
using osu.Framework.Graphics.OpenGL.Buffers;
using osu.Framework.Graphics.OpenGL.Textures;
using osu.Framework.Graphics.OpenGL.Vertices;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Textures;
using osu.Framework.MathUtils.Clipping;
using osu.Framework.Threading;
using osuTK;

namespace osu.Framework.Graphics
{
    /// <summary>
    /// Contains all the information required to draw a single <see cref="Drawable"/>.
    /// A hierarchy of <see cref="DrawNode"/>s is passed to the draw thread for rendering every frame.
    /// </summary>
    public class DrawNode : IDisposable
    {
        /// <summary>
        /// Contains the linear transformation of this <see cref="DrawNode"/>.
        /// </summary>
        protected DrawInfo DrawInfo { get; private set; }

        /// <summary>
        /// Contains the colour and blending information of this <see cref="DrawNode"/>.
        /// </summary>
        protected DrawColourInfo DrawColourInfo { get; private set; }

        /// <summary>
        /// Identifies the state of this draw node with an invalidation state of its corresponding
        /// <see cref="Drawable"/>. An update is required when the invalidation state of this draw node disagrees
        /// with the invalidation state of its <see cref="Drawable"/>.
        /// </summary>
        protected internal long InvalidationID { get; private set; }

        /// <summary>
        /// The <see cref="Drawable"/> which this <see cref="DrawNode"/> draws.
        /// </summary>
        protected IDrawable Source { get; private set; }

        private readonly AtomicCounter referenceCount = new AtomicCounter();

        /// <summary>
        /// The depth at which drawing should take place.
        /// This is written to from the front-to-back pass and used in both passes.
        /// </summary>
        private float drawDepth;

        /// <summary>
        /// Creates a new <see cref="DrawNode"/>.
        /// </summary>
        /// <param name="source">The <see cref="Drawable"/> to draw with this <see cref="DrawNode"/>.</param>
        public DrawNode(IDrawable source)
        {
            Source = source;

            Reference();
        }

        /// <summary>
        /// Applies the state of <see cref="Source"/> to this <see cref="DrawNode"/> for use in rendering.
        /// The applied state must remain immutable.
        /// </summary>
        public virtual void ApplyState()
        {
            DrawInfo = Source.DrawInfo;
            DrawColourInfo = Source.DrawColourInfo;
            InvalidationID = Source.InvalidationID;
        }

        /// <summary>
        /// Draws this <see cref="DrawNode"/> to the screen.
        /// </summary>
        /// <remarks>
        /// Subclasses must invoke <code>base.Draw()</code> prior to drawing vertices.
        /// </remarks>
        /// <param name="vertexAction">The action to be performed on each vertex of the draw node in order to draw it if required. This is primarily used by textured sprites.</param>
        public virtual void Draw(Action<TexturedVertex2D> vertexAction)
        {
            GLWrapper.SetBlend(DrawColourInfo.Blending);

            // This is the back-to-front (BTF) pass. The back-buffer depth test function used is GL_LESS.
            // The depth test will fail for samples that overlap the opaque interior of this <see cref="DrawNode"/> and any <see cref="DrawNode"/>s above this one.
            GLWrapper.SetDrawDepth(drawDepth);
        }

        /// <summary>
        /// Draws the opaque interior of this <see cref="DrawNode"/> and all <see cref="DrawNode"/>s further down the scene graph, invoking <see cref="DrawOpaqueInterior"/> if <see cref="CanDrawOpaqueInterior"/>
        /// indicates that an opaque interior can be drawn for each relevant <see cref="DrawNode"/>.
        /// </summary>
        /// <remarks>
        /// This is the front-to-back pass. The back-buffer depth test function used is GL_LESS.<br />
        /// If an opaque interior is not drawn: the current value of <paramref name="depthValue"/> is stored.<br />
        /// If an opaque interior is drawn: <paramref name="depthValue"/> is incremented, stored, and the opaque interior vertices are drawn at the post-incremented depth value.
        /// Incrementing <paramref name="depthValue"/> at this point allows for early-z testing to also occur within the front-to-back pass.<br />
        /// </remarks>
        /// <param name="depthValue">The previous depth value.</param>
        /// <param name="vertexAction">The action to be performed on each vertex of the draw node in order to draw it if required. This is primarily used by textured sprites.</param>
        internal virtual void DrawOpaqueInteriorSubTree(DepthValue depthValue, Action<TexturedVertex2D> vertexAction)
        {
            if (!depthValue.CanIncrement || !CanDrawOpaqueInterior)
            {
                // The back-to-front pass requires the depth value
                drawDepth = depthValue;
                return;
            }

            // It is crucial to draw with an incremented depth value, consider the case of a box:
            // In the front-to-back pass, the inner conservative area is drawn at depth X
            // In the back-to-front pass, the full area is drawn at depth X, and the depth test function is set to GL_LESS, so the inner conservative area is not redrawn
            // Furthermore, a back-to-front-drawn object above the box will be visible since it will be drawn with a depth of (X - increment), satisfying the depth test
            drawDepth = depthValue.Increment();

            DrawOpaqueInterior(vertexAction);
        }

        /// <summary>
        /// Draws the opaque interior of this <see cref="DrawNode"/> to the screen.
        /// The opaque interior must be a fully-opaque, non-blended area of this <see cref="DrawNode"/>, clipped to the current masking area via <code>DrawClipped()</code>.
        /// See <see cref="Shapes.Box.BoxDrawNode"/> for an example implementation.
        /// </summary>
        /// <remarks>
        /// Subclasses must invoke <code>base.DrawOpaqueInterior()</code> prior to drawing vertices.
        /// </remarks>
        /// <param name="vertexAction">The action to be performed on each vertex of the draw node in order to draw it if required. This is primarily used by textured sprites.</param>
        protected virtual void DrawOpaqueInterior(Action<TexturedVertex2D> vertexAction)
        {
            GLWrapper.SetBlend(DrawColourInfo.Blending);
            GLWrapper.SetDrawDepth(drawDepth);
        }

        /// <summary>
        /// Whether this <see cref="DrawNode"/> can draw a opaque interior. <see cref="DrawOpaqueInterior"/> will only be invoked if this value is <code>true</code>.
        /// Should not return <code>true</code> if <see cref="DrawOpaqueInterior"/> will result in a no-op.
        /// </summary>
        protected internal virtual bool CanDrawOpaqueInterior => false;

        /// <summary>
        /// Draws a triangle to the screen.
        /// </summary>
        /// <param name="texture">The texture to fill the triangle with.</param>
        /// <param name="vertexTriangle">The triangle to draw.</param>
        /// <param name="textureRect">The texture rectangle.</param>
        /// <param name="drawColour">The vertex colour.</param>
        /// <param name="vertexAction">An action that adds vertices to a <see cref="VertexBatch{T}"/>.</param>
        /// <param name="inflationPercentage">The percentage amount that <see cref="textureRect"/> should be inflated.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected void DrawTriangle(Texture texture, Triangle vertexTriangle, ColourInfo drawColour, RectangleF? textureRect = null, Action<TexturedVertex2D> vertexAction = null,
                                    Vector2? inflationPercentage = null)
            => texture.DrawTriangle(vertexTriangle, drawColour, textureRect, vertexAction, inflationPercentage);

        /// <summary>
        /// Draws a triangle to the screen.
        /// </summary>
        /// <param name="texture">The texture to fill the triangle with.</param>
        /// <param name="vertexTriangle">The triangle to draw.</param>
        /// <param name="drawColour">The vertex colour.</param>
        /// <param name="textureRect">The texture rectangle.</param>
        /// <param name="vertexAction">An action that adds vertices to a <see cref="VertexBatch{T}"/>.</param>
        /// <param name="inflationPercentage">The percentage amount that <see cref="textureRect"/> should be inflated.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected void DrawTriangle(TextureGL texture, Triangle vertexTriangle, ColourInfo drawColour, RectangleF? textureRect = null, Action<TexturedVertex2D> vertexAction = null,
                                    Vector2? inflationPercentage = null)
            => texture.DrawTriangle(vertexTriangle, drawColour, textureRect, vertexAction, inflationPercentage);

        /// <summary>
        /// Draws a quad to the screen.
        /// </summary>
        /// <param name="texture">The texture to fill the triangle with.</param>
        /// <param name="vertexQuad">The quad to draw.</param>
        /// <param name="textureRect">The texture rectangle.</param>
        /// <param name="drawColour">The vertex colour.</param>
        /// <param name="vertexAction">An action that adds vertices to a <see cref="VertexBatch{T}"/>.</param>
        /// <param name="inflationPercentage">The percentage amount that <see cref="textureRect"/> should be inflated.</param>
        /// <param name="blendRangeOverride">The range over which the edges of the <see cref="textureRect"/> should be blended.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected void DrawQuad(Texture texture, Quad vertexQuad, ColourInfo drawColour, RectangleF? textureRect = null, Action<TexturedVertex2D> vertexAction = null,
                                Vector2? inflationPercentage = null, Vector2? blendRangeOverride = null)
            => texture.DrawQuad(vertexQuad, drawColour, textureRect, vertexAction, inflationPercentage: inflationPercentage, blendRangeOverride: blendRangeOverride);

        /// <summary>
        /// Draws a quad to the screen.
        /// </summary>
        /// <param name="texture">The texture to fill the triangle with.</param>
        /// <param name="vertexQuad">The quad to draw.</param>
        /// <param name="drawColour">The vertex colour.</param>
        /// <param name="textureRect">The texture rectangle.</param>
        /// <param name="vertexAction">An action that adds vertices to a <see cref="VertexBatch{T}"/>.</param>
        /// <param name="inflationPercentage">The percentage amount that <see cref="textureRect"/> should be inflated.</param>
        /// <param name="blendRangeOverride">The range over which the edges of the <see cref="textureRect"/> should be blended.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected void DrawQuad(TextureGL texture, Quad vertexQuad, ColourInfo drawColour, RectangleF? textureRect = null, Action<TexturedVertex2D> vertexAction = null,
                                Vector2? inflationPercentage = null, Vector2? blendRangeOverride = null)
            => texture.DrawQuad(vertexQuad, drawColour, textureRect, vertexAction, inflationPercentage: inflationPercentage, blendRangeOverride: blendRangeOverride);

        /// <summary>
        /// Clips a <see cref="IConvexPolygon"/> to the current masking area and draws the resulting triangles to the screen using the specified texture.
        /// </summary>
        /// <param name="polygon">The polygon to draw.</param>
        /// <param name="texture">The texture to fill the triangle with.</param>
        /// <param name="textureRect">The texture rectangle.</param>
        /// <param name="drawColour">The vertex colour.</param>
        /// <param name="vertexAction">An action that adds vertices to a <see cref="VertexBatch{T}"/>.</param>
        /// <param name="inflationPercentage">The percentage amount that <see cref="textureRect"/> should be inflated.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected void DrawClipped<T>(ref T polygon, Texture texture, ColourInfo drawColour, RectangleF? textureRect = null, Action<TexturedVertex2D> vertexAction = null,
                                      Vector2? inflationPercentage = null)
            where T : IConvexPolygon
        {
            var maskingQuad = GLWrapper.CurrentMaskingInfo.ConservativeScreenSpaceQuad;

            var clipper = new ConvexPolygonClipper<Quad, T>(ref maskingQuad, ref polygon);
            Span<Vector2> buffer = stackalloc Vector2[clipper.GetClipBufferSize()];
            Span<Vector2> clippedRegion = clipper.Clip(buffer);

            for (int i = 2; i < clippedRegion.Length; i++)
                DrawTriangle(texture, new Triangle(clippedRegion[0], clippedRegion[i - 1], clippedRegion[i]), drawColour, textureRect, vertexAction, inflationPercentage);
        }

        /// <summary>
        /// Clips a <see cref="IConvexPolygon"/> to the current masking area and draws the resulting triangles to the screen using the specified texture.
        /// </summary>
        /// <param name="polygon">The polygon to draw.</param>
        /// <param name="texture">The texture to fill the triangle with.</param>
        /// <param name="textureRect">The texture rectangle.</param>
        /// <param name="drawColour">The vertex colour.</param>
        /// <param name="vertexAction">An action that adds vertices to a <see cref="VertexBatch{T}"/>.</param>
        /// <param name="inflationPercentage">The percentage amount that <see cref="textureRect"/> should be inflated.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected void DrawClipped<T>(ref T polygon, TextureGL texture, ColourInfo drawColour, RectangleF? textureRect = null, Action<TexturedVertex2D> vertexAction = null,
                                      Vector2? inflationPercentage = null)
            where T : IConvexPolygon
        {
            var maskingQuad = GLWrapper.CurrentMaskingInfo.ConservativeScreenSpaceQuad;

            var clipper = new ConvexPolygonClipper<Quad, T>(ref maskingQuad, ref polygon);
            Span<Vector2> buffer = stackalloc Vector2[clipper.GetClipBufferSize()];
            Span<Vector2> clippedRegion = clipper.Clip(buffer);

            for (int i = 2; i < clippedRegion.Length; i++)
                DrawTriangle(texture, new Triangle(clippedRegion[0], clippedRegion[i - 1], clippedRegion[i]), drawColour, textureRect, vertexAction, inflationPercentage);
        }

        /// <summary>
        /// Draws a <see cref="FrameBuffer"/> to the screen.
        /// </summary>
        /// <param name="frameBuffer">The <see cref="FrameBuffer"/> to draw.</param>
        /// <param name="vertexQuad">The destination vertices.</param>
        /// <param name="drawColour">The colour to draw the <paramref name="frameBuffer"/> with.</param>
        /// <param name="vertexAction">An action that adds vertices to a <see cref="VertexBatch{T}"/>.</param>
        /// <param name="inflationPercentage">The percentage amount that the frame buffer area  should be inflated.</param>
        /// <param name="blendRangeOverride">The range over which the edges of the frame buffer should be blended.</param>
        protected void DrawFrameBuffer(FrameBuffer frameBuffer, Quad vertexQuad, ColourInfo drawColour, Action<TexturedVertex2D> vertexAction = null,
                                       Vector2? inflationPercentage = null, Vector2? blendRangeOverride = null)
        {
            // The strange Y coordinate and Height are a result of OpenGL coordinate systems having Y grow upwards and not downwards.
            RectangleF textureRect = new RectangleF(0, frameBuffer.Texture.Height, frameBuffer.Texture.Width, -frameBuffer.Texture.Height);

            if (frameBuffer.Texture.Bind())
                DrawQuad(frameBuffer.Texture, vertexQuad, drawColour, textureRect, vertexAction, inflationPercentage, blendRangeOverride);
        }

        /// <summary>
        /// Increments the reference count of this <see cref="DrawNode"/>, blocking <see cref="Dispose"/> until the count reaches 0.
        /// Invoke <see cref="Dispose"/> to remove the reference.
        /// </summary>
        /// <remarks>
        /// All <see cref="DrawNode"/>s start with a reference count of 1.
        /// </remarks>
        internal void Reference() => referenceCount.Increment();

        ~DrawNode()
        {
            GLWrapper.ScheduleDisposal(() => Dispose(false));
        }

        protected internal bool IsDisposed { get; private set; }

        public void Dispose()
        {
            if (referenceCount.Decrement() != 0)
                return;

            GLWrapper.ScheduleDisposal(() => Dispose(true));
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool isDisposing)
        {
            Source = null;
            IsDisposed = true;
        }
    }
}
