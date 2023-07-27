// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osuTK.Graphics.ES30;

namespace osu.Framework.Graphics.OpenGL.Shaders
{
    /// <summary>
    /// A uniform block of an OpenGL shader.
    /// </summary>
    internal class GLUniformBlock
    {
        public readonly int Binding;

        /// <summary>
        /// Creates a new uniform block.
        /// </summary>
        /// <param name="shader">The shader.</param>
        /// <param name="index">The index (location) of this block in the GL shader.</param>
        /// <param name="binding">A unique index for this block to bound to in the GL program.</param>
        public GLUniformBlock(GLShader shader, int index, int binding)
        {
            Binding = binding;

            // This creates a mapping in the shader program that binds the block at location `index` to location `binding` in the GL pipeline.
            GL.UniformBlockBinding(shader, index, binding);
        }
    }
}
