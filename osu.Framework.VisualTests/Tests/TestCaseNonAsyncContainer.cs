// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Linq;
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
            testChangingDepth();
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

        /// <summary>
        /// Tests if <see cref="Container{T}.InternalChildren"/> and <see cref="Container{T}.AliveInternalChildren"/> remain consistent
        /// if the depth of a drawable added to it is changed while the container is not loaded.
        /// Additionally, this also tests that changing the Depth after a child is added throws the expected exception.
        /// </summary>
        private void testChangingDepth()
        {
            var container = new DepthConsistencyContainer();
            AddStep("Changing depth", () => LoadComponentAsync(container));
            AddAssert("AliveInternalChildren == InternalChildren", () => container.IsConsistent);
            AddAssert("Not valid operation", () => container.ExceptionThrown);
        }

        private class DepthConsistencyContainer : Container
        {
            public readonly bool ExceptionThrown;
            public bool IsConsistent => AliveInternalChildren.First() == InternalChildren.First();

            public DepthConsistencyContainer()
            {
                var spriteA = new Sprite();
                var spriteB = new Sprite();

                Children = new [] { spriteA, spriteB };

                // If parent is properly set, this will throw an exception, but that's okay because for the purpose of this test because
                // 1) The state should remain consistent regardless of whether the exception is caught or not
                // 2) The user's application would crash if not caught, alerting them of the invalid operation
                try
                {
                    spriteB.Depth = 1;
                }
                catch (InvalidOperationException) { ExceptionThrown = true; }
            }
        }
    }
}