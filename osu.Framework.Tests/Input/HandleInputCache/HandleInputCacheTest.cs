// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using NUnit.Framework;
using osu.Framework.Graphics;
using osu.Framework.Input.Events;

namespace osu.Framework.Tests.Input.HandleInputCache
{
    public partial class HandleInputCacheTest
    {
        [Test]
        public void TestNonPartialLeafClass()
        {
            var d = new NonPartialLeafClass();

            Assert.That(Drawable.HandleInputCache.RequestsPositionalInput(d));
            Assert.That(Drawable.HandleInputCache.RequestsNonPositionalInput(d));
        }

        [Test]
        public void TestPartialLeafClass()
        {
            var d = new PartialLeafClass();

            Assert.That(Drawable.HandleInputCache.RequestsPositionalInput(d));
            Assert.That(Drawable.HandleInputCache.RequestsNonPositionalInput(d));
        }

        [Test]
        public void TestPartialLeafClassWithNonPartialIntermediateClass()
        {
            var d = new PartialLeafClassWithIntermediateNonPartial();

            Assert.That(Drawable.HandleInputCache.RequestsPositionalInput(d));
            Assert.That(Drawable.HandleInputCache.RequestsNonPositionalInput(d));
        }

#pragma warning disable OFSG001
        private sealed class NonPartialLeafClass : Drawable
        {
            protected override bool Handle(UIEvent e) => true;
        }
#pragma warning restore OFSG001

        private sealed partial class PartialLeafClass : Drawable
        {
            protected override bool Handle(UIEvent e) => true;
        }

#pragma warning disable OFSG001
        private class IntermediateNonPartialClass : Drawable
        {
            protected override bool Handle(UIEvent e) => true;
        }
#pragma warning restore OFSG001

        private sealed partial class PartialLeafClassWithIntermediateNonPartial : IntermediateNonPartialClass
        {
        }
    }
}
