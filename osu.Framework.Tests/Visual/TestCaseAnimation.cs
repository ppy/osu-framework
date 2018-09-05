// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Animations;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Textures;
using osu.Framework.Testing;
using OpenTK;
using OpenTK.Graphics;

namespace osu.Framework.Tests.Visual
{
    [System.ComponentModel.Description("frame-based animations")]
    public class TestCaseAnimation : TestCase
    {
        public TestCaseAnimation()
        {
            DrawableAnimation drawableAnimation;

            Add(new Container
            {
                Position = new Vector2(10f, 10f),
                RelativeSizeAxes = Axes.Both,
                Children = new Drawable[]
                {
                    new DelayedLoadWrapper(new AvatarAnimation
                    {
                        AutoSizeAxes = Axes.None,
                        RelativeSizeAxes = Axes.Both,
                        Size = new Vector2(0.25f)
                    }),
                    drawableAnimation = new DrawableAnimation
                    {
                        RelativePositionAxes = Axes.Both,
                        Position = new Vector2(0f, 0.3f),
                        AutoSizeAxes = Axes.None,
                        RelativeSizeAxes = Axes.Both,
                        Size = new Vector2(0.25f)
                    }
                }
            });

            drawableAnimation.AddFrames(new[]
            {
                new FrameData<Drawable>(new Box { Size = new Vector2(50f), Colour = Color4.Red }, 500),
                new FrameData<Drawable>(new Box { Size = new Vector2(50f), Colour = Color4.Green }, 500),
                new FrameData<Drawable>(new Box { Size = new Vector2(50f), Colour = Color4.Blue }, 500),
            });
        }

        private class AvatarAnimation : TextureAnimation
        {
            [BackgroundDependencyLoader]
            private void load(TextureStore textures)
            {
                AddFrame(textures.Get("https://a.ppy.sh/2"), 500);
                AddFrame(textures.Get("https://a.ppy.sh/3"), 500);
                AddFrame(textures.Get("https://a.ppy.sh/1876669"), 500);
            }
        }
    }
}
