// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Textures;
using osu.Framework.Testing;
using OpenTK;
using OpenTK.Graphics;

namespace osu.Framework.VisualTests.Tests
{
    internal class TestCaseOnlineTextures : TestCase
    {
        private FillFlowContainer flow;

        public override void Reset()
        {
            base.Reset();

            Children = new Drawable[]
            {
                new ScrollContainer
                {
                    RelativeSizeAxes = Axes.Both,
                    Children = new Drawable[]
                    {
                        flow = new FillFlowContainer
                        {
                            RelativeSizeAxes = Axes.X,
                            AutoSizeAxes = Axes.Y,
                        }
                    }
                }
            };

            for (int i = 55; i < 2048; i++)
                flow.Add(new Container
                {
                    Size = new Vector2(128),
                    Children = new Drawable[]
                    {
                        new DelayedLoadContainer
                        {
                            RelativeSizeAxes = Axes.Both,
                            FinishedLoading = d => {
                                if ((d.Children.First() as Sprite)?.Texture == null)
                                {
                                    d.Add(new SpriteText {
                                        Colour = Color4.Gray,
                                        Text = @"nope",
                                        Anchor = Anchor.Centre,
                                        Origin = Anchor.Centre,
                                    });
                                }
                            },
                            Children = new Drawable[]
                            {
                                new Avatar(i) { RelativeSizeAxes = Axes.Both }
                            }
                        },
                        new SpriteText { Text = i.ToString() },
                    }
                });
        }
    }

    public class Avatar : Sprite
    {
        private readonly int userId;

        public Avatar(int userId)
        {
            this.userId = userId;
        }

        [BackgroundDependencyLoader]
        private void load(TextureStore textures)
        {
            Texture = textures.Get($@"https://a.ppy.sh/{userId}");
        }
    }
}
