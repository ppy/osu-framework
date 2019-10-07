// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using System.Collections.Generic;
using System.Drawing;
using JetBrains.Annotations;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Configuration;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Platform;
using osuTK;
using osuTK.Graphics;

namespace osu.Framework.Graphics.Visualisation
{
    /// <summary>
    /// Displays a selectable list of resolution/safe area combinations for testing device layouts on desktop.
    /// Apple device info taken from https://iosref.com/res/
    /// </summary>
    internal class DevicePresetsDisplay : ToolWindow
    {
        [Resolved]
        private GameHost host { get; set; }

        private Bindable<Size> windowedSize;
        private Bindable<WindowMode> windowMode;
        private DevicePresetButton selectedButton;

        public DevicePresetsDisplay()
            : base("Device Presets", "(Ctrl+F4 to toggle)")
        {
            ScrollContent.Children = new Drawable[]
            {
                new FillFlowContainer<DevicePresetGroupSection>
                {
                    Padding = new MarginPadding(5),
                    RelativeSizeAxes = Axes.X,
                    AutoSizeAxes = Axes.Y,
                    Direction = FillDirection.Vertical,
                    Spacing = new Vector2(5),
                    Children = presetGroups.Select(group => new DevicePresetGroupSection(group, this)).ToArray()
                },
            };
        }

        [BackgroundDependencyLoader]
        private void load(FrameworkConfigManager config)
        {
            windowedSize = config.GetBindable<Size>(FrameworkSetting.WindowedSize);
            windowedSize.ValueChanged += _ => selectButton(null);
            windowMode = config.GetBindable<WindowMode>(FrameworkSetting.WindowMode);
            windowMode.ValueChanged += _ => selectButton(null);
        }

        private void selectPreset(DevicePresetButton button, DevicePreset preset)
        {
            var scale = (preset.Scale ?? 1f) / (preset.Downsample ?? 1f);

            if (preset.Short != 0 && preset.Long != 0)
            {
                var width = preset.Orientation == Orientation.Portrait ? preset.Short : preset.Long;
                var height = preset.Orientation == Orientation.Portrait ? preset.Long : preset.Short;
                windowMode.Value = WindowMode.Windowed;
                windowedSize.Value = new Size((int)width, (int)height);
            }

            host.Window.SafeAreaPadding.Value = new MarginPadding
            {
                Left = preset.SafeAreaPadding.Left * scale,
                Top = preset.SafeAreaPadding.Top * scale,
                Right = preset.SafeAreaPadding.Right * scale,
                Bottom = preset.SafeAreaPadding.Bottom * scale
            };

            selectButton(button);
        }

        private void selectButton([CanBeNull] DevicePresetButton button)
        {
            selectedButton?.SetSelected(false);
            selectedButton = button;
            selectedButton?.SetSelected(true);
        }

        internal class DevicePresetGroupSection : CompositeDrawable
        {
            public DevicePresetGroupSection(DevicePresetGroup group, DevicePresetsDisplay display)
            {
                RelativeSizeAxes = Axes.X;
                AutoSizeAxes = Axes.Y;

                InternalChild = new FillFlowContainer
                {
                    RelativeSizeAxes = Axes.X,
                    AutoSizeAxes = Axes.Y,
                    Direction = FillDirection.Vertical,
                    Spacing = new Vector2(5),
                    Children = new Drawable[]
                    {
                        new SpriteText
                        {
                            Text = group.Name,
                            Font = FrameworkFont.Regular.With(weight: "Bold")
                        },
                        new FillFlowContainer<DevicePresetButton>
                        {
                            Padding = new MarginPadding { Left = 5 },
                            RelativeSizeAxes = Axes.X,
                            AutoSizeAxes = Axes.Y,
                            Direction = FillDirection.Vertical,
                            Spacing = new Vector2(2),
                            Children = group.Presets.Select(preset => new DevicePresetButton(display, preset)).ToArray()
                        }
                    }
                };
            }
        }

        internal class DevicePresetButton : Button
        {
            public DevicePresetButton(DevicePresetsDisplay display, DevicePreset preset)
            {
                Text = preset.ToString();
                RelativeSizeAxes = Axes.X;
                Anchor = Anchor.CentreLeft;
                Origin = Anchor.CentreLeft;
                Height = 30;
                Action = () => display.selectPreset(this, preset);

                SetSelected(false);
            }

            protected override SpriteText CreateText() => new SpriteText
            {
                Depth = -1,
                Origin = Anchor.CentreLeft,
                Anchor = Anchor.CentreLeft,
                Padding = new MarginPadding { Left = 5 },
                Font = FrameworkFont.Regular,
                Colour = FrameworkColour.Yellow
            };

            internal void SetSelected(bool selected)
            {
                BackgroundColour = selected ? FrameworkColour.BlueGreen.Lighten(0.75f) : FrameworkColour.BlueGreen;
                SpriteText.Colour = selected ? Color4.White : FrameworkColour.Yellow;
            }
        }

        private readonly IEnumerable<DevicePresetGroup> presetGroups = new[]
        {
            // Non-device
            new DevicePresetGroup
            {
                Name = "Desktop",
                Presets = new[]
                {
                    new DevicePreset { Name = "No safe area" },
                    new DevicePreset { Name = "Test safe area", SafeAreaPadding = new MarginPadding(100) },
                }
            },
            // Android
            new DevicePresetGroup
            {
                Name = "Common Android Resolutions",
                Presets = new[]
                {
                    // 1280x800
                    new DevicePreset { Orientation = Orientation.Portrait, Short = 800, Long = 1280 },
                    new DevicePreset { Orientation = Orientation.Landscape, Short = 800, Long = 1280 },
                    // 1920x1080
                    new DevicePreset { Orientation = Orientation.Portrait, Short = 1080, Long = 1920 },
                    new DevicePreset { Orientation = Orientation.Landscape, Short = 1080, Long = 1920 },
                    // 2160x1080
                    new DevicePreset { Orientation = Orientation.Portrait, Short = 1080, Long = 2160 },
                    new DevicePreset { Orientation = Orientation.Landscape, Short = 1080, Long = 2160 },
                    // 2560x1440
                    new DevicePreset { Orientation = Orientation.Portrait, Short = 1440, Long = 2560 },
                    new DevicePreset { Orientation = Orientation.Landscape, Short = 1440, Long = 2560 },
                    // 2960x1440
                    new DevicePreset { Orientation = Orientation.Portrait, Short = 1440, Long = 2960 },
                    new DevicePreset { Orientation = Orientation.Landscape, Short = 1440, Long = 2960 },
                    // 2048x1536
                    new DevicePreset { Orientation = Orientation.Portrait, Short = 1536, Long = 2048 },
                    new DevicePreset { Orientation = Orientation.Landscape, Short = 1536, Long = 2048 },
                }
            },
            // iPhone
            new DevicePresetGroup
            {
                Name = "iPhone XS Max, 11 Pro Max",
                Presets = new[]
                {
                    new DevicePreset { Orientation = Orientation.Portrait, Short = 414, Long = 896, Scale = 1.0f, SafeAreaPadding = new MarginPadding { Top = 44, Bottom = 34 } },
                    new DevicePreset { Orientation = Orientation.Landscape, Short = 414, Long = 896, Scale = 1.0f, SafeAreaPadding = new MarginPadding { Left = 44, Right = 44, Bottom = 34 } },
                    new DevicePreset { Orientation = Orientation.Portrait, Short = 414, Long = 896, Scale = 3.0f, SafeAreaPadding = new MarginPadding { Top = 44, Bottom = 34 } },
                    new DevicePreset { Orientation = Orientation.Landscape, Short = 414, Long = 896, Scale = 3.0f, SafeAreaPadding = new MarginPadding { Left = 44, Right = 44, Bottom = 34 } },
                }
            },
            new DevicePresetGroup
            {
                Name = "iPhone X, XS, 11 Pro",
                Presets = new[]
                {
                    new DevicePreset { Orientation = Orientation.Portrait, Short = 375, Long = 812, Scale = 1.0f, SafeAreaPadding = new MarginPadding { Top = 44, Bottom = 34 } },
                    new DevicePreset { Orientation = Orientation.Landscape, Short = 375, Long = 812, Scale = 1.0f, SafeAreaPadding = new MarginPadding { Left = 44, Right = 44, Bottom = 34 } },
                    new DevicePreset { Orientation = Orientation.Portrait, Short = 375, Long = 812, Scale = 3.0f, SafeAreaPadding = new MarginPadding { Top = 44, Bottom = 34 } },
                    new DevicePreset { Orientation = Orientation.Landscape, Short = 375, Long = 812, Scale = 3.0f, SafeAreaPadding = new MarginPadding { Left = 44, Right = 44, Bottom = 34 } },
                }
            },
            new DevicePresetGroup
            {
                Name = "iPhone XR, 11",
                Presets = new[]
                {
                    new DevicePreset { Orientation = Orientation.Portrait, Short = 414, Long = 896, Scale = 1.0f, SafeAreaPadding = new MarginPadding { Top = 44, Bottom = 34 } },
                    new DevicePreset { Orientation = Orientation.Landscape, Short = 414, Long = 896, Scale = 1.0f, SafeAreaPadding = new MarginPadding { Left = 44, Right = 44, Bottom = 34 } },
                    new DevicePreset { Orientation = Orientation.Portrait, Short = 414, Long = 896, Scale = 2.0f, SafeAreaPadding = new MarginPadding { Top = 44, Bottom = 34 } },
                    new DevicePreset { Orientation = Orientation.Landscape, Short = 414, Long = 896, Scale = 2.0f, SafeAreaPadding = new MarginPadding { Left = 44, Right = 44, Bottom = 34 } },
                }
            },
            new DevicePresetGroup
            {
                Name = "iPhone 6+, 6s+, 7+, 8+",
                Presets = new[]
                {
                    new DevicePreset { Orientation = Orientation.Portrait, Short = 414, Long = 736, Scale = 1.0f },
                    new DevicePreset { Orientation = Orientation.Landscape, Short = 414, Long = 736, Scale = 1.0f },
                    new DevicePreset { Orientation = Orientation.Portrait, Short = 414, Long = 736, Scale = 3.0f, Downsample = 1.15f },
                    new DevicePreset { Orientation = Orientation.Landscape, Short = 414, Long = 736, Scale = 3.0f, Downsample = 1.15f },
                }
            },
            new DevicePresetGroup
            {
                Name = "iPhone 6, 6s, 7, 8",
                Presets = new[]
                {
                    new DevicePreset { Orientation = Orientation.Portrait, Short = 375, Long = 667, Scale = 1.0f },
                    new DevicePreset { Orientation = Orientation.Landscape, Short = 375, Long = 667, Scale = 1.0f },
                    new DevicePreset { Orientation = Orientation.Portrait, Short = 375, Long = 667, Scale = 2.0f },
                    new DevicePreset { Orientation = Orientation.Landscape, Short = 375, Long = 667, Scale = 2.0f },
                }
            },
            new DevicePresetGroup
            {
                Name = "iPhone 5, 5s, 5c, SE",
                Presets = new[]
                {
                    new DevicePreset { Orientation = Orientation.Portrait, Short = 320, Long = 568, Scale = 1.0f },
                    new DevicePreset { Orientation = Orientation.Landscape, Short = 320, Long = 568, Scale = 1.0f },
                    new DevicePreset { Orientation = Orientation.Portrait, Short = 320, Long = 568, Scale = 2.0f },
                    new DevicePreset { Orientation = Orientation.Landscape, Short = 320, Long = 568, Scale = 2.0f },
                }
            },

            // iPad
            new DevicePresetGroup
            {
                Name = "iPad 7th",
                Presets = new[]
                {
                    new DevicePreset { Orientation = Orientation.Portrait, Short = 810, Long = 1080, Scale = 1.0f },
                    new DevicePreset { Orientation = Orientation.Landscape, Short = 810, Long = 1080, Scale = 1.0f },
                    new DevicePreset { Orientation = Orientation.Portrait, Short = 810, Long = 1080, Scale = 2.0f },
                    new DevicePreset { Orientation = Orientation.Landscape, Short = 810, Long = 1080, Scale = 2.0f },
                }
            },
            new DevicePresetGroup
            {
                Name = "iPad Pro (12.9\") 2nd, 3rd",
                Presets = new[]
                {
                    new DevicePreset { Orientation = Orientation.Portrait, Short = 1024, Long = 1366, Scale = 1.0f, SafeAreaPadding = new MarginPadding { Top = 24, Bottom = 20 } },
                    new DevicePreset { Orientation = Orientation.Landscape, Short = 1024, Long = 1366, Scale = 1.0f, SafeAreaPadding = new MarginPadding { Top = 24, Bottom = 20 } },
                    new DevicePreset { Orientation = Orientation.Portrait, Short = 1024, Long = 1366, Scale = 2.0f, SafeAreaPadding = new MarginPadding { Top = 24, Bottom = 20 } },
                    new DevicePreset { Orientation = Orientation.Landscape, Short = 1024, Long = 1366, Scale = 2.0f, SafeAreaPadding = new MarginPadding { Top = 24, Bottom = 20 } },
                }
            },
            new DevicePresetGroup
            {
                Name = "iPad Pro (12.9\") 1st",
                Presets = new[]
                {
                    new DevicePreset { Orientation = Orientation.Portrait, Short = 1024, Long = 1366, Scale = 1.0f },
                    new DevicePreset { Orientation = Orientation.Landscape, Short = 1024, Long = 1366, Scale = 1.0f },
                    new DevicePreset { Orientation = Orientation.Portrait, Short = 1024, Long = 1366, Scale = 2.0f },
                    new DevicePreset { Orientation = Orientation.Landscape, Short = 1024, Long = 1366, Scale = 2.0f },
                }
            },
            new DevicePresetGroup
            {
                Name = "iPad Pro (11\")",
                Presets = new[]
                {
                    new DevicePreset { Orientation = Orientation.Portrait, Short = 834, Long = 1194, Scale = 1.0f, SafeAreaPadding = new MarginPadding { Top = 24, Bottom = 20 } },
                    new DevicePreset { Orientation = Orientation.Landscape, Short = 834, Long = 1194, Scale = 1.0f, SafeAreaPadding = new MarginPadding { Top = 24, Bottom = 20 } },
                    new DevicePreset { Orientation = Orientation.Portrait, Short = 834, Long = 1194, Scale = 2.0f, SafeAreaPadding = new MarginPadding { Top = 24, Bottom = 20 } },
                    new DevicePreset { Orientation = Orientation.Landscape, Short = 834, Long = 1194, Scale = 2.0f, SafeAreaPadding = new MarginPadding { Top = 24, Bottom = 20 } },
                }
            },
            new DevicePresetGroup
            {
                Name = "iPad Pro (10.5\"), iPad Air (10.5\")",
                Presets = new[]
                {
                    new DevicePreset { Orientation = Orientation.Portrait, Short = 834, Long = 1112, Scale = 1.0f },
                    new DevicePreset { Orientation = Orientation.Landscape, Short = 834, Long = 1112, Scale = 1.0f },
                    new DevicePreset { Orientation = Orientation.Portrait, Short = 834, Long = 1112, Scale = 2.0f },
                    new DevicePreset { Orientation = Orientation.Landscape, Short = 834, Long = 1112, Scale = 2.0f },
                }
            },
            new DevicePresetGroup
            {
                Name = "iPad 3rd-6th, iPad Mini 2nd-5th, iPad Air 1st-2nd, iPad Pro 9.7\"",
                Presets = new[]
                {
                    new DevicePreset { Orientation = Orientation.Portrait, Short = 768, Long = 1024, Scale = 1.0f },
                    new DevicePreset { Orientation = Orientation.Landscape, Short = 768, Long = 1024, Scale = 1.0f },
                    new DevicePreset { Orientation = Orientation.Portrait, Short = 768, Long = 1024, Scale = 2.0f },
                    new DevicePreset { Orientation = Orientation.Landscape, Short = 768, Long = 1024, Scale = 2.0f },
                }
            },
            new DevicePresetGroup
            {
                Name = "iPad 1st-2nd, iPad Mini 1st",
                Presets = new[]
                {
                    new DevicePreset { Orientation = Orientation.Portrait, Short = 768, Long = 1024, Scale = 1.0f },
                    new DevicePreset { Orientation = Orientation.Landscape, Short = 768, Long = 1024, Scale = 1.0f },
                }
            },
        };

        internal struct DevicePresetGroup
        {
            internal string Name;
            internal DevicePreset[] Presets;
        }

        internal struct DevicePreset
        {
            internal string Name;
            internal float Short;
            internal float Long;
            internal float? Scale;
            internal float? Downsample;
            internal Orientation Orientation;
            internal MarginPadding SafeAreaPadding;

            public override string ToString()
            {
                var str = Name ?? "";

                if (Short != 0 && Long != 0)
                {
                    if (str != "") str += " ";

                    str += Orientation == Orientation.Portrait ? "Portrait" : "Landscape";
                    str += !Scale.HasValue ? "" : Scale.Value == 1f ? ", Logical" : ", Physical";
                    str += Orientation == Orientation.Portrait ? $" - ({Short}, {Long})" : $" - ({Long}, {Short})";
                    str += !Scale.HasValue ? "" : $" @ {Scale.Value:F1}x";
                    str += Downsample.HasValue ? " (Downsampled)" : "";
                }

                if (SafeAreaPadding.Total != Vector2.Zero)
                    str += $" - {SafeAreaPadding}";

                return str;
            }
        }

        internal enum Orientation
        {
            Portrait,
            Landscape,
        }
    }
}
