// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Testing;
using osuTK;
using osuTK.Graphics;
using osuTK.Input;

namespace osu.Framework.Tests.Visual.UserInterface
{
    public class TestSceneRearrangeableListContainer : ManualInputManagerTestScene
    {
        public override IReadOnlyList<Type> RequiredTypes => new[]
        {
            typeof(BasicRearrangeableListContainer),
            typeof(BasicRearrangeableItem),
            typeof(RearrangeableListItem),
            typeof(RearrangeableListContainer<>)
        };

        [Test]
        public void TestAddItem()
        {
        }

        [Test]
        public void TestRemoveItem()
        {
        }

        [Test]
        public void TestClearItems()
        {
        }

        [Test]
        public void TestDragDownToRearrange()
        {
        }

        [Test]
        public void TestDragUpToRearrange()
        {
        }

        [Test]
        public void TestScrollWhenDraggedToTop()
        {
        }

        [Test]
        public void TestScrollWhenDraggedToBottom()
        {
        }
    }
}
