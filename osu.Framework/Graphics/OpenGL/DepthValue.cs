// Copyright (c) 2007-2019 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;

namespace osu.Framework.Graphics.OpenGL
{
    public class DepthValue
    {
        private float depth;

        public DepthValue()
        {
            depth = -1;
        }

        public void Increment()
        {
            depth = Math.Min(1, depth + 0.001f);
        }

        public static implicit operator float(DepthValue d) => d.depth;
    }
}
