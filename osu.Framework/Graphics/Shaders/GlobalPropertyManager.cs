// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu/master/LICENCE

using System;
using System.Collections.Generic;
using System.Linq;

namespace osu.Framework.Graphics.Shaders
{
    internal static class GlobalPropertyManager
    {
        private static readonly List<Shader> all_shaders = new List<Shader>();
        private static readonly UniformMapping[] global_properties;

        static GlobalPropertyManager()
        {
            var values = Enum.GetValues(typeof(GlobalProperty)).OfType<GlobalProperty>().ToArray();

            global_properties = new UniformMapping[values.Length];
            foreach (var v in values)
                global_properties[(int)v] = new UniformMapping($"g_{v}");
        }

        /// <summary>
        /// Sets a uniform for all shaders that contain this property.
        /// <para>Any future-initialized shaders will also have this uniform set.</para>
        /// </summary>
        /// <param name="property">The uniform.</param>
        /// <param name="value">The uniform value.</param>
        public static void Set(GlobalProperty property, object value) => global_properties[(int)property].Value = value;

        public static void InitializeShader(Shader shader)
        {
            // transfer all existing global properties across.
            foreach (var global in global_properties)
            {
                if (!shader.Uniforms.TryGetValue(global.Name, out UniformBase uniform))
                    continue;

                uniform.Value = global.Value;
                global.LinkedUniforms.Add(uniform);
            }

            all_shaders.Add(shader);
        }

        public static void Remove(Shader shader) => all_shaders.Remove(shader);
    }
}
