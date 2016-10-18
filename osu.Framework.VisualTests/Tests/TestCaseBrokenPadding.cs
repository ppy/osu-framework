// Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using OpenTK;
using OpenTK.Graphics;
using osu.Framework.GameModes.Testing;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.UserInterface;

namespace osu.Framework.VisualTests.Tests
{
    class TestCaseBrokenPadding : TestCase
    {
        public override string Name => @"Broken Padding";

        public override void Reset()
        {
            base.Reset();

            Children = new Drawable[]
            {
                new AutoSizeContainer
                {
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    Children = new Drawable[]
                    {
                        new Box
                        {
                            Colour = Color4.Blue,
                            RelativeSizeAxes = Axes.Both,
                        },
                        new AutoSizeContainer
                        {
                            Padding = new MarginPadding(50),
                            Children = new Drawable[]
                            {
                                new Box
                                {
                                    Size = new Vector2(50),
                                    Colour = Color4.Yellow,
                                },
                            }
                        }
                    }
                }
            };
        }
    }
}
