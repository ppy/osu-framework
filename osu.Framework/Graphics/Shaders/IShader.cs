// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using osu.Framework.Graphics.Rendering;

namespace osu.Framework.Graphics.Shaders
{
    public interface IShader : IDisposable
    {
        /// <summary>
        /// All uniforms declared by this shader.
        /// </summary>
        internal IReadOnlyDictionary<string, IUniform> Uniforms { get; }

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
            where T : unmanaged, IEquatable<T>;

        /// <summary>
        /// Binds an <see cref="IUniformBuffer"/> to a uniform block of the given name.
        /// </summary>
        /// <param name="blockName">The uniform block name.</param>
        /// <param name="buffer">The buffer to bind to the block.</param>
        void BindUniformBlock(string blockName, IUniformBuffer buffer);
    }
}
