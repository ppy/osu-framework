// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

namespace osu.Framework.Graphics.Shaders
{
    public class Uniform<T>
    {
        private readonly UniformBase uniformBase;

        internal Uniform(UniformBase uniformBase)
        {
            this.uniformBase = uniformBase;
        }

        /// <summary>
        /// Gets or sets the value of this uniform.
        /// </summary>
        public T Value
        {
            get { return (T)uniformBase.Value; }
            set { uniformBase.Value = value; }
        }

        /// <summary>
        /// Returns the value of the uniform.
        /// </summary>
        /// <param name="filterUniform">The uniform to retrieve the value of.</param>
        public static implicit operator T(Uniform<T> filterUniform)
        {
            return filterUniform.Value;
        }
    }
}
