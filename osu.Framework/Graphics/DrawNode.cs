// Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System.Collections.Generic;
using osu.Framework.Graphics.OpenGL;
using osu.Framework.Graphics.Shaders;

namespace osu.Framework.Graphics
{
    public class DrawNode
    {
        public DrawInfo DrawInfo;

        // TODO: Get rid of this. The DrawNode should _never_ access its drawable
        // (I am aware this is currently done for not re-constructing the entire
        // drawnode hierarchy every frame, but this should be avoided nonetheless.)
        public Drawable Drawable;

        internal bool IsValid;

        public void DrawSubTree()
        {
            GLWrapper.SetBlend(DrawInfo.Blending.Source, DrawInfo.Blending.Destination);

            Draw();
        }


        protected virtual void Draw()
        {
        }
    }
}
