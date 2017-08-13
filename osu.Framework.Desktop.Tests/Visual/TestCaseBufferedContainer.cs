﻿// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using NUnit.Framework;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using OpenTK;

namespace osu.Framework.Desktop.Tests.Visual
{
    [TestFixture]
    internal class TestCaseBufferedContainer : TestCaseMasking
    {
        public override string Description => @"Buffered containers containing almost all visual effects.";

        private readonly BufferedContainer buffer;

        public TestCaseBufferedContainer()
        {
            Remove(TestContainer);

            Add(buffer = new BufferedContainer
            {
                RelativeSizeAxes = Axes.Both,
                Children = new[] { TestContainer }
            });
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            buffer.BlurTo(new Vector2(20), 1000).Then().BlurTo(Vector2.Zero, 1000).Loop();
        }
    }
}
