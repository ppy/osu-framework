// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using NUnit.Framework;
using osu.Framework.Bindables;

namespace osu.Framework.Tests.Bindables
{
    [TestFixture]
    public class BindableLeasingTest
    {
        private Bindable<int> original;

        [SetUp]
        public void SetUp()
        {
            original = new Bindable<int>(1);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void TestLeaseAndReturn(bool revert)
        {
            var leased = original.BeginLease(revert);

            Assert.AreEqual(original.Value, leased.Value);

            leased.Value = 2;

            Assert.AreEqual(original.Value, 2);
            Assert.AreEqual(original.Value, leased.Value);

            Assert.AreEqual(true, leased.Return());

            Assert.AreEqual(original.Value, revert ? 1 : 2);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void TestLeaseReturnedOnUnbindAll(bool revert)
        {
            var leased = original.BeginLease(revert);

            Assert.AreEqual(original.Value, leased.Value);

            leased.Value = 2;

            Assert.AreEqual(original.Value, 2);
            Assert.AreEqual(original.Value, leased.Value);

            original.UnbindAll();

            Assert.AreEqual(original.Value, revert ? 1 : 2);
        }

        [Test]
        public void TestConsecutiveLeases()
        {
            var leased1 = original.BeginLease(false);
            Assert.AreEqual(true, leased1.Return());
            var leased2 = original.BeginLease(false);
            Assert.AreEqual(true, leased2.Return());
        }

        [Test]
        public void TestModifyAfterReturnFail()
        {
            var leased = original.BeginLease(false);
            Assert.AreEqual(true, leased.Return());

            Assert.Throws<InvalidOperationException>(() => leased.Value = 2);
            Assert.Throws<InvalidOperationException>(() => leased.Disabled = true);
        }

        [Test]
        public void TestDoubleReturnSilentlyNoops()
        {
            var leased = original.BeginLease(false);

            Assert.AreEqual(true, leased.Return());
            Assert.AreEqual(false, leased.Return());
        }

        [Test]
        public void TestDoubleLeaseFails()
        {
            original.BeginLease(false);
            Assert.Throws<InvalidOperationException>(() => original.BeginLease(false));
        }

        [Test]
        public void TestIncorrectEndLease()
        {
            // end a lease when no lease exists.
            Assert.Throws<InvalidOperationException>(() => original.EndLease(null));

            // end a lease with an incorrect bindable
            original.BeginLease(true);
            Assert.Throws<InvalidOperationException>(() => original.EndLease(new Bindable<int>().BeginLease(true)));
        }

        [Test]
        public void TestDisabledStateDuringLease()
        {
            Assert.IsFalse(original.Disabled);

            var leased = original.BeginLease(true);

            Assert.IsTrue(original.Disabled);
            Assert.IsTrue(leased.Disabled); // during lease, the leased bindable is also set to a disabled state (but is always bypassed when setting the value via it directly).

            // you can't change the disabled state of the original during a lease...
            Assert.Throws<InvalidOperationException>(() => original.Disabled = false);

            // ..but you can change it from the leased instance..
            leased.Disabled = false;

            Assert.IsFalse(leased.Disabled);
            Assert.IsFalse(original.Disabled);

            // ..allowing modification of the original during lease.
            original.Value = 2;

            // even if not disabled, you still cannot change disabled from the original during a lease.
            Assert.Throws<InvalidOperationException>(() => original.Disabled = true);
            Assert.IsFalse(original.Disabled);
            Assert.IsFalse(leased.Disabled);

            // you must use the leased instance.
            leased.Disabled = true;

            Assert.IsTrue(original.Disabled);
            Assert.IsTrue(leased.Disabled);

            Assert.AreEqual(true, leased.Return());

            Assert.IsFalse(original.Disabled);
        }

        [Test]
        public void TestDisabledChangeViaBindings()
        {
            original.BeginLease(true);

            // ensure we can't change original's disabled via a bound bindable.
            var bound = original.GetBoundCopy();

            Assert.Throws<InvalidOperationException>(() => bound.Disabled = false);
            Assert.IsTrue(original.Disabled);
        }

        [Test]
        public void TestDisabledChangeViaBindingToLeased()
        {
            bool? changedState = null;
            original.DisabledChanged += d => changedState = d;

            var leased = original.BeginLease(true);

            var bound = leased.GetBoundCopy();

            bound.Disabled = false;

            Assert.AreEqual(changedState, false);
            Assert.IsFalse(original.Disabled);
        }

        [Test]
        public void TestValueChangeViaBindings()
        {
            original.BeginLease(true);

            // ensure we can't change original's disabled via a bound bindable.
            var bound = original.GetBoundCopy();

            Assert.Throws<InvalidOperationException>(() => bound.Value = 2);
            Assert.AreEqual(original.Value, 1);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void TestDisabledRevertedAfterLease(bool revert)
        {
            bool? changedState = null;

            original.Disabled = true;
            original.DisabledChanged += d => changedState = d;

            var leased = original.BeginLease(revert);

            Assert.AreEqual(true, leased.Return());

            // regardless of revert specification, disabled should always be reverted to the original value.
            Assert.IsTrue(original.Disabled);
            Assert.IsFalse(changedState.HasValue);
        }

        [Test]
        public void TestLeaseFromBoundBindable()
        {
            var copy = original.GetBoundCopy();

            var leased = copy.BeginLease(true);

            // can't take a second lease from the original.
            Assert.Throws<InvalidOperationException>(() => original.BeginLease(false));

            // can't take a second lease from the copy.
            Assert.Throws<InvalidOperationException>(() => copy.BeginLease(false));

            leased.Value = 2;

            // value propagates everywhere
            Assert.AreEqual(original.Value, 2);
            Assert.AreEqual(original.Value, copy.Value);
            Assert.AreEqual(original.Value, leased.Value);

            // bound copies of the lease still allow setting value / disabled.
            var leasedCopy = leased.GetBoundCopy();

            leasedCopy.Value = 3;

            Assert.AreEqual(original.Value, 3);
            Assert.AreEqual(original.Value, copy.Value);
            Assert.AreEqual(original.Value, leased.Value);
            Assert.AreEqual(original.Value, leasedCopy.Value);

            leasedCopy.Disabled = false;
            leasedCopy.Disabled = true;

            Assert.AreEqual(true, leased.Return());

            original.Value = 1;

            Assert.AreEqual(original.Value, 1);
            Assert.AreEqual(original.Value, copy.Value);
            Assert.IsFalse(original.Disabled);
        }

        [Test]
        public void TestCantLeaseFromLease()
        {
            var leased = original.BeginLease(false);
            Assert.Throws<InvalidOperationException>(() => leased.BeginLease(false));
        }

        [Test]
        public void TestCantLeaseFromBindingChain()
        {
            var bound1 = original.GetBoundCopy();
            var bound2 = bound1.GetBoundCopy();

            original.BeginLease(false);
            Assert.Throws<InvalidOperationException>(() => bound2.BeginLease(false));
        }

        [Test]
        public void TestReturnFromBoundCopyOfLeaseFails()
        {
            var leased = original.BeginLease(true);

            var copy = leased.GetBoundCopy();

            Assert.Throws<InvalidOperationException>(() => ((LeasedBindable<int>)copy).Return());
        }

        [Test]
        public void TestUnbindAllReturnsLease()
        {
            var leased = original.BeginLease(true);
            leased.UnbindAll();
            leased.UnbindAll();
        }

        [Test]
        public void TestLeasedBoundToMultiple()
        {
            var leased = original.BeginLease(false);

            var another = new Bindable<int>();
            leased.BindTo(another);
            another.Value = 3;
            Assert.AreEqual(another.Value, 3);
            Assert.AreEqual(another.Value, leased.Value);

            leased.Value = 4;
            Assert.AreEqual(original.Value, 4);
            Assert.AreEqual(another.Value, 4);
            Assert.AreEqual(original.Value, leased.Value);
        }
    }
}
