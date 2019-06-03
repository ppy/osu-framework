// Copyright (c) 2007-2019 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using osu.Framework.Graphics;
using osu.Framework.Graphics.OpenGL;
using osu.Framework.Graphics.OpenGL.Vertices;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Testing;
using osuTK;

namespace osu.Framework.Tests.Visual
{
    public class TestSceneFTB : TestScene
    {
        public TestSceneFTB()
        {
            for (int i = 0; i < 10000; i++)
            {
                Add(new TestBox { Size = new Vector2(200) });
            }
        }

        private class TestBox : Box
        {
            protected override DrawNode CreateDrawNode() => new TestBoxDrawNode(this);

            private class TestBoxDrawNode : BoxDrawNode
            {
                public TestBoxDrawNode(Box source)
                    : base(source)
                {
                }
            }
        }
    }
}
