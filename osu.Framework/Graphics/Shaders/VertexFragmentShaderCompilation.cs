// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Veldrid.SPIRV;

namespace osu.Framework.Graphics.Shaders
{
    public class VertexFragmentShaderCompilation
    {
        /// <summary>
        /// Whether this compilation was retrieved from cache.
        /// </summary>
        public bool WasCached { get; set; }

        /// <summary>
        /// The SpirV bytes for the vertex shader.
        /// </summary>
        public byte[] VertexBytes { get; set; } = null!;

        /// <summary>
        /// The SpirV bytes for the fragment shader.
        /// </summary>
        public byte[] FragmentBytes { get; set; } = null!;

        /// <summary>
        /// The cross-compiled vertex shader text.
        /// </summary>
        public string VertexText { get; set; } = null!;

        /// <summary>
        /// The cross-compiled fragment shader text.
        /// </summary>
        public string FragmentText { get; set; } = null!;

        /// <summary>
        /// A reflection of the shader program, describing the layout of resources.
        /// </summary>
        public SpirvReflection Reflection { get; set; } = null!;
    }
}
