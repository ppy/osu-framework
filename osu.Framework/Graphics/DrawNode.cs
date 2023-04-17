// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Threading;
using osu.Framework.Graphics.Rendering;

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

        private long referenceCount;

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
        /// <param name="renderer">The renderer to draw with.</param>
        public virtual void Draw(IRenderer renderer)
        {
            renderer.SetBlend(DrawColourInfo.Blending);

            // This is the back-to-front (BTF) pass. The back-buffer depth test function used is GL_LESS.
            // The depth test will fail for samples that overlap the opaque interior of this <see cref="DrawNode"/> and any <see cref="DrawNode"/>s above this one.
            renderer.SetDrawDepth(drawDepth);
        }

        /// <summary>
        /// Draws the opaque interior of this <see cref="DrawNode"/> and all <see cref="DrawNode"/>s further down the scene graph, invoking <see cref="DrawOpaqueInterior"/> if <see cref="CanDrawOpaqueInterior"/>
        /// indicates that an opaque interior can be drawn for each relevant <see cref="DrawNode"/>.
        /// </summary>
        /// <remarks>
        /// This is the front-to-back pass. The back-buffer depth test function used is GL_LESS.<br />
        /// During this pass, the opaque interior is drawn BELOW ourselves. For this to occur, <see cref="drawDepth"/> is temporarily incremented and then decremented after drawing is complete.
        /// Other <see cref="DrawNode"/>s behind ourselves receive the incremented depth value before doing the same themselves, allowing early-z to take place during this pass.
        /// </remarks>
        /// <param name="renderer">The renderer to draw with.</param>
        /// <param name="depthValue">The previous depth value.</param>
        internal virtual void DrawOpaqueInteriorSubTree(IRenderer renderer, DepthValue depthValue)
        {
            if (!depthValue.CanIncrement || !CanDrawOpaqueInterior)
            {
                // The back-to-front pass requires the depth value.
                drawDepth = depthValue;
                return;
            }

            // For an incoming depth value D, the opaque interior is drawn at depth D+e and the content is drawn at depth D.
            // As such, when the GL_LESS test function is applied, the content will always pass the depth test for the same DrawNode (D < D+e).

            // Increment the depth.
            float previousDepthValue = depthValue;
            drawDepth = depthValue.Increment();

            DrawOpaqueInterior(renderer);

            // Decrement the depth.
            drawDepth = previousDepthValue;
        }

        /// <summary>
        /// Draws the opaque interior of this <see cref="DrawNode"/> to the screen.
        /// The opaque interior must be a fully-opaque, non-blended area of this <see cref="DrawNode"/>, clipped to the current masking area via <code>DrawClipped()</code>.
        /// See <see cref="Sprites.SpriteDrawNode"/> for an example implementation.
        /// </summary>
        /// <remarks>
        /// Subclasses must invoke <code>base.DrawOpaqueInterior()</code> prior to drawing vertices.
        /// </remarks>
        /// <param name="renderer">The renderer to draw with.</param>
        protected virtual void DrawOpaqueInterior(IRenderer renderer)
        {
            renderer.SetDrawDepth(drawDepth);
        }

        /// <summary>
        /// Whether this <see cref="DrawNode"/> can draw a opaque interior. <see cref="DrawOpaqueInterior"/> will only be invoked if this value is <code>true</code>.
        /// Should not return <code>true</code> if <see cref="DrawOpaqueInterior"/> will result in a no-op.
        /// </summary>
        protected internal virtual bool CanDrawOpaqueInterior => false;

        /// <summary>
        /// Increments the reference count of this <see cref="DrawNode"/>, blocking <see cref="Dispose()"/> until the count reaches 0.
        /// Invoke <see cref="Dispose()"/> to remove the reference.
        /// </summary>
        /// <remarks>
        /// All <see cref="DrawNode"/>s start with a reference count of 1.
        /// </remarks>
        internal void Reference() => Interlocked.Increment(ref referenceCount);

        protected internal bool IsDisposed { get; private set; }

        public void Dispose()
        {
            if (Interlocked.Decrement(ref referenceCount) != 0)
                return;

            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool isDisposing)
        {
            Source = null;
            IsDisposed = true;
        }
    }
}
