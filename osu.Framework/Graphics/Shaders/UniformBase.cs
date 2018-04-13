// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using OpenTK.Graphics.ES30;
using osu.Framework.Graphics.OpenGL;

namespace osu.Framework.Graphics.Shaders
{
    internal class UniformBase
    {
        public string Name { get; }

        private object value;

        public object Value
        {
            get { return value; }
            set
            {
                if (value == this.value)
                    return;

                this.value = value;
                HasChanged = true;

                if (owner.IsBound)
                    Update();
            }
        }

        private readonly int location;
        private readonly ActiveUniformType type;

        public bool HasChanged { get; private set; } = true;

        private readonly Shader owner;

        public UniformBase(Shader owner, string name, int uniformLocation, ActiveUniformType type)
        {
            this.owner = owner;
            Name = name;
            location = uniformLocation;
            this.type = type;
        }

        public void Update()
        {
            if (!HasChanged)
                return;

            HasChanged = false;

            if (Value == null)
                return;

            GLWrapper.SetUniform(owner, type, location, Value);
        }
    }
}
