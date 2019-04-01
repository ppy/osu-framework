// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics.OpenGL;
using System;
using osu.Framework.Graphics.OpenGL.Vertices;

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
        protected DrawInfo DrawInfo;

        protected DrawColourInfo DrawColourInfo;

        /// <summary>
        /// Identifies the state of this draw node with an invalidation state of its corresponding
        /// <see cref="Drawable"/>. Whenever the invalidation state of this draw node disagrees
        /// with the state of its <see cref="Drawable"/> it has to be updated.
        /// </summary>
        protected internal long InvalidationID { get; private set; }

        /// <summary>
        /// Applies properties from a <see cref="Drawable"/> to this <see cref="DrawNode"/>.
        /// </summary>
        /// <param name="source">The <see cref="Drawable"/> to apply properties from.</param>
        public virtual void ApplyFromDrawable(Drawable source)
        {
            DrawInfo = source.DrawInfo;
            DrawColourInfo = source.DrawColourInfo;
            InvalidationID = source.InvalidationID;
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
