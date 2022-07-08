// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

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
