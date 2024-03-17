// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using osu.Framework.Graphics.Shaders;
using osu.Framework.Graphics.Veldrid.Shaders;

namespace osu.Framework.Graphics.Rendering.Deferred
{
    internal class DeferredShader : IShader
    {
        public VeldridShader Resource { get; }

        private readonly DeferredRenderer renderer;

        public DeferredShader(DeferredRenderer renderer, VeldridShader shader)
        {
            this.renderer = renderer;
            Resource = shader;
        }

        IReadOnlyDictionary<string, IUniform> IShader.Uniforms { get; } = new Dictionary<string, IUniform>();

        public void Bind()
        {
            if (IsBound)
                return;

            renderer.BindShader(this);
            IsBound = true;
        }

        public void Unbind()
        {
            if (!IsBound)
                return;

            renderer.UnbindShader(this);
            IsBound = false;
        }

        public bool IsLoaded
            => Resource.IsLoaded;

        public bool IsBound { get; private set; }

        public Uniform<T> GetUniform<T>(string name)
            where T : unmanaged, IEquatable<T>
            => throw new NotSupportedException();

        public void BindUniformBlock(string blockName, IUniformBuffer buffer)
            => renderer.BindUniformBuffer(blockName, buffer);

        public void Dispose()
            => Resource.Dispose();
    }
}
