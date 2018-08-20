// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Graphics.OpenGL;

namespace osu.Framework.Graphics.Shaders
{
    public class GlobalUniform<T> : IUniformWithValue<T>
        where T : struct
    {
        /// <summary>
        /// Non-null denotes a pending global change. Must be a field to allow for reference access.
        /// </summary>
        internal UniformMapping<T> PendingChange;

        public GlobalUniform(Shader owner, string name, int uniformLocation)
        {
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

            GLWrapper.SetUniform(this);
            PendingChange = null;
        }

        public ref T GetValue() => ref PendingChange.Value;

        public Shader Owner { get; }
        public int Location { get; }
        public string Name { get; }
    }
}
