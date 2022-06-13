// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System.Linq;
using NUnit.Framework;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Testing;

namespace osu.Framework.Tests.Visual.Testing
{
    public class TestSceneTestingExtensions : FrameworkTestScene
    {
        [Test]
        public void TestChildrenOfTypeMatchingComposite()
        {
            Container container = null;

            AddStep("create children", () =>
            {
                Child = container = new Container { Child = new Box() };
            });

            AddAssert("ChildrenOfType returns 2 children", () => container.ChildrenOfType<Drawable>().Count() == 2);
        }
    }
}
