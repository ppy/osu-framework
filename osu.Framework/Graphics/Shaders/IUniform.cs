// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace osu.Framework.Graphics.Shaders
{
    /// <summary>
    /// Represents an updateable shader uniform.
    /// </summary>
    public interface IUniform
    {
        /// <summary>
        /// The shader which this uniform was declared in.
        /// </summary>
        IShader Owner { get; }

        /// <summary>
        /// The name of this uniform as declared by the shader, or <see cref="GlobalPropertyManager"/> for global uniforms.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// The location of this uniform in relation to all other uniforms in the shader.
        /// </summary>
        /// <remarks>
        /// Depending on the renderer used, this could either be zero-based index number or location in bytes.
        /// </remarks>
        int Location { get; }

        /// <summary>
        /// Updates the renderer with the current value of this uniform.
        /// </summary>
        void Update();
    }
}
