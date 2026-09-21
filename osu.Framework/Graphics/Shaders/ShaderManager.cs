// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using osu.Framework.Graphics.Rendering;
using osu.Framework.IO.Stores;

namespace osu.Framework.Graphics.Shaders
{
    public class ShaderManager : IShaderStore, IDisposable
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
        /// Retrieves a usable <see cref="IShader"/> given the vertex and fragment shaders.
        /// </summary>
        /// <param name="vertex">The vertex shader name.</param>
        /// <param name="fragment">The fragment shader name.</param>
        public IShader Load(string vertex, string fragment)
        {
            IShader? cached = GetCachedShader(vertex, fragment);
            if (cached != null)
                return cached;

            return shaderCache[(vertex, fragment)] = renderer.CreateShader(
                $"{vertex}/{fragment}",
                new[]
                {
                    resolveShaderPart(vertex, ShaderPartType.Vertex),
                    resolveShaderPart(fragment, ShaderPartType.Fragment)
                });
        }

        /// <summary>
        /// Attempts to retrieve an already-cached shader.
        /// </summary>
        /// <param name="vertex">The vertex shader name.</param>
        /// <param name="fragment">The fragment shader name.</param>
        /// <returns>A cached <see cref="IShader"/> instance, if existing.</returns>
        public virtual IShader? GetCachedShader(string vertex, string fragment)
            => shaderCache.GetValueOrDefault((vertex, fragment));

        /// <summary>
        /// Attempts to retrieve an already-cached shader part.
        /// </summary>
        /// <param name="name">The name of the shader part.</param>
        /// <returns>A cached <see cref="IShaderPart"/> instance, if existing.</returns>
        public virtual IShaderPart? GetCachedShaderPart(string name)
            => partCache.GetValueOrDefault(name);

        /// <summary>
        /// Attempts to retrieve the raw data for a shader file.
        /// </summary>
        /// <param name="fileName">The file name.</param>
        /// <returns></returns>
        public virtual byte[]? GetRawData(string fileName) => store.Get(fileName);

        private IShaderPart resolveShaderPart(string name, ShaderPartType type)
        {
            name = ensureValidName(name, type);

            IShaderPart? cached = GetCachedShaderPart(name);
            if (cached != null)
                return cached;

            byte[]? rawData = GetRawData(name);

            if (rawData == null)
                throw new FileNotFoundException($"{type} shader part could not be found.", name);

            return partCache[name] = renderer.CreateShaderPart(this, name, rawData, type);
        }

        private string ensureValidName(string name, ShaderPartType partType)
        {
            string ending = string.Empty;
            if (string.IsNullOrEmpty(Path.GetExtension(name)))
                ending = getFileEnding(partType);

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
    }

    public static class FragmentShaderDescriptor
    {
        public const string TEXTURE = "Texture";
        public const string GLOW = "Glow";
        public const string BLUR = "Blur";
        public const string GRAYSCALE = "Grayscale";
        public const string VIDEO = "Video";
    }
}
