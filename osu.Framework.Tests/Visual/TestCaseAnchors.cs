// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using NUnit.Framework;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.MathUtils;
using osu.Framework.Testing;
using OpenTK;

namespace osu.Framework.Tests.Visual
{
    public class TestCaseAnchors : TestCase
    {
        [TestCase(Anchor.TopLeft, 0, 0)]
        [TestCase(Anchor.TopCentre, 0.5f, 0)]
        [TestCase(Anchor.TopRight, 1, 0)]
        [TestCase(Anchor.CentreLeft, 0, 0.5f)]
        [TestCase(Anchor.Centre, 0.5f, 0.5f)]
        [TestCase(Anchor.CentreRight, 1, 0.5f)]
        [TestCase(Anchor.BottomLeft, 0, 1)]
        [TestCase(Anchor.BottomCentre, 0.5f, 1)]
        [TestCase(Anchor.BottomRight, 1, 1)]
        public void TestSimpleAnchors(Anchor anchor, float expectedX, float expectedY)
        {
            Box box = null;
            AddStep($"Construct {anchor}", () =>
            {
                Clear();
                Add(new Container
                {
                    Size = Vector2.One,
                    Child = box = new Box { Anchor = anchor }
                });
            });

            AddAssert("At expected position", () => validatePosition(box, new Vector2(expectedX, expectedY)));
        }

        [TestCase(0, 0)]
        [TestCase(0.25f, 0)]
        [TestCase(0.5f, 0)]
        [TestCase(0.75f, 0)]
        [TestCase(1, 0)]
        [TestCase(0, 1)]
        [TestCase(0, 0.25f)]
        [TestCase(0, 0.5f)]
        [TestCase(0, 0.75f)]
        [TestCase(0, 1)]
        [TestCase(-0.5f, -0.5f)]
        [TestCase(1.5f, -0.5f)]
        [TestCase(-0.5f, 1.5f)]
        [TestCase(1.5f, 1.5f)]
        [TestCase(0.5f, 0.5f)]
        public void TestCustomAnchors(float anchorX, float anchorY)
        {
            var anchorPosition = new Vector2(anchorX, anchorY);

            Box box = null;
            AddStep($"Construct {anchorPosition}", () =>
            {
                Clear();
                Add(new Container
                {
                    Size = Vector2.One,
                    Child = box = new Box { AnchorPosition = anchorPosition }
                });
            });

            AddAssert("At expected position", () => validatePosition(box, anchorPosition));
        }

        private bool validatePosition(Box box, Vector2 expectedPosition) => Precision.AlmostEquals(expectedPosition, box.ToParentSpace(Vector2.Zero));
    }
}
