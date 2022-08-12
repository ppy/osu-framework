// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Concurrent;
using osu.Framework.Graphics.Rendering;
using osu.Framework.IO.Stores;

namespace osu.Framework.Graphics.Shaders
{
    public class ShaderManager : IDisposable
    {
        private const string shader_prefix = @"sh_";

        private readonly ConcurrentDictionary<string, IShaderPart> partCache = new ConcurrentDictionary<string, IShaderPart>();
        private readonly ConcurrentDictionary<(string, string), IShader> shaderCache = new ConcurrentDictionary<(string, string), IShader>();

        private readonly IRenderer renderer;
        private readonly IResourceStore<byte[]> store;

        /// <summary>
        /// Constructs a new <see cref="ShaderManager"/>.
        /// </summary>
        public ShaderManager(IRenderer renderer, IResourceStore<byte[]> store)
        {
            this.renderer = renderer;
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

            if (shaderCache.TryGetValue(tuple, out IShader? shader))
                return shader;

            return shaderCache[tuple] = CreateShader(
                renderer,
                $"{vertex}/{fragment}",
                createShaderPart(vertex, ShaderPartType.Vertex),
                createShaderPart(fragment, ShaderPartType.Fragment));
        }

        internal virtual IShader CreateShader(IRenderer renderer, string name, params IShaderPart[] parts) => renderer.CreateShader(name, parts);

        private IShaderPart createShaderPart(string name, ShaderPartType partType, bool bypassCache = false)
        {
            name = ensureValidName(name, partType);

            if (!bypassCache && partCache.TryGetValue(name, out IShaderPart? part))
                return part;

            byte[]? rawData = LoadRaw(name);

            part = renderer.CreateShaderPart(this, name, rawData, partType);

            //cache even on failure so we don't try and fail every time.
            partCache[name] = part;
            return part;
        }

        private string ensureValidName(string name, ShaderPartType partType)
        {
            string ending = getFileEnding(partType);

            if (!name.StartsWith(shader_prefix, StringComparison.Ordinal))
                name = shader_prefix + name;
            if (name.EndsWith(ending, StringComparison.Ordinal))
                return name;

            return name + ending;
        }

        private string getFileEnding(ShaderPartType partType)
        {
            switch (partType)
            {
                case ShaderPartType.Fragment:
                    return @".fs";

                case ShaderPartType.Vertex:
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

                renderer.ScheduleDisposal(s =>
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
