// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Collections.Generic;
using System.Linq;
using osuTK;

namespace osu.Framework.Graphics.Shaders
{
    internal static class GlobalPropertyManager
    {
        private static readonly HashSet<IShader> all_shaders = new HashSet<IShader>();
        private static readonly IUniformMapping[] global_properties;

        static GlobalPropertyManager()
        {
            var values = Enum.GetValues(typeof(GlobalProperty)).OfType<GlobalProperty>().ToArray();

            global_properties = new IUniformMapping[values.Length];

            global_properties[(int)GlobalProperty.ProjMatrix] = new UniformMapping<Matrix4>("g_ProjMatrix");
            global_properties[(int)GlobalProperty.GammaCorrection] = new UniformMapping<bool>("g_GammaCorrection");
            global_properties[(int)GlobalProperty.WrapModeS] = new UniformMapping<int>("g_WrapModeS");
            global_properties[(int)GlobalProperty.WrapModeT] = new UniformMapping<int>("g_WrapModeT");
            global_properties[(int)GlobalProperty.MaskingBlockSampler] = new UniformMapping<int>("g_MaskingBlockSampler");
            global_properties[(int)GlobalProperty.MaskingTextureSize] = new UniformMapping<Vector2>("g_MaskingTexSize");

            // Backbuffer internals
            global_properties[(int)GlobalProperty.BackbufferDraw] = new UniformMapping<bool>("g_BackbufferDraw");

            Set(GlobalProperty.MaskingBlockSampler, 10);
        }

        /// <summary>
        /// Sets a uniform for all shaders that contain this property.
        /// <para>Any future-initialized shaders will also have this uniform set.</para>
        /// </summary>
        /// <param name="property">The uniform.</param>
        /// <param name="value">The uniform value.</param>
        public static void Set<T>(GlobalProperty property, T value)
            where T : unmanaged, IEquatable<T>
        {
            ((UniformMapping<T>)global_properties[(int)property]).UpdateValue(ref value);
        }

        public static void Register(IShader shader)
        {
            if (!all_shaders.Add(shader)) return;

            // transfer all existing global properties across.
            foreach (var global in global_properties)
            {
                if (!shader.Uniforms.TryGetValue(global.Name, out IUniform uniform))
                    continue;

                global.LinkShaderUniform(uniform);
            }
        }

        public static void Unregister(IShader shader)
        {
            if (!all_shaders.Remove(shader)) return;

            foreach (var global in global_properties)
            {
                if (!shader.Uniforms.TryGetValue(global.Name, out IUniform uniform))
                    continue;

                global.UnlinkShaderUniform(uniform);
            }
        }

        /// <summary>
        /// Check whether a global uniform exists with the specified name.
        /// </summary>
        /// <param name="name">The name to check</param>
        /// <returns>Whether a global exists.</returns>
        public static bool CheckGlobalExists(string name) => global_properties.Any(m => m.Name == name);
    }
}
