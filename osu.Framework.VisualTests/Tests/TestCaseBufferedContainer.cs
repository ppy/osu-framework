// Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Transformations;
using osu.Framework.Input;
using OpenTK;
using OpenTK.Graphics;
using osu.Framework.GameModes.Testing;
using System;

namespace osu.Framework.VisualTests.Tests
{
    class TestCaseBufferedContainer : TestCaseMasking
    {
        public override string Name => @"BufferedContainer";
        public override string Description => @"Various scenarios which potentially challenge buffered containers calculations.";

        public override void Reset()
        {
            base.Reset();

            Remove(TestContainer);

            Add(new BufferedContainer()
            {
                RelativeSizeAxes = Axes.Both,
                Children = new[] { TestContainer }
            });
        }
    }

}
