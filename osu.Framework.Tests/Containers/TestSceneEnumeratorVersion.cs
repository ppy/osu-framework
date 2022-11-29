// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using NUnit.Framework;
using osu.Framework.Graphics.Containers;
using osu.Framework.Testing;
using osu.Framework.Tests.Visual;

namespace osu.Framework.Tests.Containers
{
    [HeadlessTest]
    public partial class TestSceneEnumeratorVersion : FrameworkTestScene
    {
        private Container parent = null!;

        [SetUp]
        public void SetUp() => Schedule(() =>
        {
            Child = parent = new Container
            {
                Child = new Container()
            };
        });

        [Test]
        public void TestEnumeratingNormally()
        {
            AddStep("iterate through parent doing nothing", () => Assert.DoesNotThrow(() =>
            {
                foreach (var _ in parent)
                {
                }
            }));
        }

        [Test]
        public void TestAddChildDuringEnumerationFails()
        {
            AddStep("adding child during enumeration fails", () => Assert.Throws<InvalidOperationException>(() =>
            {
                foreach (var _ in parent)
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
                foreach (var _ in parent)
                {
                    parent.Clear();
                }
            }));
        }
    }
}
