// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics.Containers;
using osu.Framework.Testing;

namespace osu.Framework.Tests.Visual.Drawables
{
    public class TestSceneTransformSequence : GridTestScene
    {
        public readonly Container[] Boxes;

        public TestSceneTransformSequence()
            : base(4, 3)
        {
            Boxes = new Container[Rows * Cols];
        }
    }
}
