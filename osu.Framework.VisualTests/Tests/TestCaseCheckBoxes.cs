// Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using OpenTK;
using OpenTK.Graphics;
using osu.Framework.GameModes.Testing;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Drawables;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.UserInterface;

namespace osu.Framework.VisualTests.Tests
{
    class TestCaseCheckBox : TestCase
    {
        public override string Name => @"Checkboxes";

        public override string Description => @"CheckBoxes with clickable labels";

        public CheckBox BasicCheckBox;
        public CheckBox ResizingWidthCheckBox;
        public CheckBox ResizingHeightCheckBox;
        public CheckBox ActionsCheckBox;

        public override void Reset()
        {
            base.Reset();

            Children = new Drawable[]
            {
                new FlowContainer
                {
                    Anchor = Anchor.TopLeft,
                    Origin = Anchor.TopLeft,
                    Spacing = new Vector2(0,10),
                    Padding = new MarginPadding(10),
                    Direction = FlowDirection.VerticalOnly,
                    Children = new Drawable[]
                    {
                        BasicCheckBox = new CheckBox
                        {
                            Labels = new Drawable[]
                            {
                                new SpriteText
                                {
                                    Padding = new MarginPadding
                                    {
                                        Left = 20
                                    },
                                    Text = @"Basic Test"
                                }
                            }
                        },
                        ResizingWidthCheckBox = new CheckBox
                        {
                            CheckedDrawable = new Box
                            {
                                Size = new Vector2(50, 20),
                                Colour = Color4.Cyan
                            },
                            Labels = new Drawable[]
                            {
                                new SpriteText
                                {
                                    Padding = new MarginPadding
                                    {
                                        Left = 20
                                    },
                                    Text = @"Resizing Width Test"
                                },
                            }
                        },
                        ResizingHeightCheckBox = new CheckBox
                        {
                            CheckedDrawable = new Box
                            {
                                Size = new Vector2(20, 50),
                                Colour = Color4.Cyan
                            },
                            Labels = new Drawable[]
                            {
                                new SpriteText
                                {
                                    Padding = new MarginPadding
                                    {
                                        Left = 20
                                    },
                                    Text = @"Resizing Height Test"
                                }
                            }
                        },
                        ActionsCheckBox = new CheckBox
                        {
                            CheckedAction = () => ActionsCheckBox.RotateTo(45, 100),
                            UncheckedAction = () => ActionsCheckBox.RotateTo(0, 100),
                            Labels = new Drawable[]
                            {
                                new SpriteText
                                {
                                    Padding = new MarginPadding
                                    {
                                        Left = 20
                                    },
                                    Text = @"Enabled/Disabled Actions Test"
                                }
                            }
                        }
                    }
                }
            };
        }
    }
}
