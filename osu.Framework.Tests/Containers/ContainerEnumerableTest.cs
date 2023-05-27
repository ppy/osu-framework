// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Collections.Generic;
using NUnit.Framework;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;

namespace osu.Framework.Tests.Containers
{
    [TestFixture]
    public class ContainerEnumerableTest
    {
        /// <summary>
        /// Ensures that adding container as an enumerable of drawables to another container results in an exception.
        /// Tests with a regular <see cref="Container{T}"/>, and an <see cref="AudioContainer{T}"/> which doesn't directly inherit from <see cref="Container{T}"/>.
        /// </summary>
        [TestCase(typeof(Container))]
        [TestCase(typeof(Container<Drawable>))]
        [TestCase(typeof(Container<Box>))]
        [TestCase(typeof(AudioContainer))]
        [TestCase(typeof(AudioContainer<Drawable>))]
        [TestCase(typeof(AudioContainer<Box>))]
        public void TestAddingContainerAsEnumerableRangeThrows(Type containerType)
        {
            Assert.Throws<InvalidOperationException>(() =>
            {
                var unused = new Container
                {
                    Children = (IReadOnlyList<Drawable>)Activator.CreateInstance(containerType)
                };
            });

            Assert.Throws<InvalidOperationException>(() =>
            {
                var unused = new Container();

                unused.AddRange((IEnumerable<Drawable>)Activator.CreateInstance(containerType));
            });

            Assert.Throws<InvalidOperationException>(() =>
            {
                var unused = new AudioContainer
                {
                    Children = (IReadOnlyList<Drawable>)Activator.CreateInstance(containerType)!
                };
            });

            Assert.Throws<InvalidOperationException>(() =>
            {
                var unused = new AudioContainer();

                unused.AddRange((IEnumerable<Drawable>)Activator.CreateInstance(containerType)!);
            });
        }
    }
}
