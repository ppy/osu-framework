// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
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

        private readonly List<string> shaderCodes = new List<string>();
        private readonly ShaderManager manager;

        public VeldridShaderPart(byte[]? data, ShaderPartType type, ShaderManager manager)
        {
            this.manager = manager;

            Type = type;

            // Load the shader files.
            shaderCodes.Add(loadFile(data, true));

            int lastInputIndex = 0;

            // Parse all shader inputs to find the last input index.
            for (int i = 0; i < shaderCodes.Count; i++)
            {
                foreach (Match m in shader_input_pattern.Matches(shaderCodes[i]))
                    lastInputIndex = Math.Max(lastInputIndex, int.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture));
            }

            // Update the location of the m_BackbufferDrawDepth input to be placed after all other inputs.
            for (int i = 0; i < shaderCodes.Count; i++)
                shaderCodes[i] = shaderCodes[i].Replace("layout(location = -1)", $"layout(location = {lastInputIndex + 1})");

            // Increment the binding set of all uniform blocks.
            // After this transformation, the g_GlobalUniforms block is placed in set 0 and all other user blocks begin from 1.
            // The difference in implementation here (compared to above) is intentional, as uniform blocks must be consistent between the shader stages, so they can't be easily appended.
            for (int i = 0; i < shaderCodes.Count; i++)
            {
                shaderCodes[i] = uniform_pattern.Replace(shaderCodes[i],
                    match => $"{match.Groups[1].Value}set = {int.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture) + 1}{match.Groups[3].Value}");
            }
        }

        private string loadFile(byte[]? bytes, bool mainFile)
        {
            if (bytes == null)
                return string.Empty;

            using (MemoryStream ms = new MemoryStream(bytes))
            using (StreamReader sr = new StreamReader(ms))
            {
                string code = string.Empty;

                while (sr.Peek() != -1)
                {
                    string? line = sr.ReadLine();

                    if (string.IsNullOrEmpty(line))
                    {
                        code += line + '\n';
                        continue;
                    }

                    if (line.StartsWith("#version", StringComparison.Ordinal)) // the version directive has to appear before anything else in the shader
                    {
                        shaderCodes.Insert(0, line + '\n');
                        continue;
                    }

                    if (line.StartsWith("#extension", StringComparison.Ordinal))
                    {
                        shaderCodes.Add(line + '\n');
                        continue;
                    }

                    Match includeMatch = include_pattern.Match(line);

                    if (includeMatch.Success)
                    {
                        string includeName = includeMatch.Groups[1].Value.Trim();

                        //#if DEBUG
                        //                        byte[] rawData = null;
                        //                        if (File.Exists(includeName))
                        //                            rawData = File.ReadAllBytes(includeName);
                        //#endif
                        code += loadFile(manager.LoadRaw(includeName), false) + '\n';
                    }
                    else
                        code += line + '\n';
                }

                if (mainFile)
                {
                    string internalIncludes = loadFile(manager.LoadRaw("Internal/sh_Compatibility.h"), false) + "\n";
                    internalIncludes += loadFile(manager.LoadRaw("Internal/sh_GlobalUniforms.h"), false) + "\n";
                    code = internalIncludes + code;

                    string outputCode = loadFile(manager.LoadRaw($"Internal/sh_{Type}_Output.h"), false);

                    if (!string.IsNullOrEmpty(outputCode))
                    {
                        string realMainName = "real_main_" + Guid.NewGuid().ToString("N");

                        outputCode = outputCode.Replace("{{ real_main }}", realMainName);
                        code = Regex.Replace(code, @"void main\((.*)\)", $"void {realMainName}()") + outputCode + '\n';

                        // In D3D11, unused fragment inputs cull their corresponding vertex output, which affects the vertex input/output structure leading to seemingly undefined behaviour.
                        // To prevent this from happening, make sure all fragment inputs are sent in the output so that D3D11 doesn't consider them "unused".
                        if (Type == ShaderPartType.Fragment)
                        {
                            int fragmentOutputLayoutIndex = shader_output_pattern.Matches(code).Count;

                            var fragmentOutputLayout = new StringBuilder();
                            var fragmentOutputAssignment = new StringBuilder();

                            foreach (Match m in shader_input_pattern.Matches(code).DistinctBy(m => m.Groups[3].Value))
                            {
                                fragmentOutputLayout.AppendLine($"layout (location = {fragmentOutputLayoutIndex++}) out {m.Groups[2].Value} o_{m.Groups[3].Value};");
                                fragmentOutputAssignment.AppendLine($"o_{m.Groups[3].Value} = {m.Groups[3].Value};");
                            }

                            code = code.Replace("{{ fragment_output_layout }}", fragmentOutputLayout.ToString());
                            code = code.Replace("{{ fragment_output_assignment }}", fragmentOutputAssignment.ToString());
                        }
                    }
                }

                return code;
            }
        }

        public string GetRawText() => string.Join('\n', shaderCodes);

        #region IDisposable Support

        public void Dispose()
        {
        }

        #endregion
    }
}
