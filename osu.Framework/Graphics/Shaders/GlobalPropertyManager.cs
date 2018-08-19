// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Collections.Generic;
using System.Linq;
using OpenTK;

namespace osu.Framework.Graphics.Shaders
{
    internal static class GlobalPropertyManager
    {
        private static readonly List<Shader> all_shaders = new List<Shader>();
        private static readonly IUniformMapping[] global_properties;

        static GlobalPropertyManager()
        {
            var values = Enum.GetValues(typeof(GlobalProperty)).OfType<GlobalProperty>().ToArray();

            global_properties = new IUniformMapping[values.Length];

            global_properties[(int)GlobalProperty.ProjMatrix] = new UniformMapping<Matrix4>("g_ProjMatrix");
            global_properties[(int)GlobalProperty.MaskingRect] = new UniformMapping<Vector4>("g_MaskingRect");
            global_properties[(int)GlobalProperty.ToMaskingSpace] = new UniformMapping<Matrix3>("g_ToMaskingSpace");
            global_properties[(int)GlobalProperty.CornerRadius] = new UniformMapping<float>("g_CornerRadius");
            global_properties[(int)GlobalProperty.BorderThickness] = new UniformMapping<float>("g_BorderThickness");
            global_properties[(int)GlobalProperty.BorderColour] = new UniformMapping<Vector4>("g_BorderColour");
            global_properties[(int)GlobalProperty.MaskingBlendRange] = new UniformMapping<float>("g_MaskingBlendRange");
            global_properties[(int)GlobalProperty.AlphaExponent] = new UniformMapping<float>("g_AlphaExponent");
            global_properties[(int)GlobalProperty.DiscardInner] = new UniformMapping<bool>("g_DiscardInner");
        }

        /// <summary>
        /// Sets a uniform for all shaders that contain this property.
        /// <para>Any future-initialized shaders will also have this uniform set.</para>
        /// </summary>
        /// <param name="property">The uniform.</param>
        /// <param name="value">The uniform value.</param>
        public static void Set<T>(GlobalProperty property, T value)
            where T : struct => ((UniformMapping<T>)global_properties[(int)property]).UpdateValue(ref value);

        public static void InitializeShader(Shader shader)
        {
            // transfer all existing global properties across.
            foreach (var global in global_properties)
            {
                if (!shader.Uniforms.TryGetValue(global.Name, out IUniform uniform))
                    continue;

                global.LinkShaderUniform(uniform);
            }

            all_shaders.Add(shader);
        }

        public static void Remove(Shader shader)
        {
            if (!all_shaders.Remove(shader)) return;

            foreach (var global in global_properties)
            {
                if (!shader.Uniforms.TryGetValue(global.Name, out IUniform uniform))
                    continue;

                global.UnlinkShaderUniform(uniform);
            }
        }
    }
}
