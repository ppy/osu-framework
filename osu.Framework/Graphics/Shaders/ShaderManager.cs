﻿// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using osu.Framework.IO.Stores;
using osu.Framework.Logging;
using osuTK.Graphics.ES30;

namespace osu.Framework.Graphics.Shaders
{
    public class ShaderManager
    {
        private const string shader_prefix = @"sh_";

        private readonly ConcurrentDictionary<string, ShaderPart> partCache = new ConcurrentDictionary<string, ShaderPart>();
        private readonly ConcurrentDictionary<(string, string), Shader> shaderCache = new ConcurrentDictionary<(string, string), Shader>();

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

        private ShaderPart createShaderPart(string name, ShaderType type, bool bypassCache = false)
        {
            name = ensureValidName(name, type);

            if (!bypassCache && partCache.TryGetValue(name, out ShaderPart part))
                return part;

            byte[] rawData = LoadRaw(name);

            part = new ShaderPart(name, rawData, type, this);

            //cache even on failure so we don't try and fail every time.
            partCache[name] = part;
            return part;
        }

        public Shader Load(string vertex, string fragment, bool continuousCompilation = false)
        {
            var tuple = (vertex, fragment);

            if (shaderCache.TryGetValue(tuple, out Shader shader))
                return shader;

            List<ShaderPart> parts = new List<ShaderPart>
            {
                createShaderPart(vertex, ShaderType.VertexShader),
                createShaderPart(fragment, ShaderType.FragmentShader)
            };

            shader = new Shader($"{vertex}/{fragment}", parts);

            if (!shader.Loaded)
            {
                StringBuilder logContents = new StringBuilder();
                logContents.AppendLine($@"Loading shader {vertex}/{fragment}");
                logContents.Append(shader.Log);
                foreach (ShaderPart p in parts)
                    logContents.Append(p.Log);
                Logger.Log(logContents.ToString().Trim('\n'), LoggingTarget.Runtime, LogLevel.Debug);
            }

            shaderCache[tuple] = shader;

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
