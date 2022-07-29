// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using osu.Framework.Graphics.OpenGL.Shaders;
using osu.Framework.Graphics.Rendering;
using osu.Framework.Graphics.Shaders;
using osuTK.Graphics.ES30;

namespace osu.Framework.Graphics.OpenGL
{
    public class OpenGLRenderer : IRenderer
    {
        IShaderPart IRenderer.CreateShaderPart(ShaderManager manager, string name, byte[]? rawData, ShaderPartType partType)
        {
            ShaderType glType;

            switch (partType)
            {
                case ShaderPartType.Fragment:
                    glType = ShaderType.FragmentShader;
                    break;

                case ShaderPartType.Vertex:
                    glType = ShaderType.VertexShader;
                    break;

                default:
                    throw new ArgumentException($"Unsupported shader part type: {partType}", nameof(partType));
            }

            return new OpenGLShaderPart(name, rawData, glType, manager);
        }

        IShader IRenderer.CreateShader(string name, params IShaderPart[] parts) => new OpenGLShader(name, parts.Cast<OpenGLShaderPart>().ToArray());
    }
}
