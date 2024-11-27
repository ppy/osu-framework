// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osuTK;
using osuTK.Graphics;

namespace osu.Framework.Tests.Visual.Graphics
{
    public partial class TestSceneScissor : FrameworkTestScene
    {
        public TestSceneScissor()
        {
            Children = new Drawable[]
            {
                new Container
                {
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    RelativeSizeAxes = Axes.Both,
                    Size = new Vector2(0.5f),
                    Masking = true,
                    Child = new Container
                    {
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                        RelativeSizeAxes = Axes.Both,
                        Size = new Vector2(2),
                        Masking = true,
                        Child = new Box
                        {
                            RelativeSizeAxes = Axes.Both
                        },
                    }
                },
                new Container
                {
                    Name = "Overlays",
                    RelativeSizeAxes = Axes.Both,
                    Masking = true,
                    BorderColour = Color4.Red,
                    BorderThickness = 4,
                    Children = new Drawable[]
                    {
                        new Box
                        {
                            RelativeSizeAxes = Axes.Both,
                            Alpha = 0,
                            AlwaysPresent = true,
                        },
                        new SpriteText
                        {
                            Anchor = Anchor.TopCentre,
                            Origin = Anchor.TopCentre,
                            Text = "Invisible Area",
                            Colour = Color4.Red,
                            Font = FontUsage.Default.With(size: 36)
                        },
                        new Container
                        {
                            Anchor = Anchor.Centre,
                            Origin = Anchor.Centre,
                            RelativeSizeAxes = Axes.Both,
                            Size = new Vector2(0.5f),
                            Masking = true,
                            BorderColour = Color4.Green,
                            BorderThickness = 4,
                            Children = new Drawable[]
                            {
                                new Box
                                {
                                    RelativeSizeAxes = Axes.Both,
                                    Alpha = 0,
                                    AlwaysPresent = true
                                },
                                new SpriteText
                                {
                                    Anchor = Anchor.TopCentre,
                                    Origin = Anchor.TopCentre,
                                    Text = "Visible Area",
                                    Colour = Color4.Green,
                                    Font = FontUsage.Default.With(size: 36)
                                }
                            }
                        }
                    }
                }
            };
        }
    }
}
