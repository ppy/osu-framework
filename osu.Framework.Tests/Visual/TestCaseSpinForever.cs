// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Testing;
using OpenTK;
using OpenTK.Graphics;

namespace osu.Framework.Tests.Visual
{
    public class TestCaseSpinForever : TestCase
    {
        private readonly Container box;

        public TestCaseSpinForever()
        {
            Child = box = new Container
            {
                RelativeSizeAxes = Axes.Both,
                Size = new Vector2(0.25f),
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                EdgeEffect = new EdgeEffectParameters
                {
                    Type = EdgeEffectType.Glow,
                    Radius = 20,
                    Colour = Color4.Blue,
                },
                Child = new Box
                {
                    RelativeSizeAxes = Axes.Both,
                }
            };
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();
            box.Spin(1000, RotationDirection.Clockwise);
        }
    }
}
