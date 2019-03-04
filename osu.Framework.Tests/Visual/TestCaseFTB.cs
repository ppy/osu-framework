// Copyright (c) 2007-2019 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using osu.Framework.Graphics;
using osu.Framework.Graphics.OpenGL.Vertices;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Testing;

namespace osu.Framework.Tests.Visual
{
    public class TestCaseFTB : TestCase
    {
        public TestCaseFTB()
        {
            for (int i = 0; i < 500; i++)
            {
                Add(new TestBox { RelativeSizeAxes = Axes.Both });
            }
        }

        private class TestBox : Box
        {
            protected override DrawNode CreateDrawNode() => new TestBoxDrawNode();

            private class TestBoxDrawNode : BoxDrawNode
            {
                public override void DrawHull(Action<TexturedVertex2D> vertexAction, ref float vertexDepth)
                {
                    base.DrawHull(vertexAction, ref vertexDepth);
                }
            }
        }
    }
}
