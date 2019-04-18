// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Testing;

namespace osu.Framework.Tests.Visual.UserInterface
{
    public class TestCaseRearrangeableTextList : ManualInputManagerTestCase
    {
        private RearrangableTextList list;

        public override IReadOnlyList<Type> RequiredTypes => new[]
        {
            typeof(RearrangableTextList),
            typeof(RearrangeableTextLabel),
        };

        public TestCaseRearrangeableTextList()
        {
            Add(list = new RearrangableTextList
            {
                Width = 0.25f,
            });
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            for (int i = 0; i < 10; i++)
                list.AddItem($"test {i}");
        }

        [SetUp]
        public override void SetUp()
        {
        }

        [Test]
        public void SortingTests()
        {
        }

        [Test]
        public void AddRemoveTests()
        {
        }
    }
}
