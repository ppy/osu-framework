// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Globalization;
using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Textures;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Input.Events;
using osuTK;

namespace osu.Framework.Tests.Visual.Sprites
{
    [TestFixture]
    public partial class TestSceneNineSliceSprite : FrameworkTestScene
    {
        private readonly Bindable<float> topInset = new BindableFloat(100) { MinValue = 0 };
        private readonly Bindable<float> bottomInset = new BindableFloat(100) { MinValue = 0 };
        private readonly Bindable<float> leftInset = new BindableFloat(100) { MinValue = 0 };
        private readonly Bindable<float> rightInset = new BindableFloat(100) { MinValue = 0 };
        private readonly Bindable<Axes> relativeInsetAxes = new Bindable<Axes>();

        private NineSliceSprite sprite = null!;

        [BackgroundDependencyLoader]
        private void load(TextureStore textures)
        {
            AddRange(new Drawable[]
            {
                new FillFlowContainer
                {
                    RelativeSizeAxes = Axes.Both,
                    Width = 0.3f,
                    Direction = FillDirection.Vertical,
                    Spacing = new Vector2(10),
                    Padding = new MarginPadding(10),
                    Children = new Drawable[]
                    {
                        new LabelledContainer("Left")
                        {
                            Child = new InsetTextBox(leftInset) { TabbableContentContainer = this }
                        },
                        new LabelledContainer("Right")
                        {
                            Child = new InsetTextBox(rightInset) { TabbableContentContainer = this }
                        },
                        new LabelledContainer("Top")
                        {
                            Child = new InsetTextBox(topInset) { TabbableContentContainer = this }
                        },
                        new LabelledContainer("Bottom")
                        {
                            Child = new InsetTextBox(bottomInset) { TabbableContentContainer = this }
                        },
                        new LabelledContainer("RelativeInsetAxes")
                        {
                            Child = new Container
                            {
                                RelativeSizeAxes = Axes.X,
                                Height = 30,
                                Child = new BasicDropdown<Axes>
                                {
                                    RelativeSizeAxes = Axes.X,
                                    Items = new[]
                                    {
                                        Axes.None,
                                        Axes.X,
                                        Axes.Y,
                                        Axes.Both,
                                    },
                                    Current = relativeInsetAxes,
                                }
                            }
                        }
                    }
                },
                new Container
                {
                    RelativeSizeAxes = Axes.Both,
                    Width = 0.7f,
                    Anchor = Anchor.TopRight,
                    Origin = Anchor.TopRight,
                    Child = sprite = new NineSliceSprite
                    {
                        Texture = textures.Get("sample-nine-slice-texture.png"),
                        TextureInset = new MarginPadding(100),
                    }
                }
            });

            sprite.ResizeTo(new Vector2(600, 400), 1000, Easing.InOutCubic)
                  .Then()
                  .ResizeTo(new Vector2(256, 256), 1000, Easing.InOutCubic)
                  .Loop();

            leftInset.BindValueChanged(e => sprite.TextureInset = sprite.TextureInset with { Left = e.NewValue });
            topInset.BindValueChanged(e => sprite.TextureInset = sprite.TextureInset with { Top = e.NewValue });
            rightInset.BindValueChanged(e => sprite.TextureInset = sprite.TextureInset with { Right = e.NewValue });
            bottomInset.BindValueChanged(e => sprite.TextureInset = sprite.TextureInset with { Bottom = e.NewValue });

            relativeInsetAxes.BindValueChanged(e =>
            {
                sprite.TextureInsetRelativeAxes = e.NewValue;

                leftInset.Value = sprite.TextureInset.Left;
                topInset.Value = sprite.TextureInset.Top;
                rightInset.Value = sprite.TextureInset.Right;
                bottomInset.Value = sprite.TextureInset.Bottom;
            });
        }

        private partial class InsetTextBox : BasicTextBox
        {
            private readonly Bindable<float> insetValue;

            public InsetTextBox(Bindable<float> insetValue)
            {
                this.insetValue = insetValue.GetBoundCopy();

                RelativeSizeAxes = Axes.X;
                Height = 30;
                Text = insetValue.Value.ToString(CultureInfo.InvariantCulture);
                CommitOnFocusLost = true;
            }

            protected override void LoadComplete()
            {
                base.LoadComplete();

                OnCommit += textBoxOnCommit;

                insetValue.BindValueChanged(e => Text = e.NewValue.ToString(CultureInfo.InvariantCulture));
            }

            private void textBoxOnCommit(TextBox textBox, bool newText)
            {
                if (float.TryParse(textBox.Text, out float value))
                    insetValue.Value = value;
                else
                    textBox.Text = insetValue.Value.ToString(CultureInfo.InvariantCulture);
            }

            protected override void OnFocus(FocusEvent e)
            {
                base.OnFocus(e);

                SelectAll();
            }
        }

        private partial class LabelledContainer : Container
        {
            public LabelledContainer(string label)
            {
                RelativeSizeAxes = Axes.X;
                AutoSizeAxes = Axes.Y;

                InternalChildren = new Drawable[]
                {
                    new SpriteText
                    {
                        Text = label,
                        Font = FrameworkFont.Regular.With(size: 20),
                        Anchor = Anchor.CentreLeft,
                        Origin = Anchor.CentreLeft,
                    },
                    content = new Container
                    {
                        Padding = new MarginPadding { Left = 140 },
                        RelativeSizeAxes = Axes.X,
                        AutoSizeAxes = Axes.Y
                    }
                };
            }

            private readonly Container content;

            protected override Container<Drawable> Content => content;
        }
    }
}
