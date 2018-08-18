// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu/master/LICENCE

using System.Collections.Generic;

namespace osu.Framework.Graphics.Shaders
{
    /// <summary>
    /// A mapping of a global uniform to many shaders which need to receive updates on a change.
    /// </summary>
    internal class UniformMapping
    {
        private object _value;
        public string Name;
        public List<UniformBase> LinkedUniforms = new List<UniformBase>();

        public UniformMapping(string name)
        {
            Name = name;
        }

        public object Value
        {
            get { return _value; }
            set
            {
                if (_value?.Equals(value) == true)
                    return;

                _value = value;
                foreach (var linked in LinkedUniforms)
                    linked.Value = value;
            }
        }
    }
}
