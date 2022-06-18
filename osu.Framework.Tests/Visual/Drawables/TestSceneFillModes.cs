// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Textures;
using osu.Framework.Testing;
using osuTK;
using osuTK.Graphics;

namespace osu.Framework.Tests.Visual.Drawables
{
    [System.ComponentModel.Description("sprite stretching")]
    public class TestSceneFillModes : GridTestScene
    {
        public TestSceneFillModes()
            : base(3, 3)
        {
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            FillMode[] fillModes =
            {
                FillMode.Stretch,
                FillMode.Fit,
                FillMode.Fill,
            };

            float[] aspects = { 1, 2, 0.5f };

            for (int i = 0; i < Rows; ++i)
            {
                for (int j = 0; j < Cols; ++j)
                {
                    Cell(i, j).AddRange(new Drawable[]
                    {
                        new SpriteText
                        {
                            Text = $"{nameof(FillMode)}=FillMode.{fillModes[i]}, {nameof(FillAspectRatio)}={aspects[j]}",
                            Font = new FontUsage(size: 20),
                        },
                        new Container
                        {
                            RelativeSizeAxes = Axes.Both,
                            Size = new Vector2(0.5f),
                            Anchor = Anchor.Centre,
                            Origin = Anchor.Centre,
                            Masking = true,
                            Children = new Drawable[]
                            {
                                new Box
                                {
                                    RelativeSizeAxes = Axes.Both,
                                    Colour = Color4.Blue,
                                },
                                new Sprite
                                {
                                    RelativeSizeAxes = Axes.Both,
                                    Texture = texture,
                                    Anchor = Anchor.Centre,
                                    Origin = Anchor.Centre,
                                    FillMode = fillModes[i],
                                    FillAspectRatio = aspects[j],
                                }
                            }
                        }
                    });
                }
            }
        }

        private Texture texture;

        [BackgroundDependencyLoader]
        private void load(TextureStore store)
        {
            texture = store.Get(@"sample-texture");
        }
    }
}
