// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics.OpenGL;
using System;
using osu.Framework.Graphics.OpenGL.Vertices;
using osuTK;

namespace osu.Framework.Graphics
{
    /// <summary>
    /// Contains all the information required to draw a single <see cref="Drawable"/>.
    /// A hierarchy of DrawNodes is passed to the draw thread for rendering every frame.
    /// </summary>
    public class DrawNode : IDisposable
    {
        /// <summary>
        /// Contains a linear transformation, colour information, and blending information
        /// of this draw node.
        /// </summary>
        public DrawInfo DrawInfo { get; internal set; }

        public DrawColourInfo DrawColourInfo { get; internal set; }

        /// <summary>
        /// Identifies the state of this draw node with an invalidation state of its corresponding
        /// <see cref="Drawable"/>. Whenever the invalidation state of this draw node disagrees
        /// with the state of its <see cref="Drawable"/> it has to be updated.
        /// </summary>
        public long InvalidationID { get; internal set; }

        public float Depth;

        /// <summary>
        /// Draws this draw node to the screen.
        /// </summary>
        /// <param name="vertexAction">The action to be performed on each vertex of
        ///     the draw node in order to draw it if required. This is primarily used by
        ///     textured sprites.</param>
        public virtual void Draw(Action<TexturedVertex2D> vertexAction)
        {
            GLWrapper.SetBlend(DrawColourInfo.Blending);
        }

        public virtual void DrawHull(Action<TexturedVertex2D> vertexAction, ref uint depthIndex)
        {
            Depth = MathHelper.Clamp(-1 + depthIndex / 16383f, -1, 1);
        }

        ~DrawNode()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool isDisposing)
        {
        }
    }
}
