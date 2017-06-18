// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using OpenTK;
using OpenTK.Graphics;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Animations;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Textures;
using osu.Framework.Testing;
using System.Collections.Generic;

namespace osu.Framework.VisualTests.Tests
{
    internal class TestCaseAnimation : TestCase
    {
        private AvatarAnimation textureAnimation;
        private DrawableAnimation drawableAnimation;

        public override string Description => "Various frame-based animations";

        public override void Reset()
        {
            base.Reset();

            Add(new Container
            {
                Position = new Vector2(10f, 10f),
                RelativeSizeAxes = Axes.Both,
                Children = new Drawable[]
                {
                    new DelayedLoadWrapper(textureAnimation = new AvatarAnimation
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

            bool isScalingUp = true;
            textureAnimation.OnUpdate = drawable =>
            {
                if (textureAnimation.Size.X < 0.3f && isScalingUp)
                    textureAnimation.Size += new Vector2(0.001f);
                else if (textureAnimation.Size.X >= 0.3f && isScalingUp)
                    isScalingUp = false;
                else if (textureAnimation.Size.X > 0.2f && !isScalingUp)
                    textureAnimation.Size -= new Vector2(0.001f);
                else if (textureAnimation.Size.X <= 0.2f && !isScalingUp)
                    isScalingUp = true;
            };
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
