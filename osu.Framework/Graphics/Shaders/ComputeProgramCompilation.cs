// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Veldrid.SPIRV;

namespace osu.Framework.Graphics.Shaders
{
    public class ComputeProgramCompilation
    {
        /// <summary>
        /// Whether this compilation was retrieved from cache.
        /// </summary>
        public bool WasCached { get; set; }

        /// <summary>
        /// The SpirV bytes for the program.
        /// </summary>
        public byte[] ProgramBytes { get; set; } = null!;

        /// <summary>
        /// The cross-compiled program text.
        /// </summary>
        public string ProgramText { get; set; } = null!;

        /// <summary>
        /// A reflection of the shader program, describing the layout of resources.
        /// </summary>
        public SpirvReflection Reflection { get; set; } = null!;
    }
}
