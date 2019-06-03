// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics.OpenGL;
using System;
using osu.Framework.Graphics.Colour;
using osu.Framework.Graphics.OpenGL.Textures;
using osu.Framework.Graphics.OpenGL.Vertices;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Textures;
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
        protected internal DrawColourInfo DrawColourInfo { get; internal set; }

        /// <summary>
        /// Identifies the state of this draw node with an invalidation state of its corresponding
        /// <see cref="Drawable"/>. An update is required when the invalidation state of this draw node disagrees
        /// with the invalidation state of its <see cref="Drawable"/>.
        /// </summary>
        protected internal long InvalidationID { get; private set; }

        /// <summary>
        /// The <see cref="Drawable"/> which this <see cref="DrawNode"/> draws.
        /// </summary>
        protected readonly IDrawable Source;

        private readonly AtomicCounter referenceCount = new AtomicCounter();

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
        /// Draws this draw node to the screen.
        /// </summary>
        /// <param name="vertexAction">The action to be performed on each vertex of
        /// the draw node in order to draw it if required. This is primarily used by
        /// textured sprites.</param>
        public virtual void Draw(Action<TexturedVertex2D> vertexAction)
        {
            GLWrapper.SetBlend(DrawColourInfo.Blending);
        }

        protected internal virtual void DrawHullSubTree(Action<TexturedVertex2D> vertexAction, DepthValue depthValue)
        {
            if (!depthValue.CanIncrement || !CanDrawHull)
            {
                // The back-to-front pass requires the depth value
                drawDepth = depthValue;
                return;
            }

            // It is crucial to draw with an incremented depth value, consider the case of a box:
            // In the FTB pass, the inner conservative area is drawn at depth X
            // In the BTF pass, the full area is drawn at depth X, and the depth test function is set to GL_LESS, so the inner conservative area is not redrawn
            // Furthermore, a BTF-drawn object above the box will be visible since it will be drawn with a depth of (X - increment), satisfying the depth test
            drawDepth = depthValue.Increment();

            DrawHull(vertexAction);
        }

        protected virtual void DrawHull(Action<TexturedVertex2D> vertexAction)
        {
        }

        protected virtual bool CanDrawHull => false;

        protected void DrawTriangle(Texture texture, Triangle vertexTriangle, ColourInfo colour, RectangleF? textureRect = null, Action<TexturedVertex2D> vertexAction = null,
                                    Vector2? inflationPercentage = null)
            => texture.DrawTriangle(vertexTriangle, drawDepth, colour, textureRect, vertexAction, inflationPercentage);

        protected void DrawTriangle(TextureGL texture, Triangle vertexTriangle, RectangleF? textureRect, ColourInfo drawColour, Action<TexturedVertex2D> vertexAction = null,
                                    Vector2? inflationPercentage = null)
            => texture.DrawTriangle(vertexTriangle, drawDepth, textureRect, drawColour, vertexAction, inflationPercentage);

        protected void DrawQuad(Texture texture, Quad vertexQuad, ColourInfo colour, RectangleF? textureRect = null, Action<TexturedVertex2D> vertexAction = null, Vector2? inflationPercentage = null,
                                Vector2? blendRangeOverride = null)
            => texture.DrawQuad(vertexQuad, drawDepth, colour, textureRect, vertexAction, inflationPercentage, blendRangeOverride);

        protected void DrawQuad(TextureGL texture, Quad vertexQuad, RectangleF? textureRect, ColourInfo drawColour, Action<TexturedVertex2D> vertexAction = null, Vector2? inflationPercentage = null,
                                Vector2? blendRangeOverride = null)
            => texture.DrawQuad(vertexQuad, drawDepth, textureRect, drawColour, vertexAction, inflationPercentage, blendRangeOverride);

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
            Dispose(false);
        }

        public void Dispose()
        {
            if (referenceCount.Decrement() != 0)
                return;

            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool isDisposing)
        {
        }
    }
}
