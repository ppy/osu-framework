// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;

namespace osu.Framework.Graphics.Shaders
{
    public interface IShader : IDisposable
    {
        /// <summary>
        /// Binds this shader to be used for rendering.
        /// </summary>
        void Bind();

        /// <summary>
        /// Unbinds this shader.
        /// </summary>
        void Unbind();

        /// <summary>
        /// Whether this shader is ready for use.
        /// </summary>
        bool IsLoaded { get; }

        /// <summary>
        /// Whether this shader is currently bound.
        /// </summary>
        bool IsBound { get; }

        /// <summary>
        /// Retrieves a uniform from the shader.
        /// </summary>
        /// <param name="name">The name of the uniform.</param>
        /// <returns>The retrieved uniform.</returns>
        Uniform<T> GetUniform<T>(string name)
            where T : struct, IEquatable<T>;
    }
}
