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

        private NineSliceSprite sprite = null!;

        [BackgroundDependencyLoader]
        private void load(TextureStore textures)
        {
            AddRange(new Drawable[]
            {
                new FillFlowContainer
                {
                    RelativeSizeAxes = Axes.Both,
                    Width = 0.5f,
                    Direction = FillDirection.Vertical,
                    Spacing = new Vector2(10),
                    Padding = new MarginPadding(10),
                    Children = new Drawable[]
                    {
                        new InsetTextBox("Left", leftInset),
                        new InsetTextBox("Right", rightInset),
                        new InsetTextBox("Top", topInset),
                        new InsetTextBox("Bottom", bottomInset)
                    }
                },
                new Container
                {
                    RelativeSizeAxes = Axes.Both,
                    Width = 0.5f,
                    Anchor = Anchor.TopRight,
                    Origin = Anchor.TopRight,
                    Child = sprite = new NineSliceSprite
                    {
                        // Scale = new Vector2(0.5f),
                        Texture = textures.Get("sample-texture.png"),
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
        }

        private partial class InsetTextBox : CompositeDrawable
        {
            private readonly TextBox textBox;

            private readonly Bindable<float> insetValue;

            public InsetTextBox(string label, Bindable<float> insetValue)
            {
                RelativeSizeAxes = Axes.X;
                AutoSizeAxes = Axes.Y;

                this.insetValue = insetValue.GetBoundCopy();

                AddRangeInternal(new Drawable[]
                {
                    new SpriteText
                    {
                        Text = label,
                        Anchor = Anchor.CentreLeft,
                        Origin = Anchor.CentreLeft,
                    },
                    new Container
                    {
                        RelativeSizeAxes = Axes.X,
                        AutoSizeAxes = Axes.Y,
                        Padding = new MarginPadding { Left = 80 },
                        Child = textBox = new BasicTextBox
                        {
                            RelativeSizeAxes = Axes.X,
                            Height = 40,
                            Text = insetValue.Value.ToString(CultureInfo.InvariantCulture),
                            CommitOnFocusLost = true,
                        }
                    }
                });
            }

            protected override void LoadComplete()
            {
                base.LoadComplete();

                textBox.OnCommit += textBoxOnCommit;

                textBox.TabbableContentContainer = Parent;
            }

            private void textBoxOnCommit(TextBox textBox, bool newText)
            {
                if (float.TryParse(textBox.Text, out float value))
                    insetValue.Value = value;
                else
                    textBox.Text = insetValue.Value.ToString(CultureInfo.InvariantCulture);
            }
        }
    }
}
