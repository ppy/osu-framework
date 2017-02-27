﻿// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Graphics.OpenGL;
using System;

namespace osu.Framework.Graphics
{
    public class DrawNode
    {
        /// <summary>
        /// Contains a linear transformation, colour information, and blending information
        /// of this draw node.
        /// </summary>
        public DrawInfo DrawInfo;

        /// <summary>
        /// Identifies the state of this draw node with an invalidation state of its corresponding
        /// <see cref="Drawable"/>. Whenever the invalidation state of this draw node disagrees
        /// with the state of its <see cref="Drawable"/> it has to be updated.
        /// </summary>
        public long InvalidationID;

        /// <summary>
        /// Draws this draw node to the screen.
        /// </summary>
        /// <param name="vertexAction">The action to be performed on each vertex of
        /// the draw node in order to draw it if required. This is primarily used by
        /// textured sprites.</param>
        public virtual void Draw(Action<TexturedVertex2D> vertexAction)
        {
            GLWrapper.SetBlend(DrawInfo.Blending);
        }
    }
}
