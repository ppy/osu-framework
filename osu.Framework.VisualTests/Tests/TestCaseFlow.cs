using OpenTK;
using OpenTK.Graphics;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Colour;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.MathUtils;
using osu.Framework.Screens.Testing;
using osu.Framework.Threading;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace osu.Framework.VisualTests.Tests
{
    class TestCaseFlow : TestCase
    {
        public override string Name => "Flow";
        public override string Description => "Test lots of different settings for Flow Containers";

        FlowTestCase current;
        TestCaseDropdown selectionDropdown;
        Container testContainer;

        protected override Container<Drawable> Content => testContainer;

        public override void Reset()
        {
            base.Reset();

            AddInternal(testContainer = new Container()
            {
                RelativeSizeAxes = Axes.Both,
            });
            ButtonsContainer.Add(selectionDropdown = new TestCaseDropdown
            {
                Width = 150,
                Position = new Vector2(10, 10),
                Description = @"Drop-down menu",
                Items = null,
                SelectedIndex = 0,
            });
            changeTest(FlowTestCase.FillWithSpacing);
        }

        protected override void Update()
        {
            base.Update();

            if (current != selectionDropdown.SelectedValue)
                changeTest(selectionDropdown.SelectedValue);
        }

        private void changeTest(FlowTestCase testCase)
        {
            current = testCase;
            testContainer.Clear();

            var method = GetType().GetMethods(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance).SingleOrDefault(m => m.GetCustomAttribute<FlowTestCaseAttribute>()?.TestCase == testCase);
            if(method != null)
                method.Invoke(this, new object[0]);
        }

        private FlowContainer buildTest(IFlowStrategy flowStrategy)
        {
            ButtonsContainer.RemoveAll(btn => btn != selectionDropdown);

            FlowContainer fc;
            var cnt = new Container()
            {
                Padding = new MarginPadding(25f) { Top = 100f },
                RelativeSizeAxes = Axes.Both,
                Children = new[]
                {
                    fc = new FlowContainer()
                    {
                        RelativeSizeAxes = Axes.Both,
                        AutoSizeAxes = Axes.None,
                        FlowStrategy = flowStrategy
                    }
                }
            };
            Add(cnt);

            var rotateBtn = AddButton("Rotate Container", () =>
            {
                if (fc.Rotation > 0)
                    fc.RotateTo(0f, 1000);
                else
                    fc.RotateTo(45f, 1000);
            });
            AddButton("Scale Container", () =>
            {
                if (fc.Scale.X == 1f)
                    fc.ScaleTo(1.2f, 1000);
                else
                    fc.ScaleTo(1f, 1000);
            });
            AddButton("Shear Container", () =>
            {
                if (fc.Shear.X == 0)
                    fc.Shear = new Vector2(0.5f, 0f);
                else
                    fc.Shear = new Vector2(0f, 0f);
            });
            AddToggle("Center Container Anchor", (state) =>
            {
                if (state)
                    fc.Anchor = Anchor.Centre;
                else
                    fc.Anchor = Anchor.TopLeft;
            });
            AddToggle("Center Container Origin", (state) =>
            {
                if (state)
                    fc.Origin = Anchor.Centre;
                else
                    fc.Origin = Anchor.TopLeft;
            });
            AddToggle("Autosize Container", (state) =>
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
            AddToggle("Anchor TopCenter children", (state) =>
            {
                if (state)
                {
                    foreach (var child in fc.Children)
                        child.Anchor = Anchor.TopCentre;
                }
                else
                {
                    foreach (var child in fc.Children)
                        child.Anchor = Anchor.TopLeft;
                }
            });
            AddToggle("Rotate children", (state) =>
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
            AddToggle("Shear children", (state) =>
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
            AddToggle("Scale children", (state) =>
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
            AddToggle("Change children origin", (state) =>
            {
                if (state)
                {
                    foreach (var child in fc.Children)
                        child.Origin = Anchor.Centre;
                }
                else
                {
                    foreach (var child in fc.Children)
                        child.Origin = Anchor.TopLeft;
                }
            });
            var addChildrenBtn = AddToggle("Stop adding children", (state) => { });
            cnt.Position = new Vector2(rotateBtn.Width, 0f);
            cnt.Padding = new MarginPadding(25f) { Top = cnt.Padding.Top, Right = 25f + cnt.Position.X };
            Add(new Box { Colour = Color4.HotPink, Width = 3, Height = 3, Position = fc.Parent.ToSpaceOfOtherDrawable(fc.BoundingBox.TopLeft, this), Origin = Anchor.Centre });
            Add(new Box { Colour = Color4.HotPink, Width = 3, Height = 3, Position = fc.Parent.ToSpaceOfOtherDrawable(fc.BoundingBox.TopRight, this), Origin = Anchor.Centre });
            Add(new Box { Colour = Color4.HotPink, Width = 3, Height = 3, Position = fc.Parent.ToSpaceOfOtherDrawable(fc.BoundingBox.BottomLeft, this), Origin = Anchor.Centre });
            Add(new Box { Colour = Color4.HotPink, Width = 3, Height = 3, Position = fc.Parent.ToSpaceOfOtherDrawable(fc.BoundingBox.BottomRight, this), Origin = Anchor.Centre });

            ScheduledDelegate d = null;
            d = Scheduler.AddDelayed(
                () =>
                {
                    if (fc.Parent == null)
                        d.Cancel();

                    if(addChildrenBtn.State)
                    {
                        fc.Invalidate();
                    }

                    if (fc.Children.Count() < 1000 && !addChildrenBtn.State)
                    {
                        fc.Add(new Container
                        {
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

            return fc;
        }

        [FlowTestCase(FlowTestCase.FillWithSpacing)]
        private void test1()
        {
            // Expected behaviour: Boxes appear left-to-right, top-to-bottom
            // and wrap into the next line
            buildTest(new FillFlowStrategy()
            {
                HorizontalFlow = HorizontalDirection.LeftToRight,
                VerticalFlow = VerticalDirection.TopToBottom,
                Spacing = new Vector2(5, 5)
            });
        }

        [FlowTestCase(FlowTestCase.HorizontalWithSpacing)]
        private void test2()
        {
            // Expected behaviour: Boxes appear left-to-right
            // and start going off-screen
            buildTest(new FillFlowStrategy()
            {
                HorizontalFlow = HorizontalDirection.LeftToRight,
                VerticalFlow = VerticalDirection.None,
                Spacing = new Vector2(5, 5)
            });
        }

        [FlowTestCase(FlowTestCase.VerticalWithSpacing)]
        private void test3()
        {
            // Expected behaviour: Boxes appear top-to-bottom
            // and start going off-screen
            buildTest(new FillFlowStrategy()
            {
                HorizontalFlow = HorizontalDirection.None,
                VerticalFlow = VerticalDirection.TopToBottom,
                Spacing = new Vector2(5, 5)
            });
        }

        [FlowTestCase(FlowTestCase.RightToLeftWithSpacing)]
        private void test4()
        {
            // Expected behaviour: Boxes appear right-to-left
            // and start going off-screen
            buildTest(new FillFlowStrategy()
            {
                HorizontalFlow = HorizontalDirection.RightToLeft,
                VerticalFlow = VerticalDirection.None,
                Spacing = new Vector2(5, 5)
            });
        }

        [FlowTestCase(FlowTestCase.BottomToTopWithSpacing)]
        private void test5()
        {
            // Expected behaviour: Boxes appear bottom-to-top
            // and start going off-screen
            buildTest(new FillFlowStrategy()
            {
                HorizontalFlow = HorizontalDirection.None,
                VerticalFlow = VerticalDirection.BottomToTop,
                Spacing = new Vector2(5, 5)
            });
        }

        [FlowTestCase(FlowTestCase.FillInverseWithSpacing)]
        private void test6()
        {
            // Expected behaviour: Boxes appear right-to-left, bottom-to-top
            // and wrap into the next line
            buildTest(new FillFlowStrategy()
            {
                HorizontalFlow = HorizontalDirection.RightToLeft,
                VerticalFlow = VerticalDirection.BottomToTop,
                Spacing = new Vector2(5, 5)
            });
        }

        [FlowTestCase(FlowTestCase.FillRightToLeftWithSpacing)]
        private void test7()
        {
            // Expected behaviour: Boxes appear right-to-left, top-to-bottom
            // and wrap into the next line
            buildTest(new FillFlowStrategy()
            {
                HorizontalFlow = HorizontalDirection.RightToLeft,
                VerticalFlow = VerticalDirection.TopToBottom,
                Spacing = new Vector2(5, 5)
            });
        }

        [FlowTestCase(FlowTestCase.FillBottomToTopWithSpacing)]
        private void test8()
        {
            // Expected behaviour: Boxes appear left-to-right, top-to-bottom
            // and wrap into the next line
            buildTest(new FillFlowStrategy()
            {
                HorizontalFlow = HorizontalDirection.LeftToRight,
                VerticalFlow = VerticalDirection.BottomToTop,
                Spacing = new Vector2(5, 5)
            });
        }

        private class TestCaseDropdown : DropDownMenu<FlowTestCase>
        {
            protected override DropDownHeader CreateHeader()
            {
                return new TestCaseDropDownHeader();
            }

            protected override IEnumerable<DropDownMenuItem<FlowTestCase>> GetDropDownItems(IEnumerable<KeyValuePair<string, FlowTestCase>> values)
            {
                return Enum.GetValues(typeof(FlowTestCase)).Cast<FlowTestCase>().Select(ftc => new TestCaseDropDownMenuItem(ftc));
            }
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

        private class TestCaseDropDownMenuItem : DropDownMenuItem<FlowTestCase>
        {
            public TestCaseDropDownMenuItem(FlowTestCase testCase) : base(testCase.ToString(), testCase)
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
            public FlowTestCase TestCase { get; private set; }

            public FlowTestCaseAttribute(FlowTestCase testCase)
            {
                TestCase = testCase;
            }
        }
        enum FlowTestCase
        {
            FillWithSpacing,
            BottomToTopWithSpacing,
            HorizontalWithSpacing,
            VerticalWithSpacing,
            RightToLeftWithSpacing,
            FillRightToLeftWithSpacing,
            FillBottomToTopWithSpacing,
            FillInverseWithSpacing
        }
    }
}