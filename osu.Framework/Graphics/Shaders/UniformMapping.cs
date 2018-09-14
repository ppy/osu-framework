// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System.Collections.Generic;

namespace osu.Framework.Graphics.Shaders
{
    /// <summary>
    /// A mapping of a global uniform to many shaders which need to receive updates on a change.
    /// </summary>
    internal class UniformMapping<T> : IUniformMapping
        where T : struct
    {
        public T Value;

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
            Value = newValue;

            for (int i = 0; i < LinkedUniforms.Count; i++)
                LinkedUniforms[i].UpdateValue(this);
        }
    }
}
