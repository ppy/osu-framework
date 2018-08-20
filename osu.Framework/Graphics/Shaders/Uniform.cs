// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Graphics.OpenGL;

namespace osu.Framework.Graphics.Shaders
{
    public class Uniform<T> : IUniformWithValue<T>
        where T : struct
    {
        public Shader Owner { get; }
        public string Name { get; }
        public int Location { get; }

        public bool HasChanged { get; private set; } = true;

        public T Value;

        public Uniform(Shader owner, string name, int uniformLocation)
        {
            Owner = owner;
            Name = name;
            Location = uniformLocation;
        }

        public void UpdateValue(ref T newValue)
        {
            if (newValue.Equals(Value))
                return;

            Value = newValue;
            HasChanged = true;

            if (Owner.IsBound)
                Update();
        }

        public void Update()
        {
            if (!HasChanged) return;

            GLWrapper.SetUniform(this);
            HasChanged = false;
        }

        ref T IUniformWithValue<T>.GetValueByRef() => ref Value;
        T IUniformWithValue<T>.GetValue() => Value;
    }
}
