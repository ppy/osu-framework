// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;

namespace osu.Framework.Graphics.Shaders
{
    /// <summary>
    /// A mapping of a global uniform to many shaders which need to receive updates on a change.
    /// </summary>
    internal class UniformMapping<T> : IUniformMapping
        where T : unmanaged, IEquatable<T>
    {
        private T val;

        public T Value
        {
            get => val;
            set
            {
                if (value.Equals(val))
                    return;

                val = value;

                for (int i = 0; i < LinkedUniforms.Count; i++)
                    LinkedUniforms[i].UpdateValue(this);
            }
        }

        public List<GlobalUniform<T>> LinkedUniforms = new List<GlobalUniform<T>>();

        public string Name { get; }

        public UniformMapping(string name)
        {
            Name = name;
        }

        public void LinkShaderUniform(IUniform uniform)
        {
            var typedUniform = (GlobalUniform<T>)uniform;

            typedUniform.UpdateValue(this);
            LinkedUniforms.Add(typedUniform);
        }

        public void UnlinkShaderUniform(IUniform uniform)
        {
            var typedUniform = (GlobalUniform<T>)uniform;
            LinkedUniforms.Remove(typedUniform);
        }

        public void UpdateValue(ref T newValue)
        {
            if (newValue.Equals(val))
                return;

            val = newValue;

            for (int i = 0; i < LinkedUniforms.Count; i++)
                LinkedUniforms[i].UpdateValue(this);
        }

        public ref T GetValueByRef() => ref val;
    }
}
