// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

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
    public class TestSceneHandleInput : ManualInputManagerTestScene
    {
        public TestSceneHandleInput()
        {
            TestContainerNoHandling notHandleInput;
            TestContainer handlePositionalInput, handleNonPositionalInput;
            Add(notHandleInput = new TestContainerNoHandling { Colour = Color4.Red });
            Add(handlePositionalInput = new TestContainerHandlePositionalInput { X = 300, Colour = Color4.Blue });
            Add(handleNonPositionalInput = new TestContainerHandleNonPositionalInput { X = 600, Colour = Color4.Green });
            Add(new TestSceneMouseStates.StateTracker.BoundedCursorContainer(0));

            AddStep($"enable {notHandleInput}", () => { InputManager.MoveMouseTo(notHandleInput); });
            AddAssert($"check {nameof(notHandleInput)}", () => !notHandleInput.IsHovered && !notHandleInput.HasFocus);

            AddStep($"enable {nameof(handlePositionalInput)}", () =>
            {
                handlePositionalInput.Enabled = true;
                InputManager.MoveMouseTo(handlePositionalInput);
            });
            AddAssert($"check {nameof(handlePositionalInput)}", () => handlePositionalInput.IsHovered && handlePositionalInput.HasFocus);

            AddStep($"enable {nameof(handleNonPositionalInput)}", () =>
            {
                handleNonPositionalInput.Enabled = true;
                InputManager.MoveMouseTo(handleNonPositionalInput);
                InputManager.TriggerFocusContention(null);
            });
            AddAssert($"check {nameof(handleNonPositionalInput)}", () => !handleNonPositionalInput.IsHovered && handleNonPositionalInput.HasFocus);

            AddStep("move mouse", () => InputManager.MoveMouseTo(handlePositionalInput));
            AddStep("disable all", () =>
            {
                handlePositionalInput.Enabled = false;
                handleNonPositionalInput.Enabled = false;
            });
            AddAssert($"check {nameof(handlePositionalInput)}", () => !handlePositionalInput.IsHovered);
            // focus is not released when AcceptsFocus become false while focused
            //AddAssert($"check {nameof(handleNonPositionalInput)}", () => !handleNonPositionalInput.HasFocus);
        }

        private class TestContainerNoHandling : Container
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

        private class TestContainer : TestContainerNoHandling
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

        private class TestContainerHandlePositionalInput : TestContainer
        {
            public override bool HandlePositionalInput => Enabled;
        }

        private class TestContainerHandleNonPositionalInput : TestContainer
        {
            public override bool HandleNonPositionalInput => Enabled;
        }
    }
}
