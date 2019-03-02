// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace osu.Framework.Graphics.Shaders
{
    public interface IShader
    {
        /// <summary>
        /// Bind this shader to be the active shader.
        /// </summary>
        void Bind();

        /// <summary>
        /// Unbind this shader.
        /// </summary>
        void Unbind();

        /// <summary>
        /// Returns a uniform from the shader.
        /// </summary>
        /// <param name="name">The name of the uniform.</param>
        /// <returns>Returns a base uniform.</returns>
        Uniform<T> GetUniform<T>(string name)
            where T : struct;
    }
}
