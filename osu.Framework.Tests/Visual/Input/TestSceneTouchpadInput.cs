// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Configuration;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Input;
using osu.Framework.Input.Events;
using osu.Framework.Input.Handlers.Touchpad;
using osu.Framework.Platform;
using osuTK;
using osuTK.Graphics;
using osuTK.Input;

namespace osu.Framework.Tests.Visual.Input
{
    public class TestSceneTouchpad : FrameworkTestScene
    {
        private readonly BindableFloat width;
        private readonly BindableFloat height;
        private readonly BindableFloat xOffset;
        private readonly BindableFloat yOffset;

        [Resolved]
        private GameHost host { get; set; }

        [Resolved]
        private FrameworkConfigManager config { get; set; }

        protected override void Dispose(bool isDisposing)
        {
            base.Dispose(isDisposing);

            if (!isDisposing) return;

            // Trying to ensure that this InputHandler isn't still enabled when browsing other tests.
            var touchpadHandler = host.AvailableInputHandlers.OfType<TouchpadHandler>().FirstOrDefault();

            if (touchpadHandler != null)
            {
                touchpadHandler.Enabled.Value = false;
            }
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            var touchpadHandler = host.AvailableInputHandlers.OfType<TouchpadHandler>().FirstOrDefault();

            if (touchpadHandler != null)
            {
                width.BindValueChanged(e =>
                {
                    Vector2 areaSize = touchpadHandler.AreaSize.Value;
                    areaSize.X = e.NewValue;
                    touchpadHandler.AreaSize.Value = areaSize;
                });

                height.BindValueChanged(e =>
                {
                    Vector2 areaSize = touchpadHandler.AreaSize.Value;
                    areaSize.Y = e.NewValue;
                    touchpadHandler.AreaSize.Value = areaSize;
                });

                xOffset.BindValueChanged(e =>
                {
                    Vector2 areaOffset = touchpadHandler.AreaOffset.Value;
                    areaOffset.X = e.NewValue;
                    touchpadHandler.AreaOffset.Value = areaOffset;
                });

                yOffset.BindValueChanged(e =>
                {
                    Vector2 areaOffset = touchpadHandler.AreaOffset.Value;
                    areaOffset.Y = e.NewValue;
                    touchpadHandler.AreaOffset.Value = areaOffset;
                });
            }

            width.SetDefault();
            height.SetDefault();
            xOffset.SetDefault();
            yOffset.SetDefault();

            Child = new FillFlowContainer
            {
                RelativeSizeAxes = Axes.Both,
                Direction = FillDirection.Vertical,
                Padding = new MarginPadding(5),
                Spacing = new Vector2(5, 5),
                Children = new Drawable[]
                {
                    new SpriteText
                    {
                        Text = "Press Q to switch between Absolute and Relative modes."
                    },
                    new SpriteText
                    {
                        Text = "Press R to reset the settings below."
                    },
                    new TouchpadSettingText("Width", width),
                    new TouchpadSettingSliderBar(width),

                    new TouchpadSettingText("Height", height),
                    new TouchpadSettingSliderBar(height),

                    new TouchpadSettingText("X-offset", xOffset),
                    new TouchpadSettingSliderBar(xOffset),

                    new TouchpadSettingText("Y-offset", yOffset),
                    new TouchpadSettingSliderBar(yOffset),
                }
            };

            AddToggleStep("toggle confine mouse", enabled =>
            {
                config.SetValue(FrameworkSetting.ConfineMouseMode, enabled ? ConfineMouseMode.Always : ConfineMouseMode.Fullscreen);
            });
        }

        public TestSceneTouchpad()
        {
            width = new BindableFloat(0.9f)
            {
                MinValue = 0.2f,
                MaxValue = 1f,
                Precision = 0.01f
            };
            height = new BindableFloat(0.9f)
            {
                MinValue = 0.2f,
                MaxValue = 1f,
                Precision = 0.01f
            };
            xOffset = new BindableFloat(0.5f)
            {
                MinValue = 0f,
                MaxValue = 1f,
                Precision = 0.01f
            };
            yOffset = new BindableFloat(0.5f)
            {
                MinValue = 0f,
                MaxValue = 1f,
                Precision = 0.01f
            };
        }

        protected override bool Handle(UIEvent e)
        {
            switch (e)
            {
                case KeyDownEvent keyDown:
                    switch (keyDown.Key)
                    {
                        case Key.Q:
                            var touchpadHandler = host.AvailableInputHandlers.OfType<TouchpadHandler>().FirstOrDefault();
                            if (touchpadHandler != null)
                                touchpadHandler.Enabled.Value = !touchpadHandler.Enabled.Value;
                            break;

                        case Key.R:
                            width.SetDefault();
                            height.SetDefault();
                            xOffset.SetDefault();
                            yOffset.SetDefault();
                            break;
                    }

                    break;
            }

            return true;
        }
    }

    internal class TouchpadSettingText : SpriteText
    {
        public TouchpadSettingText(string name, BindableFloat bindable)
        {
            Text = $"{name}: {bindable.Value}";
            bindable.BindValueChanged(e =>
            {
                Current.Value = $"{name}: {bindable.Value}";
            });
        }
    }

    internal class TouchpadSettingSliderBar : BasicSliderBar<float>
    {
        public TouchpadSettingSliderBar(BindableFloat bindable)
        {
            Size = new Vector2(200, 10);
            RangePadding = 20;
            BackgroundColour = Color4.White;
            SelectionColour = Color4.Pink;
            KeyboardStep = 0.01f;
            Current = bindable;
        }
    }
}
