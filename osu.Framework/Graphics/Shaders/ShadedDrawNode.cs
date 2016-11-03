// Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Shaders;
using osu.Framework.Graphics.Textures;
using OpenTK.Graphics.ES30;
using OpenTK;
using osu.Framework.Graphics.OpenGL;

namespace osu.Framework.Graphics.Shaders
{
    public class ShadedDrawNode : DrawNode
    {
        public Shader Shader;
    }
}
