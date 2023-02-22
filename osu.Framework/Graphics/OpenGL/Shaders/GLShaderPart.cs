// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using osu.Framework.Graphics.Rendering;
using osu.Framework.Graphics.Shaders;
using osuTK.Graphics.ES30;

namespace osu.Framework.Graphics.OpenGL.Shaders
{
    internal class GLShaderPart : IShaderPart
    {
        public static readonly Regex VERTEX_SHADER_INPUT_PATTERN = new Regex(@"^\s*(?>IN\(\s*-?\d+\s*\))\s+(?:(?:lowp|mediump|highp)\s+)?\w+\s+(\w+)");
        private static readonly Regex fragment_shader_output_pattern = new Regex(@"^\s*(?>OUT\(\s*-?\d+\s*\))\s+(?:(?:lowp|mediump|highp)\s+)?\w+\s+(\w+)", RegexOptions.Multiline);
        private static readonly Regex include_pattern = new Regex(@"^\s*#\s*include\s+[""<](.*)["">]");

        internal List<ShaderInputInfo> ShaderInputs = new List<ShaderInputInfo>();

        private readonly IRenderer renderer;
        internal string Name;
        internal bool HasCode;
        internal bool Compiled;

        internal ShaderType Type;

        protected virtual string InternalResourceNamespace => "GL";

        private bool isVertexShader => Type == ShaderType.VertexShader || Type == ShaderType.VertexShaderArb;

        private int partID = -1;

        private int lastShaderInputIndex;

        private readonly List<string> shaderCodes = new List<string>();

        private readonly ShaderManager manager;

        internal GLShaderPart(IRenderer renderer, string name, byte[] data, ShaderType type, ShaderManager manager)
        {
            this.renderer = renderer;
            Name = name;
            Type = type;

            this.manager = manager;

            shaderCodes.Add(loadFile(data, true));
            shaderCodes.RemoveAll(string.IsNullOrEmpty);

            if (shaderCodes.Count == 0)
                return;

            HasCode = true;
        }

        private string loadFile(byte[] bytes, bool mainFile)
        {
            if (bytes == null)
                return null;

            using (MemoryStream ms = new MemoryStream(bytes))
            using (StreamReader sr = new StreamReader(ms))
            {
                string code = string.Empty;

                while (sr.Peek() != -1)
                {
                    string line = sr.ReadLine();

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

                    if (Type == ShaderType.VertexShader || Type == ShaderType.VertexShaderArb)
                    {
                        Match inputMatch = VERTEX_SHADER_INPUT_PATTERN.Match(line);

                        if (inputMatch.Success)
                        {
                            ShaderInputs.Add(new ShaderInputInfo
                            {
                                Location = lastShaderInputIndex++,
                                Name = inputMatch.Groups[1].Value.Trim()
                            });
                        }
                    }
                }

                if (mainFile)
                {
                    string internalIncludes = loadFile(manager.LoadRaw("Internal/sh_Precision.h"), false) + "\n";

                    internalIncludes += loadFile(manager.LoadRaw($"Internal/{InternalResourceNamespace}/sh_Compatibility.h"), false) + "\n";

                    internalIncludes += loadFile(manager.LoadRaw($"Internal/sh_GlobalUniforms.h"), false) + "\n";

                    if (isVertexShader)
                        internalIncludes += loadFile(manager.LoadRaw($"Internal/{InternalResourceNamespace}/sh_VertexShader.h"), false) + "\n";
                    else
                        internalIncludes += loadFile(manager.LoadRaw($"Internal/{InternalResourceNamespace}/sh_FragmentShader.h"), false) + "\n";

                    code = internalIncludes + code;

                    if (isVertexShader)
                    {
                        string backbufferCode = loadFile(manager.LoadRaw("Internal/sh_Vertex_Output.h"), false);

                        if (!string.IsNullOrEmpty(backbufferCode))
                        {
                            string realMainName = "real_main_" + Guid.NewGuid().ToString("N");

                            backbufferCode = backbufferCode.Replace("{{ real_main }}", realMainName);
                            code = Regex.Replace(code, @"void main\((.*)\)", $"void {realMainName}()") + backbufferCode + '\n';
                        }
                    }
                    else
                    {
                        string outputCode = loadFile(manager.LoadRaw($"Internal/{InternalResourceNamespace}/sh_Fragment_Output.h"), false);

                        if (!string.IsNullOrEmpty(outputCode))
                        {
                            string tempVar = fragment_shader_output_pattern.Match(code).Groups[1].Value;
                            string realMainName = "real_main_" + Guid.NewGuid().ToString("N");

                            outputCode = outputCode.Replace("{{ real_main }}", realMainName);
                            outputCode = outputCode.Replace("{{ temp_variable }}", tempVar);

                            code = Regex.Replace(code, @"void main\((.*)\)", $"void {realMainName}()") + outputCode + '\n';
                        }
                    }
                }

                return code;
            }
        }

        internal bool Compile()
        {
            if (!HasCode)
                return false;

            if (partID == -1)
                partID = GL.CreateShader(Type);

            int[] codeLengths = new int[shaderCodes.Count];
            for (int i = 0; i < shaderCodes.Count; i++)
                codeLengths[i] = shaderCodes[i].Length;

            GL.ShaderSource(this, shaderCodes.Count, shaderCodes.ToArray(), codeLengths);
            GL.CompileShader(this);

            GL.GetShader(this, ShaderParameter.CompileStatus, out int compileResult);
            Compiled = compileResult == 1;

            if (!Compiled)
                throw new GLShader.PartCompilationFailedException(Name, GL.GetShaderInfoLog(this));

            return Compiled;
        }

        public static implicit operator int(GLShaderPart program) => program.partID;

        #region IDisposable Support

        protected internal bool IsDisposed { get; private set; }

        ~GLShaderPart()
        {
            renderer.ScheduleDisposal(s => s.Dispose(false), this);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!IsDisposed)
            {
                IsDisposed = true;

                if (partID != -1)
                    GL.DeleteShader(this);
            }
        }

        #endregion
    }
}
