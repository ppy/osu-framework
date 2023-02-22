// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics.Rendering;
using osu.Framework.Graphics.Shaders;
using osuTK.Graphics.ES30;

namespace osu.Framework.Graphics.OpenGLCore.Shaders
{
    internal class GLESShaderPart : GLCoreShaderPart
    {
        protected override string GLSLVersion => "300 es";

        internal GLESShaderPart(IRenderer renderer, string name, byte[]? data, ShaderType type, ShaderManager manager)
            : base(renderer, name, data, type, manager)
        {
        }
    }
}
