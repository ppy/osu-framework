// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Graphics.OpenGL;

namespace osu.Framework.Graphics.Shaders
{
    public class Uniform<T> : IUniform
        where T : struct
    {
        public string Name { get; }

        public T Value;

        public void UpdateValue(ref T newValue)
        {
            if (newValue.Equals(Value))
                return;

            Value = newValue;
            HasChanged = true;

            if (Owner.IsBound)
                Update();
        }

        public int Location { get; private set; }

        public bool HasChanged { get; private set; } = true;

        public Shader Owner { get; private set; }

        public Uniform(Shader owner, string name, int uniformLocation)
        {
            Owner = owner;
            Name = name;
            Location = uniformLocation;
        }

        public void Update()
        {
            if (!HasChanged) return;

            GLWrapper.SetUniform(this);
            HasChanged = false;
        }
    }
}
