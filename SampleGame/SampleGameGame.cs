// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework;
using osu.Framework.Graphics;
using osuTK;
using osuTK.Graphics;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Allocation;
using osu.Framework.Graphics.Containers;

namespace SampleGame
{
    public partial class SampleGameGame : Game
    {
        [BackgroundDependencyLoader]
        private void load()
        {
            Add(new FillFlowContainer
            {
                AutoSizeAxes = Axes.Both,
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                Direction = FillDirection.Horizontal,
                Spacing = new Vector2(5),
                Children = new Drawable[]
                {
                    new BufferedContainer
                    {
                        Size = new Vector2(200),
                        Children = new[]
                        {
                            new Box
                            {
                                RelativeSizeAxes = Axes.Both,
                                Colour = Color4.Tomato
                            },
                            new Box
                            {
                                RelativeSizeAxes = Axes.Both,
                                Width = 0.5f,
                                Colour = Color4.Black,
                                Alpha = 0.5f
                            }
                        }
                    },
                    new Container
                    {
                        Size = new Vector2(200),
                        Children = new[]
                        {
                            new Box
                            {
                                RelativeSizeAxes = Axes.Both,
                                Colour = Color4.Tomato
                            },
                            new Box
                            {
                                RelativeSizeAxes = Axes.Both,
                                Width = 0.5f,
                                Colour = Color4.Black,
                                Alpha = 0.5f
                            }
                        }
                    }
                }
            });
        }
    }
}
