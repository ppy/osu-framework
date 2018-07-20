// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using osu.Framework.Allocation;
using osu.Framework.Configuration;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Input;
using osu.Framework.Input.EventArgs;
using osu.Framework.Input.Handlers.Mouse;
using osu.Framework.Input.States;
using osu.Framework.Testing;
using OpenTK;
using OpenTK.Graphics;

namespace osu.Framework.Tests.Visual
{
    public class TestCaseInputManager : TestCase
    {
        public TestCaseInputManager()
        {
            Add(new Container
            {
                RelativeSizeAxes = Axes.Both,
                Size = new Vector2(1),
                Children = new Drawable[]
                {
                    new Container
                    {
                        RelativeSizeAxes = Axes.Both,
                        RelativePositionAxes = Axes.Both,
                        Position = new Vector2(0, -0.1f),
                        Size = new Vector2(0.7f, 0.8f),
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                        Child = new ContainingInputManagerStatusText(),
                    },
                    new Container
                    {
                        RelativeSizeAxes = Axes.Both,
                        Anchor = Anchor.Centre,
                        Size = new Vector2(0.7f, 0.1f),
                    },
                    new PassThroughInputManager
                    {
                        RelativeSizeAxes = Axes.Both,
                        RelativePositionAxes = Axes.Both,
                        Position = new Vector2(0, 0.1f),
                        Size = new Vector2(0.7f, 0.5f),
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                        Child = new ContainingInputManagerStatusText(),
                    }
                }
            });
        }

        public class SmallText : SpriteText
        {
            public SmallText()
            {
                TextSize = 20;
            }
        }

        public class ContainingInputManagerStatusText : Container
        {
            private readonly SpriteText inputManagerStatus,
                                        mouseStatus,
                                        keyboardStatus,
                                        joystickStatus,
                                        onMouseDownStatus,
                                        onMouseUpStatus,
                                        onMouseMoveStatus,
                                        onScrollStatus,
                                        onHoverStatus;

            public ContainingInputManagerStatusText()
            {
                RelativeSizeAxes = Axes.Both;
                Children = new Drawable[]
                {
                    new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = new Color4(1, 1, 1, 0.2f),
                    },
                    new FillFlowContainer
                    {
                        RelativeSizeAxes = Axes.Both,
                        Direction = FillDirection.Vertical,
                        Children = new Drawable[]
                        {
                            inputManagerStatus = new SmallText(),
                            mouseStatus = new SmallText(),
                            keyboardStatus = new SmallText(),
                            joystickStatus = new SmallText(),
                            onMouseDownStatus = new SmallText { Text = "OnMouseDown 0" },
                            onMouseUpStatus = new SmallText { Text = "OnMouseUp 0" },
                            onMouseMoveStatus = new SmallText { Text = "OnMouseMove 0" },
                            onScrollStatus = new SmallText { Text = "OnScroll 0" },
                            onHoverStatus = new SmallText { Text = "OnHover 0" },
                        }
                    }
                };
            }

            protected override void Update()
            {
                var inputManager = GetContainingInputManager();
                var currentState = inputManager.CurrentState;
                var mouse = currentState.Mouse;
                inputManagerStatus.Text = $"{inputManager}";
                mouseStatus.Text = $"Mouse: {mouse.Position} {mouse.Scroll} " + String.Join(" ", mouse.Buttons);
                keyboardStatus.Text = "Keyboard: " + String.Join(" ", currentState.Keyboard.Keys);
                joystickStatus.Text = "Joystick: " + String.Join(" ", currentState.Joystick.Buttons);
                base.Update();
            }

            public int MouseDownCount;

            protected override bool OnMouseDown(InputState state, MouseDownEventArgs args)
            {
                ++MouseDownCount;
                onMouseDownStatus.Text = $"OnMouseDown {MouseDownCount}: Position={state.Mouse.Position}";
                return true;
            }

            public int MouseUpCount;

            protected override bool OnMouseUp(InputState state, MouseUpEventArgs args)
            {
                ++MouseUpCount;
                onMouseUpStatus.Text = $"OnMouseUp {MouseUpCount}: Position={state.Mouse.Position}, PositionMouseDown={state.Mouse.PositionMouseDown}";
                return base.OnMouseUp(state, args);
            }

            public int MouseMoveCount;

            protected override bool OnMouseMove(InputState state)
            {
                ++MouseMoveCount;
                onMouseMoveStatus.Text = $"OnMouseMove {MouseMoveCount}: Position={state.Mouse.Position}, Delta={state.Mouse.Delta}";
                return base.OnMouseMove(state);
            }

            public int ScrollCount;

            protected override bool OnScroll(InputState state)
            {
                ++ScrollCount;
                onScrollStatus.Text = $"OnScroll {ScrollCount}: Scroll={state.Mouse.Scroll}, ScrollDelta={state.Mouse.ScrollDelta}, HasPreciseScroll={state.Mouse.HasPreciseScroll}";
                return base.OnScroll(state);
            }

            public int HoverCount;

            protected override bool OnHover(InputState state)
            {
                ++HoverCount;
                onHoverStatus.Text = $"OnHover {HoverCount}: Position={state.Mouse.Position}";
                return base.OnHover(state);
            }

            protected override bool OnClick(InputState state)
            {
                this.MoveToOffset(new Vector2(100, 0)).Then().MoveToOffset(new Vector2(-100, 0), 1000, Easing.In);
                return true;
            }
        }

        private FrameworkConfigManager config;

        [BackgroundDependencyLoader]
        private void load(FrameworkConfigManager config)
        {
            this.config = config;

            AddSliderStep("Cursor sensivity", 0.5, 5, 1, setCursorSensivityConfig);
            setCursorSensivityConfig(1);
            AddToggleStep("Toggle raw input", setRawInputConfig);
            setRawInputConfig(false);
            AddToggleStep("Toggle ConfineMouseMode", setConfineMouseModeConfig);
            setConfineMouseModeConfig(false);
        }

        private void setCursorSensivityConfig(double x)
        {
            config.Set(FrameworkSetting.CursorSensitivity, x);
        }

        private void setRawInputConfig(bool x)
        {
            config.Set(FrameworkSetting.IgnoredInputHandlers, x ? nameof(OpenTKMouseHandler) : nameof(OpenTKRawMouseHandler));
        }

        private void setConfineMouseModeConfig(bool x)
        {
            config.Set(FrameworkSetting.ConfineMouseMode, x ? ConfineMouseMode.Always : ConfineMouseMode.Fullscreen);
        }
    }
}
