// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Testing;

namespace osu.Framework.VisualTests.Tests
{
    public class TestCaseNonAsyncContainer : TestCase
    {
        public override string Description => "Making sure a container's internal state is consistent prior to async loads.";

        public override void Reset()
        {
            base.Reset();

            testRemoval();
            testReAddingDrawable();
        }

        /// <summary>
        /// Tests if a drawable can be removed from a container that is not loaded.
        /// </summary>
        private void testRemoval()
        {
            AddStep("Removal", () =>
            {
                var container = new Container();
                var sprite = new Sprite();

                container.Add(sprite);
                container.Remove(sprite);
            });
        }

        /// <summary>
        /// Tests if a drawable that has been removed from a container that is not loaded can be re-added to it.
        /// </summary>
        private void testReAddingDrawable()
        {
            AddStep("Re-adding to same container", () =>
            {
                var container = new Container();
                var sprite = new Sprite();

                container.Add(sprite);
                container.Remove(sprite);
                container.Add(sprite);
            });
        }
    }
}