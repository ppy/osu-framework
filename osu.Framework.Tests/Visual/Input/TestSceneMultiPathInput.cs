// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Lines;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Input.Events;
using osuTK;
using osuTK.Graphics;

namespace osu.Framework.Tests.Visual.Input
{
    public class TestSceneMultiPathInput : FrameworkTestScene
    {
        private const float path_width = 50;
        private const float path_radius = path_width / 2;

        private MultiPath path;
        private TestPoint testPoint;
        private SpriteText text;

        [Test]
        public void Setup() => Schedule(() =>
        {
            Children = new Drawable[]
            {
                path = new HoverablePath(),
                testPoint = new TestPoint(),
                text = new SpriteText { Anchor = Anchor.TopCentre, Origin = Anchor.TopCentre }
            };
        });

        [Test]
        public void TestHorizontalPath()
        {
            addPath("Single horizontal path", new[] { new Vector2(100), new Vector2(300, 100) });
            // Left out
            test(new Vector2(40, 100), false);
            // Left in
            test(new Vector2(80, 100), true);
            // Cap out
            test(new Vector2(60), false);
            // Cap in
            test(new Vector2(70), true);
            //Right out
            test(new Vector2(360, 100), false);
            // Centre
            test(new Vector2(200, 100), true);
            // Top out
            test(new Vector2(190, 40), false);
            // Top in
            test(new Vector2(190, 60), true);
        }

        [Test]
        public void TestDiagonalPath()
        {
            addPath("Single diagonal path", new[] { new Vector2(300), new Vector2(100) });
            // Top-left out
            test(new Vector2(50), false);
            // Top-left in
            test(new Vector2(80), true);
            // Left out
            test(new Vector2(145, 235), false);
            // Left in
            test(new Vector2(170, 235), true);
            // Cap out
            test(new Vector2(355, 300), false);
            // Cap in
            test(new Vector2(340, 300), true);
        }

        [Test]
        public void TestTicTacToe()
        {
            addPath("4 disjoint overlapping linear paths",
                new[] { new Vector2(100 - 50, 300 - 50), new Vector2(700 - 50, 300 - 50) },
                new[] { new Vector2(100 - 50, 500 - 50), new Vector2(700 - 50, 500 - 50) },
                new[] { new Vector2(300 - 50, 100 - 50), new Vector2(300 - 50, 700 - 50) },
                new[] { new Vector2(500 - 50, 100 - 50), new Vector2(500 - 50, 700 - 50) });

            // Outside first vertex cap
            test(new Vector2(11, 214), false);
            // Inside first vertex cap
            test(new Vector2(19, 223), true);

            // Outside last vertex cap
            test(new Vector2(413, 692), false);
            // Inside last vertex cap
            test(new Vector2(428, 689), true);

            // Outside middle vertex cap
            test(new Vector2(14, 412), false);
            // Inside middle vertex cap
            test(new Vector2(26, 420), true);

            // On line overlap
            test(new Vector2(250, 250), true);
            test(new Vector2(450, 250), true);
            test(new Vector2(250, 450), true);

            // On line segments
            test(new Vector2(250, 150), true);
            test(new Vector2(350, 250), true);

            // in middle hole
            test(new Vector2(305, 305), false);
            test(new Vector2(350, 350), false);
            test(new Vector2(395, 395), false);
        }

        protected override bool OnMouseMove(MouseMoveEvent e)
        {
            text.Text = path.ToLocalSpace(e.ScreenSpaceMousePosition).ToString();
            return base.OnMouseMove(e);
        }

        private void addPath(string name, params IEnumerable<Vector2>[] parts) => AddStep(name, () =>
        {
            path.PathRadius = path_width;
            path.ClearPaths();
            foreach (var part in parts)
                path.AddPath(part);
        });

        private void test(Vector2 position, bool shouldReceivePositionalInput)
        {
            AddAssert($"Test @ {position} = {shouldReceivePositionalInput}", () =>
            {
                testPoint.Position = position;
                return path.ReceivePositionalInputAt(path.ToScreenSpace(position)) == shouldReceivePositionalInput;
            });
        }

        private class TestPoint : CircularContainer
        {
            public TestPoint()
            {
                Origin = Anchor.Centre;

                Size = new Vector2(5);
                Colour = Color4.Red;
                Masking = true;

                InternalChild = new Box { RelativeSizeAxes = Axes.Both };
            }
        }

        private class HoverablePath : MultiPath
        {
            protected override bool OnHover(HoverEvent e)
            {
                Colour = Color4.Green;
                return true;
            }

            protected override void OnHoverLost(HoverLostEvent e)
            {
                Colour = Color4.White;
            }
        }
    }
}
