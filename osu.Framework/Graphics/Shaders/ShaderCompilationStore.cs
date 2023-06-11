// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using Newtonsoft.Json;
using osu.Framework.Extensions;
using osu.Framework.Logging;
using osu.Framework.Platform;
using Veldrid;
using Veldrid.SPIRV;

namespace osu.Framework.Graphics.Shaders
{
    public class ShaderCompilationStore
    {
        public Storage? CacheStorage { private get; set; }

        public VertexFragmentShaderCompilation CompileVertexFragment(string vertexText, string fragmentText, CrossCompileTarget target)
        {
            // vertexHash#fragmentHash#target
            string filename = $"{vertexText.ComputeMD5Hash()}#{fragmentText.ComputeMD5Hash()}#{(int)target}";

            if (tryGetCached(filename, out VertexFragmentShaderCompilation? existing))
            {
                existing.WasCached = true;
                return existing;
            }

            // Debug preserves names for reflection.
            byte[] vertexBytes = SpirvCompilation.CompileGlslToSpirv(vertexText, null, ShaderStages.Vertex, new GlslCompileOptions(true)).SpirvBytes;
            byte[] fragmentBytes = SpirvCompilation.CompileGlslToSpirv(fragmentText, null, ShaderStages.Fragment, new GlslCompileOptions(true)).SpirvBytes;

            VertexFragmentCompilationResult crossResult = SpirvCompilation.CompileVertexFragment(vertexBytes, fragmentBytes, target, new CrossCompileOptions());
            VertexFragmentShaderCompilation compilation = new VertexFragmentShaderCompilation
            {
                VertexBytes = vertexBytes,
                FragmentBytes = fragmentBytes,
                VertexText = crossResult.VertexShader,
                FragmentText = crossResult.FragmentShader,
                Reflection = crossResult.Reflection
            };

            saveToCache(filename, compilation);

            return compilation;
        }

        public ComputeProgramCompilation CompileCompute(string programText, CrossCompileTarget target)
        {
            // programHash#target
            string filename = $"{programText.ComputeMD5Hash()}#{(int)target}";

            if (tryGetCached(filename, out ComputeProgramCompilation? existing))
            {
                existing.WasCached = true;
                return existing;
            }

            // Debug preserves names for reflection.
            byte[] programBytes = SpirvCompilation.CompileGlslToSpirv(programText, null, ShaderStages.Compute, new GlslCompileOptions(true)).SpirvBytes;

            ComputeCompilationResult crossResult = SpirvCompilation.CompileCompute(programBytes, target, new CrossCompileOptions());
            ComputeProgramCompilation compilation = new ComputeProgramCompilation
            {
                ProgramBytes = programBytes,
                ProgramText = crossResult.ComputeShader,
                Reflection = crossResult.Reflection
            };

            saveToCache(filename, compilation);

            return compilation;
        }

        private bool tryGetCached<T>(string filename, [NotNullWhen(true)] out T? compilation)
            where T : class
        {
            compilation = null;

            try
            {
                if (CacheStorage == null)
                    return false;

                if (!CacheStorage.Exists(filename))
                    return false;

                using var stream = CacheStorage.GetStream(filename);
                using var br = new BinaryReader(stream);

                string checksum = br.ReadString();
                string data = br.ReadString();

                if (data.ComputeMD5Hash() != checksum)
                {
                    // Data corrupted..
                    Logger.Log("Cached shader data is corrupted - recompiling.");
                    return false;
                }

                compilation = JsonConvert.DeserializeObject<T>(data)!;
                return true;
            }
            catch (Exception e)
            {
                Logger.Error(e, "Failed to read cached shader compilation - recompiling.");
            }

            return false;
        }

        private void saveToCache(string filename, object compilation)
        {
            if (CacheStorage == null)
                return;

            try
            {
                // ensure any stale cached versions are deleted.
                CacheStorage.Delete(filename);

                using var stream = CacheStorage.CreateFileSafely(filename);
                using var bw = new BinaryWriter(stream);

                string data = JsonConvert.SerializeObject(compilation);
                string checksum = data.ComputeMD5Hash();

                bw.Write(checksum);
                bw.Write(data);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to save shader to cache.");
            }
        }
    }
}
