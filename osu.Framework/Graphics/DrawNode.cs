﻿// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics.OpenGL;
using System;
using System.Runtime.CompilerServices;
using osu.Framework.Graphics.Batches;
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
        protected readonly IDrawable Source;

        private readonly AtomicCounter referenceCount = new AtomicCounter();

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
            => texture.DrawQuad(vertexQuad, drawColour, textureRect, vertexAction, inflationPercentage, blendRangeOverride);

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
            => texture.DrawQuad(vertexQuad, drawColour, textureRect, vertexAction, inflationPercentage, blendRangeOverride);

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
