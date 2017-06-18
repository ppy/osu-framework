// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Testing;

namespace osu.Framework.VisualTests.Tests
{
    public class TestCaseContainerConsistency : TestCase
    {
        public override string Description => "Making sure a container's internal state is consistent with async loads.";

        public override void Reset()
        {
            base.Reset();

            AddStep("Removal of not-loaded drawable", testRemovalOfNotLoaded);
        }

        private void testRemovalOfNotLoaded()
        {
            Clear();

            var container = new Container();
            var sprite = new Sprite();

            container.Add(sprite);
            container.Remove(sprite);
        }
    }
}