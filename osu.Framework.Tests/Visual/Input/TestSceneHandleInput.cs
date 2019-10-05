// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Testing;
using osuTK;
using osuTK.Graphics;

namespace osu.Framework.Tests.Visual.Input
{
    public class TestSceneHandleInput : TestScene
    {
        public TestContainerNoHandling TestNotHandleInput { get; set; }
        public TestContainer TestHandlePositionalInput { get; set; }
        public TestContainer TestHandleNonPositionalInput { get; set; }

        public TestSceneHandleInput()
        {
            Add(TestNotHandleInput = new TestContainerNoHandling { Colour = Color4.Red });
            Add(TestHandlePositionalInput = new TestContainerHandlePositionalInput { X = 300, Colour = Color4.Blue });
            Add(TestHandleNonPositionalInput = new TestContainerHandleNonPositionalInput { X = 600, Colour = Color4.Green });
            Add(new TestSceneMouseStates.StateTracker.BoundedCursorContainer(0));
        }

        public class TestContainerNoHandling : Container
        {
            protected readonly Box Box;
            protected readonly Box DisabledOverlay;
            private readonly SpriteText text1, text2;

            public new Color4 Colour
            {
                get => Box.Colour;
                set => Box.Colour = value;
            }

            public TestContainerNoHandling()
            {
                Size = new Vector2(250);
                Add(Box = new Box { RelativeSizeAxes = Axes.Both });
                Add(new SpriteText { Text = GetType().Name });
                Add(text1 = new SpriteText { Y = 20 });
                Add(text2 = new SpriteText { Y = 40 });
                Add(DisabledOverlay = new Box { RelativeSizeAxes = Axes.Both, Colour = Color4.Gray.Opacity(.5f) });
            }

            protected override void Update()
            {
                text1.Text = $"IsHovered = {IsHovered}";
                text2.Text = $"HasFocus = {HasFocus}";
                base.Update();
            }
        }

        public class TestContainer : TestContainerNoHandling
        {
            public override bool AcceptsFocus => Enabled;
            public override bool RequestsFocus => Enabled;

            private bool enabled;

            public bool Enabled
            {
                protected get => enabled;
                set
                {
                    enabled = value;
                    DisabledOverlay.Alpha = enabled ? 0 : 1;
                }
            }
        }

        public class TestContainerHandlePositionalInput : TestContainer
        {
            public override bool HandlePositionalInput => Enabled;
        }

        public class TestContainerHandleNonPositionalInput : TestContainer
        {
            public override bool HandleNonPositionalInput => Enabled;
        }
    }
}
