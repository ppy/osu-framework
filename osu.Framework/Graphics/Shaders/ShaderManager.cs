// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text;
using osu.Framework.IO.Stores;
using osu.Framework.Logging;
using OpenTK.Graphics.ES30;

namespace osu.Framework.Graphics.Shaders
{
    public class ShaderManager
    {
        private const string shader_prefix = @"sh_";

        private ConcurrentDictionary<string, ShaderPart> partCache = new ConcurrentDictionary<string, ShaderPart>();
        private ConcurrentDictionary<string, Shader> shaderCache = new ConcurrentDictionary<string, Shader>();

        private ResourceStore<byte[]> store;

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

        internal byte[] LoadRaw(string name)
        {
            byte[] rawData = null;

#if DEBUG
            if (File.Exists(name))
                rawData = File.ReadAllBytes(name);
#endif

            if (rawData == null)
            {
                rawData = store.Get(name);

                if (rawData == null)
                    return null;
            }

            return rawData;
        }

        private ShaderPart createShaderPart(string name, ShaderType type, bool bypassCache = false)
        {
            name = ensureValidName(name, type);

            ShaderPart part;
            if (!bypassCache && partCache.TryGetValue(name, out part))
                return part;

            byte[] rawData = LoadRaw(name);

            part = new ShaderPart(name, rawData, type, this);

            //cache even on failure so we don't try and fail every time.
            partCache[name] = part;
            return part;
        }

        public Shader Load(string vertex, string fragment, bool continuousCompilation = false)
        {
            string name = $@"{vertex}/{fragment}";

            if (shaderCache.ContainsKey(name))
                return shaderCache[name];

            List<ShaderPart> parts = new List<ShaderPart>
            {
                createShaderPart(vertex, ShaderType.VertexShader),
                createShaderPart(fragment, ShaderType.FragmentShader)
            };

            Shader shader = new Shader(name, parts);

#if !DEBUG
            if (!shader.Loaded)
#endif
            {
                StringBuilder logContents = new StringBuilder();
                logContents.AppendLine($@"Loading shader {name}:");
                logContents.Append(shader.Log);
                logContents.AppendLine(@"Parts:");
                foreach (ShaderPart p in parts)
                    logContents.Append(p.Log);
                Logger.Log(logContents.ToString(), LoggingTarget.Runtime, LogLevel.Debug);
            }

            //#if DEBUG
            //            if (continuousCompilation)
            //            {
            //                Game.Scheduler.AddDelayed(delegate
            //                {
            //                    parts.Clear();
            //                    parts.Add(createShaderPart(vertex, ShaderType.VertexShader, true));
            //                    parts.Add(createShaderPart(fragment, ShaderType.FragmentShader, true));
            //                    shader.Compile(parts);

            //                    StringBuilder cLogContents = new StringBuilder();
            //                    cLogContents.AppendLine($@"Continuously loading shader {name}:");
            //                    cLogContents.Append(shader.Log);
            //                    cLogContents.AppendLine(@"Parts:");
            //                    foreach (ShaderPart p in parts)
            //                        cLogContents.Append(p.Log);

            //                }, 1000, true);
            //            }
            //#endif

            shaderCache[name] = shader;
            return shader;
        }

        public Shader Load<T, U>(T vertexShader, U fragmentShader, bool continuousCompilation = false)
        {
            return Load(vertexShader.ToString(), fragmentShader.ToString(), continuousCompilation);
        }
    }

    public enum VertexShaderDescriptor
    {
        Texture2D,
        Texture3D,
        Position,
        Colour,
    }

    public enum FragmentShaderDescriptor
    {
        Texture,
        TextureRounded,
        Colour,
        ColourRounded,
        Glow,
        Blur,
    }
}
