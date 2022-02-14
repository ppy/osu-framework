// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable enable

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using osu.Framework.Graphics.OpenGL;
using osu.Framework.IO.Stores;
using osuTK.Graphics.ES30;

namespace osu.Framework.Graphics.Shaders
{
    public class ShaderManager : IDisposable
    {
        private const string shader_prefix = @"sh_";

        private readonly ConcurrentDictionary<string, ShaderPart> partCache = new ConcurrentDictionary<string, ShaderPart>();
        private readonly ConcurrentDictionary<(string, string), Shader> shaderCache = new ConcurrentDictionary<(string, string), Shader>();

        private readonly IResourceStore<byte[]> store;

        /// <summary>
        /// Constructs a new <see cref="ShaderManager"/>.
        /// </summary>
        public ShaderManager(IResourceStore<byte[]> store)
        {
            this.store = store;
        }

        /// <summary>
        /// Retrieves raw shader data from the store.
        /// Use <see cref="Load"/> to retrieve a usable <see cref="IShader"/> instead.
        /// </summary>
        /// <param name="name">The shader name.</param>
        public virtual byte[]? LoadRaw(string name) => store.Get(name);

        /// <summary>
        /// Retrieves a usable <see cref="IShader"/> given the vertex and fragment shaders.
        /// </summary>
        /// <param name="vertex">The vertex shader name.</param>
        /// <param name="fragment">The fragment shader name.</param>
        /// <param name="continuousCompilation"></param>
        public IShader Load(string vertex, string fragment, bool continuousCompilation = false)
        {
            var tuple = (vertex, fragment);

            if (shaderCache.TryGetValue(tuple, out Shader? shader))
                return shader;

            List<ShaderPart> parts = new List<ShaderPart>
            {
                createShaderPart(vertex, ShaderType.VertexShader),
                createShaderPart(fragment, ShaderType.FragmentShader)
            };

            return shaderCache[tuple] = CreateShader($"{vertex}/{fragment}", parts);
        }

        internal virtual Shader CreateShader(string name, List<ShaderPart> parts) => new Shader(name, parts);

        private ShaderPart createShaderPart(string name, ShaderType type, bool bypassCache = false)
        {
            name = ensureValidName(name, type);

            if (!bypassCache && partCache.TryGetValue(name, out ShaderPart? part))
                return part;

            byte[]? rawData = LoadRaw(name);

            part = new ShaderPart(name, rawData, type, this);

            //cache even on failure so we don't try and fail every time.
            partCache[name] = part;
            return part;
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

        #region IDisposable Support

        private bool isDisposed;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!isDisposed)
            {
                isDisposed = true;

                store.Dispose();

                GLWrapper.ScheduleDisposal(s =>
                {
                    foreach (var shader in s.shaderCache.Values)
                        shader.Dispose();

                    foreach (var part in s.partCache.Values)
                        part.Dispose();
                }, this);
            }
        }

        #endregion
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
        public const string VIDEO = "Video";
        public const string VIDEO_ROUNDED = "VideoRounded";
    }
}
