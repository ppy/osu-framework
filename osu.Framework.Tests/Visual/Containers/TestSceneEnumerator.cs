// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using NUnit.Framework;
using osu.Framework.Graphics.Containers;

namespace osu.Framework.Tests.Visual.Containers
{
    public class TestSceneEnumerator : FrameworkTestScene
    {
        private Container parent;

        [SetUp]
        public void SetUp()
        {
            Child = parent = new Container
            {
                Child = new Container
                {
                }
            };
        }

        [Test]
        public void TestEnumeratingNormally()
        {
            AddStep("iterate through parent doing nothing", () => Assert.DoesNotThrow(() =>
            {
                foreach (var child in parent)
                {
                }
            }));
        }

        [Test]
        public void TestAddChildDuringEnumerationFails()
        {
            AddStep("adding child during enumeration fails", () => Assert.Throws<InvalidOperationException>(() =>
            {
                foreach (var child in parent)
                {
                    parent.Add(new Container());
                }
            }));
        }

        [Test]
        public void TestRemoveChildDuringEnumerationFails()
        {
            AddStep("removing child during enumeration fails", () => Assert.Throws<InvalidOperationException>(() =>
            {
                foreach (var child in parent)
                {
                    parent.Remove(child, true);
                }
            }));
        }

        [Test]
        public void TestClearDuringEnumerationFails()
        {
            AddStep("clearing children during enumeration fails", () => Assert.Throws<InvalidOperationException>(() =>
            {
                foreach (var child in parent)
                {
                    parent.Clear();
                }
            }));
        }
    }
}
