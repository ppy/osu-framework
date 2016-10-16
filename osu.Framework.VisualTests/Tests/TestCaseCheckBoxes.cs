// Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using OpenTK;
using OpenTK.Graphics;
using osu.Framework.GameModes.Testing;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
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
                        BasicCheckBox = new BasicCheckBox
                        {
                            LabelText = @"Basic Test",
                        },
                        ResizingWidthCheckBox = new WidthTestCheckBox
                        {
                            LabelText = @"Resizing Width Test",
                        },
                        ResizingHeightCheckBox = new HeightTestCheckBox
                        {
                            LabelText = @"Resizing Height Test",
                        },
                        ActionsCheckBox = new ActionsTestCheckBox
                        {
                            LabelText = @"Enabled/Disabled Actions Test",
                        },
                    }
                }
            };
        }
    }

    public class WidthTestCheckBox : BasicCheckBox
    {
        protected override Drawable CreateCheckedDrawable() => new Box
        {
            Size = new Vector2(20, 50),
            Colour = Color4.Cyan
        };
    }

    public class HeightTestCheckBox : BasicCheckBox
    {
        protected override Drawable CreateCheckedDrawable() => new Box
        {
            Size = new Vector2(50, 20),
            Colour = Color4.Cyan
        };
    }

    public class ActionsTestCheckBox : BasicCheckBox
    {
        protected override void OnChecked()
        {
            base.OnChecked();
            RotateTo(45, 100);
        }

        protected override void OnUnchecked()
        {
            base.OnUnchecked();
            RotateTo(0, 100);
        }
    }
}
