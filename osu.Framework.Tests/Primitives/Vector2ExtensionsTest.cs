// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using NUnit.Framework;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Primitives;
using osuTK;

namespace osu.Framework.Tests.Primitives
{
    [TestFixture]
    public class Vector2ExtensionsTest
    {
        [TestCase(true)]
        [TestCase(false)]
        public void TestArrayOrientation(bool clockwise)
        {
            var vertices = new[]
            {
                new Vector2(0, 1),
                Vector2.One,
                new Vector2(1, 0),
                Vector2.Zero
            };

            if (!clockwise)
                Array.Reverse(vertices);

            float orientation = Vector2Extensions.GetOrientation(vertices);

            Assert.That(orientation, Is.EqualTo(clockwise ? 2 : -2).Within(0.001));
        }

        [TestCase(true)]
        [TestCase(false)]
        public void TestQuadOrientation(bool normalised)
        {
            Quad quad = normalised
                ? new Quad(Vector2.Zero, new Vector2(1, 0), new Vector2(0, 1), Vector2.One)
                : new Quad(new Vector2(0, 1), Vector2.One, Vector2.Zero, new Vector2(1, 0));

            float orientation = Vector2Extensions.GetOrientation(quad.GetVertices());

            Assert.That(orientation, Is.EqualTo(normalised ? 2 : -2).Within(0.001));
        }
    }
}
