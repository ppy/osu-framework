// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using NUnit.Framework;
using osu.Framework.Extensions.IEnumerableExtensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Testing.Input;
using osuTK;
using osuTK.Graphics;

namespace osu.Framework.Testing
{
    /// <summary>
    /// An abstract test case which is tested with manual input management.
    /// </summary>
    public abstract class ManualInputManagerTestScene : TestScene
    {
        protected override Container<Drawable> Content => InputManager;

        /// <summary>
        /// The position which is used to initialize the mouse position before at setup.
        /// If the value is null, the mouse position is not moved.
        /// </summary>
        protected virtual Vector2? InitialMousePosition => null;

        /// <summary>
        /// The <see cref="ManualInputManager"/>.
        /// </summary>
        protected ManualInputManager InputManager { get; }

        private readonly BasicButton buttonTest;
        private readonly BasicButton buttonLocal;

        [SetUp]
        public virtual void SetUp() => ResetInput();

        protected ManualInputManagerTestScene()
        {
            base.Content.AddRange(new Drawable[]
            {
                InputManager = new ManualInputManager
                {
                    UseParentInput = true,
                },
                new Container
                {
                    Depth = float.MinValue,
                    AutoSizeAxes = Axes.Both,
                    Anchor = Anchor.TopRight,
                    Origin = Anchor.TopRight,
                    Margin = new MarginPadding(5),
                    CornerRadius = 5,
                    Masking = true,
                    Children = new Drawable[]
                    {
                        new Box
                        {
                            Colour = Color4.Black,
                            RelativeSizeAxes = Axes.Both,
                            Alpha = 0.5f,
                        },
                        new FillFlowContainer
                        {
                            AutoSizeAxes = Axes.Both,
                            Direction = FillDirection.Vertical,
                            Margin = new MarginPadding(5),
                            Spacing = new Vector2(5),
                            Children = new Drawable[]
                            {
                                new SpriteText
                                {
                                    Anchor = Anchor.TopCentre,
                                    Origin = Anchor.TopCentre,
                                    Text = "Input Priority"
                                },
                                new FillFlowContainer
                                {
                                    AutoSizeAxes = Axes.Both,
                                    Anchor = Anchor.TopCentre,
                                    Origin = Anchor.TopCentre,
                                    Margin = new MarginPadding(5),
                                    Spacing = new Vector2(5),
                                    Direction = FillDirection.Horizontal,

                                    Children = new Drawable[]
                                    {
                                        buttonLocal = new BasicButton
                                        {
                                            Text = "local",
                                            Size = new Vector2(50, 30),
                                            Action = returnUserInput
                                        },
                                        buttonTest = new BasicButton
                                        {
                                            Text = "test",
                                            Size = new Vector2(50, 30),
                                            Action = returnTestInput
                                        },
                                    }
                                },
                            }
                        },
                    }
                },
            });
        }

        protected override void Update()
        {
            base.Update();

            buttonTest.Enabled.Value = InputManager.UseParentInput;
            buttonLocal.Enabled.Value = !InputManager.UseParentInput;
        }

        /// <summary>
        /// Releases all pressed keys and buttons and initialize mouse position.
        /// </summary>
        protected void ResetInput()
        {
            InputManager.UseParentInput = true;
            var currentState = InputManager.CurrentState;

            var mouse = currentState.Mouse;
            var position = InitialMousePosition;
            if (position != null) InputManager.MoveMouseTo(position.Value);
            mouse.Buttons.ForEach(InputManager.ReleaseButton);

            var keyboard = currentState.Keyboard;
            keyboard.Keys.ForEach(InputManager.ReleaseKey);

            var joystick = currentState.Joystick;
            joystick.Buttons.ForEach(InputManager.ReleaseJoystickButton);
        }

        private void returnUserInput() =>
            InputManager.UseParentInput = true;

        private void returnTestInput() =>
            InputManager.UseParentInput = false;
    }
}
