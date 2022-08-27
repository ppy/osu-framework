// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace osu.Framework.Graphics.Veldrid.Shaders
{
    internal struct VeldridUniformInfo
    {
        /// <summary>
        /// The declared name of this uniform.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The type of this uniform.
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// The precision applied to this uniform.
        /// </summary>
        public string Precision { get; set; }
    }
}
