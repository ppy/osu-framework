// Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System.Collections.Generic;
using osu.Framework.Graphics.OpenGL;

namespace osu.Framework.Graphics
{
    public class DrawNode
    {
        public DrawInfo DrawInfo;

        public Drawable Drawable;

        internal bool IsValid;

        public void DrawSubTree()
        {
            PreDraw();

            GLWrapper.SetBlend(DrawInfo.Blending.Source, DrawInfo.Blending.Destination);

            Draw();

            PostDraw();
        }

        protected virtual void PreDraw()
        {
        }

        protected virtual void Draw()
        {
        }

        protected virtual void PostDraw()
        {
        }
    }
}
