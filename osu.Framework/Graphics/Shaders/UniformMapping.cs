// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu/master/LICENCE

using System.Collections.Generic;

namespace osu.Framework.Graphics.Shaders
{
    internal interface IUniformMapping
    {
        string Name { get; set; }

        void LinkShaderUniform(IUniform uniform);
        void UnlinkShaderUniform(IUniform uniform);
    }


    /// <summary>
    /// A mapping of a global uniform to many shaders which need to receive updates on a change.
    /// </summary>
    public class UniformMapping<T> : IUniformMapping
        where T : struct
    {
        public T Value;

        public List<Uniform<T>> LinkedUniforms = new List<Uniform<T>>();

        public string Name { get; set; }

        public void LinkShaderUniform(IUniform uniform)
        {
            var typedUniform = (Uniform<T>)uniform;

            typedUniform.UpdateValue(this);
            LinkedUniforms.Add(typedUniform);
        }

        public void UnlinkShaderUniform(IUniform uniform)
        {
            var typedUniform = (Uniform<T>)uniform;
            LinkedUniforms.Remove(typedUniform);
        }

        public UniformMapping(string name)
        {
            Name = name;
        }

        public void UpdateValue(ref T newValue)
        {
            Value = newValue;
            foreach (var linked in LinkedUniforms)
                linked.UpdateValue(this);
        }
    }
}
