// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

namespace osu.Framework.Graphics.Shaders
{
    /// <summary>
    /// Stores a vertex shader attribute.
    /// </summary>
    internal struct AttributeInfo
    {
        /// <summary>
        /// The 0-based location of this attribute. This is in order of appearance in the shader code.
        /// Note that osu! uses 0-based attribute locations to bind vertex pointers to.
        /// </summary>
        public int Location;

        /// <summary>
        /// The name of the attribute.
        /// </summary>
        public string Name;
    }
}
