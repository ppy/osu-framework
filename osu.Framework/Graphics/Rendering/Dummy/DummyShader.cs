// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using osu.Framework.Graphics.Shaders;

namespace osu.Framework.Graphics.Rendering.Dummy
{
    /// <summary>
    /// An <see cref="IShader"/> that does nothing. May be used for tests that don't have a visual output.
    /// </summary>
    internal class DummyShader : IShader
    {
        private readonly IRenderer renderer;

        IReadOnlyDictionary<string, IUniform> IShader.Uniforms { get; } = new Dictionary<string, IUniform>();

        public DummyShader(IRenderer renderer)
        {
            this.renderer = renderer;
        }

        public void Bind()
        {
            IsBound = true;
        }

        public void Unbind()
        {
            IsBound = false;
        }

        public bool IsLoaded => true;
        public bool IsBound { get; private set; }

        public Uniform<T> GetUniform<T>(string name)
            where T : unmanaged, IEquatable<T>
        {
            return new Uniform<T>(renderer, this, name, 0);
        }

        public void BindUniformBlock(string blockName, IUniformBuffer buffer)
        {
        }

        public void Dispose()
        {
        }
    }
}
