﻿// Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Graphics.OpenGL;
using osu.Framework.Graphics.Batches;

namespace osu.Framework.Graphics
{
    public class DrawNode
    {
        public DrawInfo DrawInfo;
        public long InvalidationID;

        public virtual void Draw(IVertexBatch vertexBatch)
        {
            GLWrapper.SetBlend(DrawInfo.Blending);
        }
    }
}
