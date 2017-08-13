// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using NUnit.Framework;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Testing;

namespace osu.Framework.Desktop.Tests.Visual
{
    [TestFixture]
    public class TestCaseNonAsyncContainer : TestCase
    {
        public override string Description => "Making sure a container's internal state is consistent prior to async loads.";

        private readonly Container container;

        public TestCaseNonAsyncContainer()
        {
            Add(container = new Container());
            testRemoval();
            testReAddingDrawable();
            testMultipleAdds();
        }

        /// <summary>
        /// Tests if a drawable can be removed from a container that is not loaded.
        /// </summary>
        private void testRemoval()
        {
            AddStep("Removal", () =>
            {
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
                var sprite = new Sprite();

                container.Add(sprite);
                container.Remove(sprite);
                container.Add(sprite);
            });
        }

        private void testMultipleAdds()
        {
            AddAssert("Adding container to multiple conatiners", () =>
            {
                try
                {
                    Add(new Container
                    {
                        // Container is an IReadOnlyList<T>, so Children can accept a Container.
                        // This further means that CompositeDrawable.AddInternal will try to add all of
                        // the children of the Container that was set to Children, which should throw an exception
                        Children = new Container { Child = new Container() }
                    });

                    return false;
                }
                catch (InvalidOperationException)
                {
                    return true;
                }
            });
        }
    }
}