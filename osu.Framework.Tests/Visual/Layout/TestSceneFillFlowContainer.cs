// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using NUnit.Framework;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Localisation;
using osu.Framework.Utils;
using osuTK;
using osuTK.Graphics;

namespace osu.Framework.Tests.Visual.Layout
{
    public partial class TestSceneFillFlowContainer : FrameworkTestScene
    {
        [Test]
        public void TestDemo()
        {
            FillFlowContainer fillContainer = null!;
            AnchorDropdown originDropdown = null!;
            AutomatedBoxSpawner spawner = null!;

            AddStep("Setup", () =>
            {
                FillDirectionDropdown selectionDropdown;
                AnchorDropdown anchorDropdown;

                Children = new Drawable[]
                {
                    new Container
                    {
                        RelativeSizeAxes = Axes.Both,
                        Width = 0.2f,
                        Anchor = Anchor.TopRight,
                        Origin = Anchor.TopRight,
                        Depth = float.MinValue,
                        Children = new[]
                        {
                            new FillFlowContainer
                            {
                                Direction = FillDirection.Vertical,
                                RelativeSizeAxes = Axes.X,
                                AutoSizeAxes = Axes.Y,
                                Children = new Drawable[]
                                {
                                    new SpriteText { Text = "Fill direction" },
                                    selectionDropdown = new FillDirectionDropdown
                                    {
                                        RelativeSizeAxes = Axes.X,
                                        Items = Enum.GetValues<FillDirection>(),
                                        Current = { Value = FillDirection.Full }
                                    },
                                    new SpriteText { Text = "Child anchor" },
                                    anchorDropdown = new AnchorDropdown
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
                                        },
                                    },
                                    new SpriteText { Text = "Child origin" },
                                    originDropdown = new AnchorDropdown
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
                                        },
                                    },
                                }
                            }
                        }
                    },
                    new Container
                    {
                        Padding = new MarginPadding(25f),
                        RelativeSizeAxes = Axes.Both,
                        Children = new Drawable[]
                        {
                            fillContainer = new FillFlowContainer
                            {
                                RelativeSizeAxes = Axes.Both,
                                AutoSizeAxes = Axes.None,
                                Spacing = new Vector2(5)
                            },
                            new Box
                            {
                                Anchor = Anchor.CentreLeft,
                                Origin = Anchor.Centre,
                                RelativeSizeAxes = Axes.Y,
                                Size = new Vector2(3, 1),
                                Colour = Color4.HotPink,
                            },
                            new Box
                            {
                                Anchor = Anchor.CentreRight,
                                Origin = Anchor.Centre,
                                RelativeSizeAxes = Axes.Y,
                                Size = new Vector2(3, 1),
                                Colour = Color4.HotPink,
                            },
                            new Box
                            {
                                Anchor = Anchor.TopCentre,
                                Origin = Anchor.Centre,
                                RelativeSizeAxes = Axes.X,
                                Size = new Vector2(1, 3),
                                Colour = Color4.HotPink,
                            },
                            new Box
                            {
                                Anchor = Anchor.BottomCentre,
                                Origin = Anchor.Centre,
                                RelativeSizeAxes = Axes.X,
                                Size = new Vector2(1, 3),
                                Colour = Color4.HotPink,
                            }
                        }
                    },
                    spawner = new AutomatedBoxSpawner(fillContainer)
                    {
                        ChildAnchor = { BindTarget = anchorDropdown.Current },
                        ChildOrigin = { BindTarget = originDropdown.Current }
                    }
                };

                anchorDropdown.Current.BindValueChanged(v =>
                {
                    foreach (var child in fillContainer.Children)
                        child.Anchor = v.NewValue;
                });

                originDropdown.Current.BindValueChanged(v =>
                {
                    foreach (var child in fillContainer.Children)
                        child.Origin = v.NewValue;
                });

                selectionDropdown.Current.BindValueChanged(v =>
                {
                    fillContainer.Direction = v.NewValue;
                });
            });

            AddToggleStep("Rotate Container", state => { fillContainer.RotateTo(state ? 45f : 0, 1000); });
            AddToggleStep("Scale Container", state => { fillContainer.ScaleTo(state ? 1.2f : 1f, 1000); });
            AddToggleStep("Shear Container", state => { fillContainer.Shear = state ? new Vector2(0.5f, 0f) : new Vector2(0f, 0f); });
            AddToggleStep("Center Container Anchor", state => { fillContainer.Anchor = state ? Anchor.Centre : Anchor.TopLeft; });
            AddToggleStep("Center Container Origin", state => { fillContainer.Origin = state ? Anchor.Centre : Anchor.TopLeft; });
            AddToggleStep("Autosize Container", state =>
            {
                if (state)
                {
                    fillContainer.RelativeSizeAxes = Axes.None;
                    fillContainer.AutoSizeAxes = Axes.Both;
                }
                else
                {
                    fillContainer.AutoSizeAxes = Axes.None;
                    fillContainer.RelativeSizeAxes = Axes.Both;
                    fillContainer.Width = 1;
                    fillContainer.Height = 1;
                }
            });

            AddToggleStep("Rotate children", state =>
            {
                if (state)
                {
                    foreach (var child in fillContainer.Children)
                        child.RotateTo(45f, 1000);
                }
                else
                {
                    foreach (var child in fillContainer.Children)
                        child.RotateTo(0f, 1000);
                }
            });

            AddToggleStep("Shear children", state =>
            {
                if (state)
                {
                    foreach (var child in fillContainer.Children)
                        child.Shear = new Vector2(0.2f, 0.2f);
                }
                else
                {
                    foreach (var child in fillContainer.Children)
                        child.Shear = Vector2.Zero;
                }
            });

            AddToggleStep("Scale children", state =>
            {
                if (state)
                {
                    foreach (var child in fillContainer.Children)
                        child.ScaleTo(1.25f, 1000);
                }
                else
                {
                    foreach (var child in fillContainer.Children)
                        child.ScaleTo(1f, 1000);
                }
            });

            AddToggleStep("Randomly scale children", state =>
            {
                if (state)
                {
                    foreach (var child in fillContainer.Children)
                        child.ScaleTo(RNG.NextSingle(1, 2), 1000);
                }
                else
                {
                    foreach (var child in fillContainer.Children)
                        child.ScaleTo(1f, 1000);
                }
            });

            AddToggleStep("Randomly set child origins", state =>
            {
                if (state)
                {
                    foreach (var child in fillContainer.Children)
                    {
                        switch (RNG.Next(9))
                        {
                            case 0:
                                child.Origin = Anchor.TopLeft;
                                break;

                            case 1:
                                child.Origin = Anchor.TopCentre;
                                break;

                            case 2:
                                child.Origin = Anchor.TopRight;
                                break;

                            case 3:
                                child.Origin = Anchor.CentreLeft;
                                break;

                            case 4:
                                child.Origin = Anchor.Centre;
                                break;

                            case 5:
                                child.Origin = Anchor.CentreRight;
                                break;

                            case 6:
                                child.Origin = Anchor.BottomLeft;
                                break;

                            case 7:
                                child.Origin = Anchor.BottomCentre;
                                break;

                            case 8:
                                child.Origin = Anchor.BottomRight;
                                break;
                        }
                    }
                }
                else
                {
                    foreach (var child in fillContainer.Children)
                        child.Origin = originDropdown.Current.Value;
                }
            });

            AddToggleStep("Stop adding children", v => spawner.AddChildren = !v);
        }

        private partial class TestSceneDropdownHeader : DropdownHeader
        {
            private readonly SpriteText label;

            protected internal override LocalisableString Label
            {
                get => label.Text;
                set => label.Text = value;
            }

            public TestSceneDropdownHeader()
            {
                Foreground.Padding = new MarginPadding(4);
                BackgroundColour = new Color4(100, 100, 100, 255);
                BackgroundColourHover = Color4.HotPink;
                Children = new[]
                {
                    label = new SpriteText(),
                };
            }

            protected override DropdownSearchBar CreateSearchBar() => new BasicDropdownSearchBar();

            private partial class BasicDropdownSearchBar : DropdownSearchBar
            {
                protected override void PopIn() => this.FadeIn();

                protected override void PopOut() => this.FadeOut();

                protected override TextBox CreateTextBox() => new BasicTextBox();
            }
        }

        private partial class AutomatedBoxSpawner : Component
        {
            public bool AddChildren { get; set; } = true;

            public readonly IBindable<Anchor> ChildAnchor = new Bindable<Anchor>();
            public readonly IBindable<Anchor> ChildOrigin = new Bindable<Anchor>();

            private readonly FillFlowContainer target;

            public AutomatedBoxSpawner(FillFlowContainer target)
            {
                this.target = target;
            }

            protected override void LoadComplete()
            {
                base.LoadComplete();

                Scheduler.AddDelayed(() =>
                {
                    if (AddChildren)
                    {
                        target.Add(new Container
                        {
                            Anchor = ChildAnchor.Value,
                            Origin = ChildOrigin.Value,
                            AutoSizeAxes = Axes.Both,
                            Children = new Drawable[]
                            {
                                new Box
                                {
                                    Width = 50,
                                    Height = 50,
                                    Colour = Color4.White
                                },
                                new SpriteText
                                {
                                    Colour = Color4.Black,
                                    RelativePositionAxes = Axes.Both,
                                    Position = new Vector2(0.5f, 0.5f),
                                    Origin = Anchor.Centre,
                                    Text = target.Children.Count.ToString()
                                }
                            }
                        });
                    }
                }, 100, true);
            }
        }

        private partial class AnchorDropdown : BasicDropdown<Anchor>
        {
            protected override DropdownHeader CreateHeader() => new TestSceneDropdownHeader();
        }

        private partial class FillDirectionDropdown : BasicDropdown<FillDirection>
        {
            protected override DropdownHeader CreateHeader() => new TestSceneDropdownHeader();
        }
    }
}
