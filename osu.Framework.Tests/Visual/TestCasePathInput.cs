// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System.Linq;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Lines;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Input;
using osu.Framework.Testing;
using OpenTK;
using OpenTK.Graphics;

namespace osu.Framework.Tests.Visual
{
    public class TestCasePathInput : TestCase
    {
        private const float path_width = 50;
        private const float path_radius = path_width / 2;

        private readonly Path path;
        private readonly TestPoint testPoint;
        private readonly SpriteText text;

        public TestCasePathInput()
        {
            Children = new Drawable[]
            {
                path = new HoverablePath(),
                testPoint = new TestPoint(),
                text = new SpriteText { Anchor = Anchor.TopCentre, Origin = Anchor.TopCentre }
            };

            testHorizontalPath();
            testDiagonalPath();
            testVShaped();
            testOverlapping();
        }

        private void testHorizontalPath()
        {
            addPath("Horizontal path", new Vector2(100), new Vector2(300, 100));
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

        private void testDiagonalPath()
        {
            addPath("Diagonal path", new Vector2(300), new Vector2(100));
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

        private void testVShaped()
        {
            addPath("V-shaped", new Vector2(100), new Vector2(300), new Vector2(500, 100));
            // Intersection out
            test(new Vector2(300, 225), false);
            // Intersection in
            test(new Vector2(300, 240), true);
            // Bottom cap out
            test(new Vector2(300, 355), false);
            // Bottom cap in
            test(new Vector2(300, 340), true);
        }

        private void testOverlapping()
        {
            addPath("Overlapping", new Vector2(100), new Vector2(600), new Vector2(800, 300), new Vector2(100, 400));
            // Left intersection out
            test(new Vector2(250, 325), false);
            // Left intersection in
            test(new Vector2(260, 325), true);
            // Top intersection out
            test(new Vector2(380, 300), false);
            // Top intersection in
            test(new Vector2(380, 320), true);
            // Triangle left intersection out
            test(new Vector2(475, 400), false);
            // Triangle left intersection in
            test(new Vector2(460, 400), true);
            // Triangle right intersection out
            test(new Vector2(690, 370), false);
            // Triangle right intersection in
            test(new Vector2(700, 370), true);
            // Triangle bottom intersection out
            test(new Vector2(590, 515), false);
            // Triangle bottom intersection in
            test(new Vector2(590, 525), true);
            // Centre intersection in
            test(new Vector2(370, 360), true);
        }

        protected override bool OnMouseMove(InputState state)
        {
            text.Text = path.ToLocalSpace(state.Mouse.NativeState.Position).ToString();
            return base.OnMouseMove(state);
        }

        private void addPath(string name, params Vector2[] vertices) => AddStep(name, () =>
        {
            path.PathWidth = path_width;
            path.Positions = vertices.ToList();
        });

        private void test(Vector2 position, bool shouldReceiveMouseInput)
        {
            AddAssert($"Test @ {position} = {shouldReceiveMouseInput}", () =>
            {
                testPoint.Position = position;
                return path.ReceiveMouseInputAt(path.ToScreenSpace(position)) == shouldReceiveMouseInput;
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

        private class HoverablePath : Path
        {
            protected override bool OnHover(InputState state)
            {
                Colour = Color4.Green;
                return true;
            }

            protected override void OnHoverLost(InputState state)
            {
                Colour = Color4.White;
            }
        }
    }
}
