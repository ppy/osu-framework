// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Graphics.OpenGL;
using System;
using osu.Framework.Graphics.OpenGL.Vertices;
using osuTK.Graphics.ES30;

namespace osu.Framework.Graphics
{
    /// <summary>
    /// Contains all the information required to draw a single <see cref="Drawable"/>.
    /// A hierarchy of DrawNodes is passed to the draw thread for rendering every frame.
    /// </summary>
    public class DrawNode
    {
        /// <summary>
        /// Contains a linear transformation, colour information, and blending information
        /// of this draw node.
        /// </summary>
        public DrawInfo DrawInfo;

        public DrawColourInfo DrawColourInfo;

        /// <summary>
        /// Identifies the state of this draw node with an invalidation state of its corresponding
        /// <see cref="Drawable"/>. Whenever the invalidation state of this draw node disagrees
        /// with the state of its <see cref="Drawable"/> it has to be updated.
        /// </summary>
        public long InvalidationID;

        /// <summary>
        /// Draws this draw node to the screen.
        /// </summary>
        /// <param name="pass"></param>
        /// <param name="vertexAction">The action to be performed on each vertex of
        ///     the draw node in order to draw it if required. This is primarily used by
        ///     textured sprites.</param>
        public virtual void Draw(RenderPass pass, Action<TexturedVertex2D> vertexAction)
        {
            GLWrapper.SetBlend(DrawColourInfo.Blending);
        }

        protected internal virtual bool SupportsFrontRenderPass => DrawColourInfo.Colour.MinAlpha == 1 && DrawColourInfo.Blending.RGBEquation == BlendEquationMode.FuncAdd;
    }

    public enum RenderPass
    {
        Back,
        Front
    }
}
