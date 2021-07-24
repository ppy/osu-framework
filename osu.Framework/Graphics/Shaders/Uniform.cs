// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics.OpenGL;
using System;

namespace osu.Framework.Graphics.Shaders
{
    public class Uniform<T> : IUniformWithValue<T>
        where T : struct, IEquatable<T>
    {
        public Shader Owner { get; }
        public string Name { get; }
        public int Location { get; }

        public bool HasChanged { get; private set; } = true;

        private T val;

        public T Value
        {
            get => val;
            set
            {
                if (value.Equals(val))
                    return;

                val = value;
                HasChanged = true;

                if (Owner.IsBound)
                    Update();
            }
        }

        public Uniform(Shader owner, string name, int uniformLocation)
        {
            Owner = owner;
            Name = name;
            Location = uniformLocation;
        }

        public void UpdateValue(ref T newValue)
        {
            if (newValue.Equals(val))
                return;

            val = newValue;
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

        ref T IUniformWithValue<T>.GetValueByRef() => ref val;
        T IUniformWithValue<T>.GetValue() => val;
    }
}
