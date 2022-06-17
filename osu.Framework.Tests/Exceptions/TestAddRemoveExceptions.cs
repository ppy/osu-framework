// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using NUnit.Framework;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;

namespace osu.Framework.Tests.Exceptions
{
    [TestFixture]
    public class TestAddRemoveExceptions
    {
        [Test]
        public void TestNonStableComparer()
        {
            Assert.Throws<InvalidOperationException>(() =>
            {
                using (var broken = new BrokenFillFlowContainer())
                {
                    var candidate = new Box();
                    var candidate2 = new Box();

                    broken.Add(candidate);
                    broken.Add(candidate2);

                    broken.Remove(candidate);
                }
            });
        }

        internal class BrokenFillFlowContainer : FillFlowContainer
        {
            protected override int Compare(Drawable x, Drawable y) => 0;
        }
    }
}
