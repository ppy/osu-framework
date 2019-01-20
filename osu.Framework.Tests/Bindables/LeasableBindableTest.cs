// Copyright (c) 2007-2019 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using NUnit.Framework;
using osu.Framework.Configuration;

namespace osu.Framework.Tests.Bindables
{
    [TestFixture]
    public class LeasableBindableTest
    {
        private Bindable<int> original;
        private LeasableBindable<int> leasable;

        [SetUp]
        public void SetUp()
        {
            original = new Bindable<int>(1);
            leasable = new LeasableBindable<int>(original);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void TestLeaseAndReturn(bool revert)
        {
            var leased = leasable.BeginLease(revert);

            Assert.AreEqual(original.Value, leasable.Value);
            Assert.AreEqual(leasable.Value, leased.Value);

            leased.Value = 2;

            Assert.AreEqual(original.Value, leasable.Value);
            Assert.AreEqual(leasable.Value, leased.Value);

            leased.Return();

            Assert.AreEqual(original.Value, revert ? 1 : 2);
        }

        [Test]
        public void TestLeaseReturnLease()
        {
            var leased1 = leasable.BeginLease(false);
            leased1.Return();
            var leased2 = leasable.BeginLease(false);
            leased2.Return();
        }

        [Test]
        public void TestModifyAfterReturnFail()
        {
            var leased1 = leasable.BeginLease(false);
            leased1.Return();

            Assert.Throws<InvalidOperationException>(() => leased1.Value = 2);
            Assert.Throws<InvalidOperationException>(() => leased1.Disabled = true);
            Assert.Throws<InvalidOperationException>(() => leased1.Return());
        }

        [Test]
        public void TestDoubleLeaseFails()
        {
            leasable.BeginLease(false);
            Assert.Throws<InvalidOperationException>(() => leasable.BeginLease(false));
        }

        [Test]
        public void TestIncorrectEndLease()
        {
            // end a lease when no lease exists.
            Assert.Throws<InvalidOperationException>(() => leasable.EndLease(null));

            // end a lease with an incorrect bindable
            leasable.BeginLease(true);
            Assert.Throws<InvalidOperationException>(() => leasable.EndLease(original));
        }

        [Test]
        public void TestDisabledStateDuringLease()
        {
            Assert.IsFalse(leasable.Disabled);

            var leased = leasable.BeginLease(true);

            Assert.IsTrue(leasable.Disabled);
            Assert.IsTrue(original.Disabled);
            // during lease, the leased bindable is also set to a disabled state (but is always bypassed when setting the value via it directly).
            Assert.IsTrue(leased.Disabled);

            // you can't change the disabled state of the leasable during a lease...
            Assert.Throws<InvalidOperationException>(() => leasable.Disabled = false);

            // ... but you can change it from the leased instance, allowing modification of the original during lease.
            leased.Disabled = false;
            Assert.IsFalse(leased.Disabled);
            Assert.IsFalse(original.Disabled);

            original.Value = 2;
            leased.Disabled = true;

            // ... as you can from the original (encapsulated) bindable.
            original.Disabled = false;
            Assert.IsFalse(leased.Disabled);
            Assert.IsFalse(original.Disabled);

            original.Value = 3;
            leased.Disabled = true;

            Assert.IsTrue(original.Disabled);
            Assert.IsTrue(leased.Disabled);

            leased.Return();

            Assert.IsFalse(original.Disabled);
            Assert.IsFalse(leasable.Disabled);
        }

        [Test]
        public void TestDisabledViaBindings()
        {
            var leased = leasable.BeginLease(true);

            // ensure we can't change leasable's disabled via a bound bindable.
            var bound = leasable.GetBoundCopy();
            Assert.Throws<InvalidOperationException>(() => bound.Disabled = false);
        }
    }
}
