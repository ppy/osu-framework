// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using osu.Framework.Graphics.OpenGL;
using OpenTK.Graphics.ES30;

namespace osu.Framework.Graphics.Shaders
{
    internal class ShaderPart : IDisposable
    {
        internal const string BOUNDARY = @"----------------------{0}";

        internal StringBuilder Log = new StringBuilder();

        internal List<AttributeInfo> Attributes = new List<AttributeInfo>();

        internal string Name;
        internal bool HasCode;
        internal bool Compiled;

        internal ShaderType Type;

        private int partID = -1;

        private int lastAttributeIndex;

        private readonly List<string> shaderCodes = new List<string>();

        private readonly Regex includeRegex = new Regex("^\\s*#\\s*include\\s+[\"<](.*)[\">]");
        private readonly Regex attributeRegex = new Regex("^\\s*attribute\\s+[^\\s]+\\s+([^;]+);");

        private readonly ShaderManager manager;

        internal ShaderPart(string name, byte[] data, ShaderType type, ShaderManager manager)
        {
            Name = name;
            Type = type;

            this.manager = manager;

            shaderCodes.Add(loadFile(data));
            shaderCodes.RemoveAll(string.IsNullOrEmpty);

            if (shaderCodes.Count == 0)
                return;

            HasCode = true;
        }

        private string loadFile(byte[] bytes)
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
                        continue;

                    Match includeMatch = includeRegex.Match(line);
                    if (includeMatch.Success)
                    {
                        string includeName = includeMatch.Groups[1].Value.Trim();

                        //#if DEBUG
                        //                        byte[] rawData = null;
                        //                        if (File.Exists(includeName))
                        //                            rawData = File.ReadAllBytes(includeName);
                        //#endif
                        shaderCodes.Add(loadFile(manager.LoadRaw(includeName)));
                    }
                    else
                        code += '\n' + line;

                    Match attributeMatch = attributeRegex.Match(line);
                    if (attributeMatch.Success)
                    {
                        Attributes.Add(new AttributeInfo
                        {
                            Location = lastAttributeIndex++,
                            Name = attributeMatch.Groups[1].Value.Trim()
                        });
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

#if DEBUG
            string compileLog = GL.GetShaderInfoLog(this);
            Log.AppendLine(string.Format('\t' + BOUNDARY, Name));
            Log.AppendLine($"\tCompiled: {Compiled}");
            if (!Compiled)
            {
                Log.AppendLine("\tLog:");
                Log.AppendLine('\t' + compileLog);
            }
#endif

            if (!Compiled)
                Dispose(true);

            return Compiled;
        }

        public static implicit operator int(ShaderPart program)
        {
            return program.partID;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected void Dispose(bool disposing)
        {
            if (!disposing || partID == -1) return;

            GLWrapper.DeleteShader(this);
            Compiled = false;
            partID = -1;
        }
    }
}
