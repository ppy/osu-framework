// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using osu.Framework.Graphics.Rendering;
using osu.Framework.Graphics.Shaders;

namespace osu.Framework.Graphics.Veldrid.Shaders
{
    internal class VeldridShaderPart : IShaderPart
    {
        private const string backbuffer_draw_depth_name = "m_BackbufferDrawDepth";

        private readonly string data;

        private readonly VeldridUniformGroup uniforms = new VeldridUniformGroup();

        public IVeldridUniformGroup Uniforms => uniforms;

        public ShaderPartType Type { get; }

        private static readonly Regex include_regex = new Regex("^\\s*#\\s*include\\s+[\"<](.*)[\">]");

        private static readonly Regex shader_uniform_regex = new Regex(@"^\s*(?>uniform)\s+(?:(lowp|mediump|highp)\s+)?(\w+)\s+(\w+);", RegexOptions.Multiline);
        private static readonly Regex shader_attribute_location_regex = new Regex(@"(layout\(location\s=\s)(-?\d+)(\)\s(?>attribute|in).*)", RegexOptions.Multiline);

        public VeldridShaderPart(ShaderManager shaders, byte[]? rawData, ShaderPartType type)
        {
            Type = type;
            data = loadShader(rawData, type, shaders, uniforms);
        }

        /// <summary>
        /// Returns compilable shader data.
        /// </summary>
        /// <param name="allUniforms">Uniforms declared on all shader parts of the grouping shader.</param>
        public byte[] GetData(IVeldridUniformGroup allUniforms)
        {
            string result = includeUniformStructure(data, allUniforms);

            if (!result.StartsWith("#version", StringComparison.Ordinal))
                result = $"#version 450\n{result}";

            return Encoding.UTF8.GetBytes(result);
        }

        /// <summary>
        /// Loads a given shader data and fills the given uniform group as defined by the shader.
        /// </summary>
        /// <param name="rawData">The raw data of the shader.</param>
        /// <param name="type">The shader type.</param>
        /// <param name="shaders">The shader manager for looking up header/internal files.</param>
        /// <param name="uniforms">The uniform group to fill with the data.</param>
        /// <param name="mainFile">Whether this is the main file of the shader, used for recursion.</param>
        private static string loadShader(byte[]? rawData, ShaderPartType type, ShaderManager shaders, VeldridUniformGroup uniforms, bool mainFile = true)
        {
            if (rawData == null)
                return string.Empty;

            using (MemoryStream ms = new MemoryStream(rawData))
            using (StreamReader sr = new StreamReader(ms))
            {
                string data = string.Empty;

                while (sr.Peek() != -1)
                {
                    string? line = sr.ReadLine();

                    if (string.IsNullOrEmpty(line))
                        continue;

                    Match includeMatch = include_regex.Match(line);

                    if (includeMatch.Success)
                    {
                        string includeName = includeMatch.Groups[1].Value.Trim();

                        data += loadShader(shaders.LoadRaw(includeName), type, shaders, uniforms, false) + '\n';
                    }
                    else
                        data += line + '\n';
                }

                if (!mainFile)
                    return data;

                data = loadShader(shaders.LoadRaw("sh_Precision_Internal.h"), type, shaders, uniforms, false) + "\n" + data;

                if (type == ShaderPartType.Vertex)
                    data = appendBackbuffer(data, shaders, uniforms);

                data = omitUniforms(data, uniforms);

                return data;
            }
        }

        /// <summary>
        /// Appends the "sh_Backbuffer_Internal.h" file to the given data.
        /// </summary>
        /// <param name="data">The data to append the header to.</param>
        /// <param name="shaders">The shader manager for looking up the backbuffer file.</param>
        /// <param name="uniforms">The uniform group to fill with the data.</param>
        private static string appendBackbuffer(string data, ShaderManager shaders, VeldridUniformGroup uniforms)
        {
            string realMainName = "real_main_" + Guid.NewGuid().ToString("N");

            string backbufferCode = loadShader(shaders.LoadRaw("sh_Backbuffer_Internal.h"), ShaderPartType.Vertex, shaders, uniforms, false);

            backbufferCode = backbufferCode.Replace("{{ real_main }}", realMainName);
            data = Regex.Replace(data, @"void main\((.*)\)", $"void {realMainName}()") + backbufferCode + '\n';

            Match attributeLocationMatch = shader_attribute_location_regex.Match(data);

            int count = 0;

            while (attributeLocationMatch.Success)
            {
                int location = int.Parse(attributeLocationMatch.Groups[2].Value);
                if (location == -1)
                    break;

                count = Math.Max(location + 1, count);
                attributeLocationMatch = attributeLocationMatch.NextMatch();
            }

            // place the backbuffer draw depth member at the bottom of the vertex structure.
            Debug.Assert(attributeLocationMatch.Value.Contains(backbuffer_draw_depth_name));
            data = data.Replace(attributeLocationMatch.Value, shader_attribute_location_regex.Replace(attributeLocationMatch.Value, m => m.Groups[1] + count.ToString() + m.Groups[3]));
            return data;
        }

        /// <summary>
        /// Omits all uniform declarations inside the given data, and fills them up in the given uniform group.
        /// </summary>
        /// <param name="data">The data to omit uniform declarations from.</param>
        /// <param name="uniforms">The uniform group to fill the uniforms in.</param>
        private static string omitUniforms(string data, VeldridUniformGroup uniforms)
        {
            Match uniformMatch = shader_uniform_regex.Match(data);

            if (!uniformMatch.Success)
                return data;

            do
            {
                uniforms.AddUniform(uniformMatch.Groups[3].Value.Trim(), uniformMatch.Groups[2].Value.Trim(), uniformMatch.Groups[1].Value.Trim());
                data = data.Replace(uniformMatch.Value, string.Empty);
            } while ((uniformMatch = uniformMatch.NextMatch()).Success);

            return data;
        }

        /// <summary>
        /// Includes a structure of the uniform buffer from the given uniform group.
        /// </summary>
        /// <param name="data">The data to include the structure in.</param>
        /// <param name="allUniforms">A list of all uniforms defined by any shader part in the grouping shader.</param>
        private string includeUniformStructure(string data, IVeldridUniformGroup allUniforms)
        {
            if (allUniforms.Uniforms.Count == 0)
                return data;

            string uniformBufferName = VeldridRenderer.UNIFORM_LAYOUT.Elements.Single().Name;

            var uniformBuilder = new StringBuilder();
            uniformBuilder.AppendLine($"layout(std140, set = {VeldridRenderer.UNIFORM_RESOURCES_SLOT}, binding = 0) uniform {uniformBufferName}");
            uniformBuilder.AppendLine("{");

            foreach (var uniform in allUniforms.Uniforms)
                uniformBuilder.AppendLine($"{uniform.Precision} {uniform.Type} {uniform.Name};".Trim());

            uniformBuilder.AppendLine("};");
            uniformBuilder.AppendLine();

            return $"{uniformBuilder}{data}";
        }

        public void Dispose()
        {
        }
    }
}
