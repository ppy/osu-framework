// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Graphics.Containers;
using osu.Framework.Testing;
using OpenTK;
using OpenTK.Graphics;

namespace osu.Framework.VisualTests.Tests
{
    public class TestCaseHollowEdgeEffect : TestCase
    {
        public override string Description => @"Hollow Container with EdgeEffect";

        public override void Reset()
        {
            base.Reset();

            Add(new Container()
            {
                Size = new Vector2(200f, 100f),
                Position = new Vector2(100f),

                Masking = true,
                EdgeEffect = new EdgeEffect()
                {
                    Type = EdgeEffectType.Glow,
                    Colour = Color4.Khaki,
                    Radius = 100f,
                    Roundness = 60f,
                    Hollow = true
                }
            });
        }
    }
}
