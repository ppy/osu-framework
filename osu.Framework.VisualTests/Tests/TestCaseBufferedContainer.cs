// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using OpenTK;
using System;

namespace osu.Framework.VisualTests.Tests
{
    internal class TestCaseBufferedContainer : TestCaseMasking
    {
        public override string Description => @"Buffered containers containing almost all visual effects.";

        public override void Reset()
        {
            base.Reset();

            Remove(TestContainer);

            BufferedContainer buffer;
            Add(buffer = new BufferedContainer
            {
                RelativeSizeAxes = Axes.Both,
                Children = new[] { TestContainer }
            });

            double timer = 0.0;
            buffer.OnUpdate += delegate
            {
                timer += 0.001;
                buffer.BlurSigma = new Vector2((float)Math.Abs(Math.Sin(timer) * 10 + 10), (float)Math.Abs(Math.Sin(timer) * 10 + 10));
            };
        }
    }

}
