// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using NUnit.Framework;
using osu.Framework.Graphics;
using osuTK;

namespace osu.Framework.Tests.Primitives
{
    [TestFixture]
    public class Vector2ExtensionsTest
    {
        [Test]
        public void TestClockwiseOrientation()
        {
            var vertices = new[]
            {
                new Vector2(0, 1),
                Vector2.One,
                new Vector2(1, 0),
                Vector2.Zero
            };

            float orientation = Vector2Extensions.GetOrientation(vertices);

            Assert.That(orientation, Is.EqualTo(2).Within(0.001));
        }

        [Test]
        public void TestCounterClockwiseOrientation()
        {
            var vertices = new[]
            {
                Vector2.Zero,
                new Vector2(1, 0),
                Vector2.One,
                new Vector2(0, 1),
            };

            float orientation = Vector2Extensions.GetOrientation(vertices);

            Assert.That(orientation, Is.EqualTo(-2).Within(0.001));
        }
    }
}
