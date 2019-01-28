﻿// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Allocation;
using osu.Framework.Configuration;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Input;
using osu.Framework.Input.Events;
using osu.Framework.Input.Handlers.Mouse;
using osu.Framework.Testing;
using osuTK;
using osuTK.Graphics;

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

            protected override bool OnMouseDown(MouseDownEvent e)
            {
                ++MouseDownCount;
                onMouseDownStatus.Text = $"OnMouseDown {MouseDownCount}: Position={e.MousePosition}";
                return true;
            }

            public int MouseUpCount;

            protected override bool OnMouseUp(MouseUpEvent e)
            {
                ++MouseUpCount;
                onMouseUpStatus.Text = $"OnMouseUp {MouseUpCount}: Position={e.MousePosition}, MouseDownPosition={e.MouseDownPosition}";
                return base.OnMouseUp(e);
            }

            public int MouseMoveCount;

            protected override bool OnMouseMove(MouseMoveEvent e)
            {
                ++MouseMoveCount;
                onMouseMoveStatus.Text = $"OnMouseMove {MouseMoveCount}: Position={e.MousePosition}, Delta={e.Delta}";
                return base.OnMouseMove(e);
            }

            public int ScrollCount;

            protected override bool OnScroll(ScrollEvent e)
            {
                ++ScrollCount;
                onScrollStatus.Text = $"OnScroll {ScrollCount}: ScrollDelta={e.ScrollDelta}, IsPrecise={e.IsPrecise}";
                return base.OnScroll(e);
            }

            public int HoverCount;

            protected override bool OnHover(HoverEvent e)
            {
                ++HoverCount;
                onHoverStatus.Text = $"OnHover {HoverCount}: Position={e.MousePosition}";
                return base.OnHover(e);
            }

            protected override bool OnClick(ClickEvent e)
            {
                this.MoveToOffset(new Vector2(100, 0)).Then().MoveToOffset(new Vector2(-100, 0), 1000, Easing.In);
                return true;
            }
        }

        [Resolved]
        private FrameworkConfigManager config { get; set; }

        [BackgroundDependencyLoader]
        private void load()
        {
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
            config.Set(FrameworkSetting.IgnoredInputHandlers, x ? nameof(OsuTKMouseHandler) : nameof(OsuTKRawMouseHandler));
        }

        private void setConfineMouseModeConfig(bool x)
        {
            config.Set(FrameworkSetting.ConfineMouseMode, x ? ConfineMouseMode.Always : ConfineMouseMode.Fullscreen);
        }
    }
}
