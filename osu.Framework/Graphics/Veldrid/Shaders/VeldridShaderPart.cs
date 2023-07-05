// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
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
        private static readonly Regex shader_input_pattern = new Regex(@"^\s*layout\s*\(\s*location\s*=\s*(-?\d+)\s*\)\s*in\s+((?:(?:lowp|mediump|highp)\s+)?\w+)\s+(\w+)\s*;", RegexOptions.Multiline);
        private static readonly Regex shader_output_pattern = new Regex(@"^\s*layout\s*\(\s*location\s*=\s*(-?\d+)\s*\)\s*out\s+((?:(?:lowp|mediump|highp)\s+)?\w+)\s+(\w+)\s*;", RegexOptions.Multiline);
        private static readonly Regex uniform_pattern = new Regex(@"^(\s*layout\s*\(.*)set\s*=\s*(-?\d)(.*\)\s*uniform)", RegexOptions.Multiline);
        private static readonly Regex include_pattern = new Regex(@"^\s*#\s*include\s+[""<](.*)["">]");

        public readonly ShaderPartType Type;

        private string header = string.Empty;

        private readonly string code;
        private readonly IShaderStore store;

        public readonly List<VeldridShaderAttribute> Inputs = new List<VeldridShaderAttribute>();
        public readonly List<VeldridShaderAttribute> Outputs = new List<VeldridShaderAttribute>();

        public VeldridShaderPart(byte[]? data, ShaderPartType type, IShaderStore store)
        {
            this.store = store;

            Type = type;

            // Load the shader files.
            code = loadFile(data, true);

            int lastInputIndex = 0;

            // Parse all shader inputs to find the last input index.
            foreach (Match m in shader_input_pattern.Matches(code))
                lastInputIndex = Math.Max(lastInputIndex, int.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture));

            // Update the location of the m_BackbufferDrawDepth input to be placed after all other inputs.
            code = code.Replace("layout(location = -1)", $"layout(location = {lastInputIndex + 1})");

            // Increment the binding set of all uniform blocks.
            // After this transformation, the g_GlobalUniforms block is placed in set 0 and all other user blocks begin from 1.
            // The difference in implementation here (compared to above) is intentional, as uniform blocks must be consistent between the shader stages, so they can't be easily appended.
            code = uniform_pattern.Replace(code, match => $"{match.Groups[1].Value}set = {int.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture) + 1}{match.Groups[3].Value}");
        }

        private VeldridShaderPart(string code, string header, ShaderPartType type, IShaderStore store)
        {
            this.code = code;
            this.header = header;
            this.store = store;

            Type = type;
        }

        private string loadFile(byte[]? bytes, bool mainFile)
        {
            if (bytes == null)
                return string.Empty;

            using (MemoryStream ms = new MemoryStream(bytes))
            using (StreamReader sr = new StreamReader(ms))
            {
                string result = string.Empty;

                while (sr.Peek() != -1)
                {
                    string? line = sr.ReadLine();

                    if (string.IsNullOrEmpty(line))
                    {
                        result += line + '\n';
                        continue;
                    }

                    if (line.StartsWith("#version", StringComparison.Ordinal)) // the version directive has to appear before anything else in the shader
                    {
                        header = line + '\n' + header;
                        continue;
                    }

                    if (line.StartsWith("#extension", StringComparison.Ordinal))
                    {
                        header += line + '\n';
                        continue;
                    }

                    Match includeMatch = include_pattern.Match(line);

                    if (includeMatch.Success)
                    {
                        string includeName = includeMatch.Groups[1].Value.Trim();

                        result += loadFile(store.GetRawData(includeName), false) + '\n';
                    }
                    else
                        result += line + '\n';
                }

                if (mainFile)
                {
                    string internalIncludes = loadFile(store.GetRawData("Internal/sh_Compatibility.h"), false) + "\n";
                    internalIncludes += loadFile(store.GetRawData("Internal/sh_GlobalUniforms.h"), false) + "\n";
                    result = internalIncludes + result;

                    Inputs.AddRange(shader_input_pattern.Matches(result).Select(m => new VeldridShaderAttribute(int.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture), m.Groups[2].Value)).ToList());
                    Outputs.AddRange(shader_output_pattern.Matches(result).Select(m => new VeldridShaderAttribute(int.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture), m.Groups[2].Value)).ToList());

                    string outputCode = loadFile(store.GetRawData($"Internal/sh_{Type}_Output.h"), false);

                    if (!string.IsNullOrEmpty(outputCode))
                    {
                        const string real_main_name = "__internal_real_main";

                        outputCode = outputCode.Replace("{{ real_main }}", real_main_name);
                        result = Regex.Replace(result, @"void main\((.*)\)", $"void {real_main_name}()") + outputCode + '\n';
                    }
                }

                return result;
            }
        }

        /// <summary>
        /// Creates a <see cref="VeldridShaderPart"/> based off this shader with a list of attributes passed through as input & output.
        /// Attributes from the list that are already defined in this shader will be ignored.
        /// </summary>
        /// <remarks>
        /// <para>
        /// In D3D11, unused fragment inputs cull their corresponding vertex output, which affects the vertex input/output structure leading to seemingly undefined behaviour.
        /// To prevent this from happening, make sure all unused vertex output are used and sent in the fragment output so that D3D11 doesn't omit them.
        /// </para>
        /// <para>
        /// This creates a new <see cref="VeldridShaderPart"/> rather than altering this existing instance since this is cached at a <see cref="IShaderStore"/> level and should remain immutable.
        /// </para>
        /// </remarks>
        /// <param name="attributes">The list of attributes to include in the shader as input & output.</param>
        public VeldridShaderPart WithPassthroughInput(IReadOnlyList<VeldridShaderAttribute> attributes)
        {
            string result = code;

            int outputLayoutIndex = Outputs.Max(m => m.Location) + 1;

            var attributesLayout = new StringBuilder();
            var attributesAssignment = new StringBuilder();
            var outputAttributes = new List<VeldridShaderAttribute>();

            foreach (VeldridShaderAttribute attribute in attributes)
            {
                if (Inputs.Any(i => attribute.Location == i.Location))
                    continue;

                string inputName = $"__unused_input_{attribute.Location}";
                string outputName = $"__unused_output_{outputLayoutIndex}";

                attributesLayout.AppendLine($"layout (location = {attribute.Location}) in {attribute.Type} {inputName};");
                attributesLayout.AppendLine($"layout (location = {outputLayoutIndex}) out {attribute.Type} {outputName};");
                attributesAssignment.Append($"{outputName} = {inputName};\n    ");

                outputAttributes.Add(attribute with { Location = outputLayoutIndex++ });
            }

            // we're only using this for fragment shader so let's just assert that.
            Debug.Assert(Type == ShaderPartType.Fragment);

            result = result.Replace("{{ fragment_output_layout }}", attributesLayout.ToString().Trim());
            result = result.Replace("{{ fragment_output_assignment }}", attributesAssignment.ToString().Trim());

            var part = new VeldridShaderPart(result, header, Type, store);
            part.Inputs.AddRange(Inputs.Concat(attributes).DistinctBy(a => a.Location));
            part.Outputs.AddRange(Outputs.Concat(outputAttributes));
            return part;
        }

        public string GetRawText() => header + '\n' + code;

        #region IDisposable Support

        public void Dispose()
        {
        }

        #endregion
    }

    public record struct VeldridShaderAttribute(int Location, string Type);
}
