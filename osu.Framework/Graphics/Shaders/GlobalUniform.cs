// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using osu.Framework.Graphics.Rendering;

namespace osu.Framework.Graphics.Shaders
{
    internal class GlobalUniform<T> : IUniformWithValue<T>
        where T : struct, IEquatable<T>
    {
        public IShader Owner { get; }
        public int Location { get; }
        public string Name { get; }

        /// <summary>
        /// Non-null denotes a pending global change. Must be a field to allow for reference access.
        /// </summary>
        public UniformMapping<T> PendingChange;

        private readonly IRenderer renderer;

        public GlobalUniform(IRenderer renderer, IShader owner, string name, int uniformLocation)
        {
            this.renderer = renderer;
            Owner = owner;
            Name = name;
            Location = uniformLocation;
        }

        internal void UpdateValue(UniformMapping<T> global)
        {
            PendingChange = global;
            if (Owner.IsBound)
                Update();
        }

        public void Update()
        {
            if (PendingChange == null)
                return;

            renderer.SetUniform(this);
            PendingChange = null;
        }

        ref T IUniformWithValue<T>.GetValueByRef() => ref PendingChange.GetValueByRef();
        T IUniformWithValue<T>.GetValue() => PendingChange.Value;
    }
}
