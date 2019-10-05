// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Cursor;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Testing;
using osuTK;
using osuTK.Graphics;

namespace osu.Framework.Tests.Visual.Containers
{
    public class TestSceneCursorContainer : ManualInputManagerTestScene
    {
        public Container Container { get; private set; }
        public TestCursorContainer CursorContainer { get; private set; }

        public void CreateContent()
        {
            Child = Container = new Container
            {
                Masking = true,
                RelativeSizeAxes = Axes.Both,
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                Size = new Vector2(0.5f),
                Children = new Drawable[]
                {
                    new Box
                    {
                        Colour = Color4.Yellow,
                        RelativeSizeAxes = Axes.Both,
                    },
                    CursorContainer = new TestCursorContainer
                    {
                        Name = "test",
                        RelativeSizeAxes = Axes.Both
                    }
                }
            };
        }

        public class TestCursorContainer : CursorContainer
        {
            protected override Drawable CreateCursor() => new Circle
            {
                Size = new Vector2(50),
                Colour = Color4.Red,
                Origin = Anchor.Centre,
            };
        }
    }
}
