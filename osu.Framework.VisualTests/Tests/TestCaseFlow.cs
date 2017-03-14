// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Screens.Testing;
using osu.Framework.Threading;
using OpenTK;
using OpenTK.Graphics;

namespace osu.Framework.VisualTests.Tests
{
    internal class TestCaseFlow : TestCase
    {
        public override string Description => "Test lots of different settings for Flow Containers";

        private FillFlowContainer dropdownContainer;

        private FlowTestCase current;
        private FillDirectionDropdown selectionDropdown;
        private Container testContainer;

        private Anchor childAnchor = Anchor.TopLeft;
        private AnchorDropDown anchorDropdown;

        private Anchor childOrigin = Anchor.TopLeft;
        private AnchorDropDown originDropdown;

        private FillFlowContainer fc;
        private ScheduledDelegate scheduledAdder;

        protected override Container<Drawable> Content => testContainer;

        public override void Reset()
        {
            base.Reset();

            scheduledAdder?.Cancel();

            AddInternal(testContainer = new Container
            {
                RelativeSizeAxes = Axes.Both,
            });

            ButtonsContainer.Add(dropdownContainer = new FillFlowContainer
            {
                Direction = FillDirection.Vertical,
                RelativeSizeAxes = Axes.X,
                AutoSizeAxes = Axes.Y,
                Children = new Drawable[]
                {
                    new SpriteText { Text = @"Fill mode" },
                    selectionDropdown = new FillDirectionDropdown
                    {
                        RelativeSizeAxes = Axes.X,
                        Items = Enum.GetValues(typeof(FlowTestCase)).Cast<FlowTestCase>()
                            .Select(value => new KeyValuePair<string, FlowTestCase>(value.ToString(), value)),
                        SelectedIndex = 0,
                    },
                    new SpriteText { Text = @"Child anchor" },
                    anchorDropdown = new AnchorDropDown
                    {
                        RelativeSizeAxes = Axes.X,
                        Items = new[]
                        {
                            Anchor.TopLeft,
                            Anchor.TopCentre,
                            Anchor.TopRight,

                            Anchor.CentreLeft,
                            Anchor.Centre,
                            Anchor.CentreRight,

                            Anchor.BottomLeft,
                            Anchor.BottomCentre,
                            Anchor.BottomRight,
                        }.Select(anchor => new KeyValuePair<string, Anchor>(anchor.ToString(), anchor)),
                        SelectedIndex = 0,
                    },
                    new SpriteText { Text = @"Child origin" },
                    originDropdown = new AnchorDropDown
                    {
                        RelativeSizeAxes = Axes.X,
                        Items = new[]
                        {
                            Anchor.TopLeft,
                            Anchor.TopCentre,
                            Anchor.TopRight,

                            Anchor.CentreLeft,
                            Anchor.Centre,
                            Anchor.CentreRight,

                            Anchor.BottomLeft,
                            Anchor.BottomCentre,
                            Anchor.BottomRight,
                        }.Select(anchor => new KeyValuePair<string, Anchor>(anchor.ToString(), anchor)),
                        SelectedIndex = 0,
                    },
                }
            });

            changeTest(FlowTestCase.Full);
        }

        protected override void Update()
        {
            base.Update();

            if (current != selectionDropdown.SelectedValue)
                changeTest(selectionDropdown.SelectedValue);

            if (childAnchor != anchorDropdown.SelectedValue)
            {
                childAnchor = anchorDropdown.SelectedValue;
                foreach (var child in fc.Children)
                    child.Anchor = childAnchor;
            }

            if (childOrigin != originDropdown.SelectedValue)
            {
                childOrigin = originDropdown.SelectedValue;
                foreach (var child in fc.Children)
                    child.Origin = childOrigin;
            }
        }

        private void changeTest(FlowTestCase testCase)
        {
            current = testCase;
            testContainer.Clear();

            var method = GetType().GetMethods(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance).SingleOrDefault(m => m.GetCustomAttribute<FlowTestCaseAttribute>()?.TestCase == testCase);
            if(method != null)
                method.Invoke(this, new object[0]);
        }

        private void buildTest(FillDirection dir, Vector2 spacing)
        {
            ButtonsContainer.RemoveAll(dr => dr != dropdownContainer);

            var cnt = new Container
            {
                Padding = new MarginPadding(25f) { Top = 100f },
                RelativeSizeAxes = Axes.Both,
                Children = new[]
                {
                    fc = new FillFlowContainer
                    {
                        RelativeSizeAxes = Axes.Both,
                        AutoSizeAxes = Axes.None,
                        Direction = dir,
                        Spacing = spacing,
                    }
                }
            };
            Add(cnt);

            var rotateBtn = AddToggle("Rotate Container", state =>
            {
                fc.RotateTo(state ? 45f : 0, 1000);
            });
            AddToggle("Scale Container", state =>
            {
                fc.ScaleTo(state ? 1.2f : 1f, 1000);
            });
            AddToggle("Shear Container", state =>
            {
                fc.Shear = state ? new Vector2(0.5f, 0f) : new Vector2(0f, 0f);
            });
            AddToggle("Center Container Anchor", state =>
            {
                fc.Anchor = state ? Anchor.Centre : Anchor.TopLeft;
            });
            AddToggle("Center Container Origin", state =>
            {
                fc.Origin = state ? Anchor.Centre : Anchor.TopLeft;
            });
            AddToggle("Autosize Container", state =>
            {
                if (state)
                {
                    fc.RelativeSizeAxes = Axes.None;
                    fc.AutoSizeAxes = Axes.Both;
                }
                else
                {
                    fc.AutoSizeAxes = Axes.None;
                    fc.RelativeSizeAxes = Axes.Both;
                    fc.Width = 1;
                    fc.Height = 1;
                }
            });
            AddToggle("Rotate children", state =>
            {
                if (state)
                {
                    foreach (var child in fc.Children)
                        child.RotateTo(45f, 1000);
                }
                else
                {
                    foreach (var child in fc.Children)
                        child.RotateTo(0f, 1000);
                }
            });
            AddToggle("Shear children", state =>
            {
                if (state)
                {
                    foreach (var child in fc.Children)
                        child.Shear = new Vector2(0.2f, 0.2f);
                }
                else
                {
                    foreach (var child in fc.Children)
                        child.Shear = Vector2.Zero;
                }
            });
            AddToggle("Scale children", state =>
            {
                if (state)
                {
                    foreach (var child in fc.Children)
                        child.ScaleTo(1.25f, 1000);
                }
                else
                {
                    foreach (var child in fc.Children)
                        child.ScaleTo(1f, 1000);
                }
            });
            var addChildrenBtn = AddToggle("Stop adding children", state => { });
            cnt.Position = new Vector2(rotateBtn.Width, 0f);
            cnt.Padding = new MarginPadding(25f) { Top = cnt.Padding.Top, Right = 25f + cnt.Position.X };
            Add(new Box { Colour = Color4.HotPink, Width = 3, Height = 3, Position = fc.Parent.ToSpaceOfOtherDrawable(fc.BoundingBox.TopLeft, this), Origin = Anchor.Centre });
            Add(new Box { Colour = Color4.HotPink, Width = 3, Height = 3, Position = fc.Parent.ToSpaceOfOtherDrawable(fc.BoundingBox.TopRight, this), Origin = Anchor.Centre });
            Add(new Box { Colour = Color4.HotPink, Width = 3, Height = 3, Position = fc.Parent.ToSpaceOfOtherDrawable(fc.BoundingBox.BottomLeft, this), Origin = Anchor.Centre });
            Add(new Box { Colour = Color4.HotPink, Width = 3, Height = 3, Position = fc.Parent.ToSpaceOfOtherDrawable(fc.BoundingBox.BottomRight, this), Origin = Anchor.Centre });

            scheduledAdder?.Cancel();
            scheduledAdder = Scheduler.AddDelayed(
                () =>
                {
                    if (fc.Parent == null)
                        scheduledAdder.Cancel();

                    if(addChildrenBtn.State)
                    {
                        fc.Invalidate();
                    }

                    if (fc.Children.Count() < 1000 && !addChildrenBtn.State)
                    {
                        fc.Add(new Container
                        {
                            Anchor = childAnchor,
                            Origin = childOrigin,
                            AutoSizeAxes = Axes.Both,
                            Children = new Drawable[]
                            {
                                new Box
                                {
                                    Width = 50,
                                    Height = 50,
                                    Colour = new Color4(255, 255, 255, 255)
                                },
                                new SpriteText
                                {
                                    Colour = Color4.Black,
                                    RelativePositionAxes = Axes.Both,
                                    Position = new Vector2(0.5f, 0.5f),
                                    Origin = Anchor.Centre,
                                    Text = fc.Children.Count().ToString()
                                }
                            }
                        });
                    }
                },
                100,
                true
            );
        }

        [FlowTestCase(FlowTestCase.Full)]
        private void test1()
        {
            buildTest(FillDirection.Full, new Vector2(5, 5));
        }

        [FlowTestCase(FlowTestCase.Horizontal)]
        private void test2()
        {
            buildTest(FillDirection.Horizontal, new Vector2(5, 5));
        }

        [FlowTestCase(FlowTestCase.Vertical)]
        private void test3()
        {
            buildTest(FillDirection.Vertical, new Vector2(5, 5));
        }

        private class TestCaseDropDownHeader : DropDownHeader
        {
            private SpriteText label;
            protected override string Label
            {
                get { return label.Text; }
                set { label.Text = value; }
            }

            public TestCaseDropDownHeader()
            {
                Foreground.Padding = new MarginPadding(4);
                BackgroundColour = new Color4(100, 100, 100, 255);
                BackgroundColourHover = Color4.HotPink;
                Children = new[]
                {
                    label = new SpriteText(),
                };
            }
        }

        private class AnchorDropDown : DropDownMenu<Anchor>
        {
            protected override DropDownHeader CreateHeader()
            {
                return new TestCaseDropDownHeader();
            }

            protected override DropDownMenuItem<Anchor> CreateDropDownItem(string key, Anchor value) => new AnchorDropdownMenuItem(value);
        }

        private class AnchorDropdownMenuItem : DropDownMenuItem<Anchor>
        {
            public AnchorDropdownMenuItem(Anchor anchor) : base(anchor.ToString(), anchor)
            {
                AutoSizeAxes = Axes.Y;
                Foreground.Padding = new MarginPadding(2);

                Children = new[]
                {
                    new SpriteText { Text = anchor.ToString() },
                };
            }
        }

        private class FillDirectionDropdown : DropDownMenu<FlowTestCase>
        {
            protected override DropDownHeader CreateHeader()
            {
                return new TestCaseDropDownHeader();
            }

            protected override DropDownMenuItem<FlowTestCase> CreateDropDownItem(string key, FlowTestCase value) => new FillDirectionDropdownMenuItem(value);
        }

        private class FillDirectionDropdownMenuItem : DropDownMenuItem<FlowTestCase>
        {
            public FillDirectionDropdownMenuItem(FlowTestCase testCase) : base(testCase.ToString(), testCase)
            {
                AutoSizeAxes = Axes.Y;
                Foreground.Padding = new MarginPadding(2);

                Children = new[]
                {
                    new SpriteText { Text = testCase.ToString() },
                };
            }
        }

        [AttributeUsage(AttributeTargets.Method)]
        private class FlowTestCaseAttribute : Attribute
        {
            public FlowTestCase TestCase { get; }

            public FlowTestCaseAttribute(FlowTestCase testCase)
            {
                TestCase = testCase;
            }
        }

        private enum FlowTestCase
        {
            Full,
            Horizontal,
            Vertical,
        }
    }
}