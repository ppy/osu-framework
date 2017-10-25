﻿// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using osu.Framework.IO.Stores;
using osu.Framework.Logging;
using OpenTK.Graphics.ES30;

namespace osu.Framework.Graphics.Shaders
{
    public class ShaderManager
    {
        private const string shader_prefix = @"sh_";

        private readonly ConcurrentDictionary<string, ShaderPart> partCache = new ConcurrentDictionary<string, ShaderPart>();
        private readonly ConcurrentDictionary<string, Shader> shaderCache = new ConcurrentDictionary<string, Shader>();

        private readonly ResourceStore<byte[]> store;

        public ShaderManager(ResourceStore<byte[]> store)
        {
            this.store = store;
        }

        private string getFileEnding(ShaderType type)
        {
            switch (type)
            {
                case ShaderType.FragmentShader:
                    return @".fs";
                case ShaderType.VertexShader:
                    return @".vs";
            }

            return string.Empty;
        }

        private string ensureValidName(string name, ShaderType type)
        {
            string ending = getFileEnding(type);
            if (!name.StartsWith(shader_prefix, StringComparison.Ordinal))
                name = shader_prefix + name;
            if (name.EndsWith(ending, StringComparison.Ordinal))
                return name;
            return name + ending;
        }

        internal byte[] LoadRaw(string name) => store.Get(name);

        private ShaderPart createShaderPart<T>(string name, ShaderType type, bool bypassCache = false) where T : ShaderPart, new()
        {
            name = ensureValidName(name, type);

            ShaderPart part;
            if (!bypassCache && partCache.TryGetValue(name, out part))
                return part;

            byte[] rawData = LoadRaw(name);

            part = new T();
            part.Init(name, rawData, type, this);

            //cache even on failure so we don't try and fail every time.
            partCache[name] = part;
            return part;
        }


        public Shader Load(string vertex, string fragment, ShaderSourceType sourceType, bool continuousCompilation = false)
        {
            string name = vertex + '/' + fragment;

            Shader shader;

            if (!shaderCache.TryGetValue(name, out shader))
            {
                List<ShaderPart> parts;

                switch (sourceType)
                {
                    case ShaderSourceType.GLSL:
                        parts = new List<ShaderPart>
                            {
                                createShaderPart<GLSLShaderPart>(vertex, ShaderType.VertexShader),
                                createShaderPart<GLSLShaderPart>(fragment, ShaderType.FragmentShader)
                            };
                        break;
                    default:
                        throw new NotSupportedException("The ShaderSourceType specified is not supported.");
                }

                shader = new Shader(name, parts);

                if (!shader.Loaded)
                {
                    StringBuilder logContents = new StringBuilder();
                    logContents.AppendLine($@"Loading shader {name}:");
                    logContents.Append(shader.Log);
                    logContents.AppendLine(@"Parts:");
                    foreach (ShaderPart p in parts)
                        logContents.Append(p.Log);
                    Logger.Log(logContents.ToString(), LoggingTarget.Runtime, LogLevel.Debug);
                }

                shaderCache[name] = shader;
            }

            return shader;
        }
    }

    public static class VertexShaderDescriptor
    {
        public const string TEXTURE_2 = "Texture2D";
        public const string TEXTURE_3 = "Texture3D";
        public const string POSITION = "Position";
        public const string COLOUR = "Colour";
    }

    public static class FragmentShaderDescriptor
    {
        public const string TEXTURE = "Texture";
        public const string TEXTURE_ROUNDED = "TextureRounded";
        public const string COLOUR = "Colour";
        public const string COLOUR_ROUNDED = "ColourRounded";
        public const string GLOW = "Glow";
        public const string BLUR = "Blur";
    }
}
