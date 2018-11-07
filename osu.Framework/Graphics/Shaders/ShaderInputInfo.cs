// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

namespace osu.Framework.Graphics.Shaders
{
    /// <summary>
    /// Stores a vertex shader input.
    /// </summary>
    internal struct ShaderInputInfo
    {
        /// <summary>
        /// The 0-based location of this input. This is in order of appearance in the shader code.
        /// Note that osu! uses 0-based input locations to bind vertex pointers to.
        /// </summary>
        public int Location;

        /// <summary>
        /// The input name.
        /// </summary>
        public string Name;
    }
}
