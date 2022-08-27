// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using osu.Framework.Graphics.Shaders;
using osuTK;
using Veldrid;

namespace osu.Framework.Graphics.Veldrid.Shaders
{
    internal class VeldridUniformGroup : IVeldridUniformGroup
    {
        private readonly List<VeldridUniformInfo> uniforms = new List<VeldridUniformInfo>();

        /// <summary>
        /// All uniforms declared by any shader part in the <see cref="VeldridShader"/>.
        /// </summary>
        public IReadOnlyList<VeldridUniformInfo> Uniforms => uniforms;

        public VeldridUniformGroup(params IVeldridUniformGroup[] groups)
        {
            foreach (var group in groups)
            {
                foreach (var uniform in group.Uniforms)
                    addUniform(uniform);
            }
        }

        public void AddUniform(string name, string type, string precision) => addUniform(new VeldridUniformInfo
        {
            Name = name,
            Type = type,
            Precision = precision
        });

        private void addUniform(VeldridUniformInfo uniform)
        {
            // todo: make uniform with higher precision override lower uniform
            if (uniforms.Any(u => u.Name == uniform.Name))
                return;

            uniforms.Add(uniform);
        }

        /// <summary>
        /// Creates a uniform buffer object for all uniforms in this group.
        /// </summary>
        /// <param name="renderer">The renderer to create the uniform buffer.</param>
        /// <param name="owner">The owner of the uniforms.</param>
        /// <param name="uniforms">A list of <see cref="IUniform"/>s instantiated for the buffer structure.</param>
        public DeviceBuffer CreateBuffer(VeldridRenderer renderer, VeldridShader owner, out Dictionary<string, IUniform> uniforms)
        {
            uniforms = new Dictionary<string, IUniform>(Uniforms.Count);

            int bufferSize = 0;

            for (int i = 0; i < Uniforms.Count; i++)
            {
                string name = Uniforms[i].Name;

                IUniform uniform;

                switch (Uniforms[i].Type)
                {
                    case "bool":
                        uniform = createUniform<bool>(renderer, owner, name, ref bufferSize);
                        break;

                    case "float":
                        uniform = createUniform<float>(renderer, owner, name, ref bufferSize);
                        break;

                    case "int":
                        uniform = createUniform<int>(renderer, owner, name, ref bufferSize);
                        break;

                    case "mat3":
                        uniform = createUniform<Matrix3>(renderer, owner, name, ref bufferSize);
                        break;

                    case "mat4":
                        uniform = createUniform<Matrix4>(renderer, owner, name, ref bufferSize);
                        break;

                    case "vec2":
                        uniform = createUniform<Vector2>(renderer, owner, name, ref bufferSize);
                        break;

                    case "vec3":
                        uniform = createUniform<Vector3>(renderer, owner, name, ref bufferSize);
                        break;

                    case "vec4":
                        uniform = createUniform<Vector4>(renderer, owner, name, ref bufferSize);
                        break;

                    case "texture2D":
                        uniform = createUniform<int>(renderer, owner, name, ref bufferSize);
                        break;

                    default:
                        continue;
                }

                uniforms[name] = uniform;
            }

            if (bufferSize % 16 > 0)
                bufferSize += 16 - (bufferSize % 16);

            return renderer.Factory.CreateBuffer(new BufferDescription((uint)bufferSize, BufferUsage.UniformBuffer));
        }

        private static IUniform createUniform<T>(VeldridRenderer renderer, VeldridShader shader, string name, ref int bufferSize)
            where T : unmanaged, IEquatable<T>
        {
            int uniformSize = Marshal.SizeOf<T>();
            int baseAlignment;

            // see https://www.khronos.org/registry/OpenGL/specs/gl/glspec45.core.pdf#page=159.
            if (typeof(T) == typeof(Vector3))
            {
                // vec3 has a base alignment of 4N (vec4).
                baseAlignment = uniformSize = Marshal.SizeOf<Vector4>();
            }
            else if (typeof(T) == typeof(Matrix3) || typeof(T) == typeof(Matrix4))
            {
                // mat3 has a base alignment of vec4 for each column.
                if (typeof(T) == typeof(Matrix3))
                    uniformSize = Marshal.SizeOf<Matrix3x4>();

                baseAlignment = Marshal.SizeOf<Vector4>();
            }
            else
                baseAlignment = uniformSize;

            // offset the location of this uniform with the calculated base alignment.
            if (bufferSize % baseAlignment > 0)
                bufferSize += baseAlignment - (bufferSize % baseAlignment);

            int location = bufferSize;

            bufferSize += uniformSize;

            if (GlobalPropertyManager.CheckGlobalExists(name))
                return new GlobalUniform<T>(renderer, shader, name, location);

            return new Uniform<T>(renderer, shader, name, location);
        }
    }
}
