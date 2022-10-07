// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Timing;

namespace osu.Framework.Tests.Bindables
{
    [TestFixture]
    public class BindableBindingTest
    {
        [Test]
        public void TestBindToAlreadyBound()
        {
            Bindable<string> bindable1 = new Bindable<string>("default");
            Bindable<string> bindable2 = bindable1.GetBoundCopy();

            Assert.Throws<InvalidOperationException>(() => bindable1.BindTo(bindable2));
        }

        [Test]
        public void TestPropagation()
        {
            Bindable<string> bindable1 = new Bindable<string>("default");
            Bindable<string> bindable2 = bindable1.GetBoundCopy();
            Bindable<string> bindable3 = bindable2.GetBoundCopy();

            Assert.AreEqual("default", bindable1.Value);
            Assert.AreEqual(bindable2.Value, bindable1.Value);
            Assert.AreEqual(bindable3.Value, bindable1.Value);

            bindable1.Value = "new value";

            Assert.AreEqual("new value", bindable1.Value);
            Assert.AreEqual(bindable2.Value, bindable1.Value);
            Assert.AreEqual(bindable3.Value, bindable1.Value);
        }

        [Test]
        public void TestDisabled()
        {
            Bindable<string> bindable1 = new Bindable<string>("default");
            Bindable<string> bindable2 = bindable1.GetBoundCopy();
            Bindable<string> bindable3 = bindable2.GetBoundCopy();

            bindable1.Disabled = true;

            Assert.Throws<InvalidOperationException>(() => bindable1.Value = "new value");
            Assert.Throws<InvalidOperationException>(() => bindable2.Value = "new value");
            Assert.Throws<InvalidOperationException>(() => bindable3.Value = "new value");

            bindable1.Disabled = false;

            bindable1.Value = "new value";

            Assert.AreEqual("new value", bindable1.Value);
            Assert.AreEqual("new value", bindable2.Value);
            Assert.AreEqual("new value", bindable3.Value);

            bindable2.Value = "new value 2";

            Assert.AreEqual("new value 2", bindable1.Value);
            Assert.AreEqual("new value 2", bindable2.Value);
            Assert.AreEqual("new value 2", bindable3.Value);
        }

        [Test]
        public void TestDefaultChanged()
        {
            Bindable<string> bindable1 = new Bindable<string>("default");
            Bindable<string> bindable2 = bindable1.GetBoundCopy();
            Bindable<string> bindable3 = bindable2.GetBoundCopy();

            int changed1 = 0, changed2 = 0, changed3 = 0;

            bindable1.DefaultChanged += _ => changed1++;
            bindable2.DefaultChanged += _ => changed2++;
            bindable3.DefaultChanged += _ => changed3++;

            bindable1.Default = "new value";

            Assert.AreEqual(1, changed1);
            Assert.AreEqual(1, changed2);
            Assert.AreEqual(1, changed3);

            bindable1.Default = "new value 2";

            Assert.AreEqual(2, changed1);
            Assert.AreEqual(2, changed2);
            Assert.AreEqual(2, changed3);

            // should not re-fire, as the value hasn't changed.
            bindable1.Default = "new value 2";

            Assert.AreEqual(2, changed1);
            Assert.AreEqual(2, changed2);
            Assert.AreEqual(2, changed3);
        }

        [Test]
        public void TestDefaultChangedWithUpstreamRejection()
        {
            Bindable<string> bindable1 = new Bindable<string>("won't change");
            Bindable<string> bindable2 = bindable1.GetBoundCopy();

            int changed1 = 0, changed2 = 0;

            bindable1.DefaultChanged += _ => changed1++;
            bindable2.DefaultChanged += _ =>
            {
                bindable2.Default = "won't change";
                changed2++;
            };

            bindable1.Default = "new value";

            Assert.AreEqual("won't change", bindable1.Default);
            Assert.AreEqual(bindable1.Default, bindable2.Default);

            // bindable1 should only receive the final value changed, skipping the intermediary (overidden) one.
            Assert.AreEqual(1, changed1);
            Assert.AreEqual(2, changed2);
        }

        [Test]
        public void TestValueChanged()
        {
            Bindable<string> bindable1 = new Bindable<string>("default");
            Bindable<string> bindable2 = bindable1.GetBoundCopy();
            Bindable<string> bindable3 = bindable2.GetBoundCopy();

            int changed1 = 0, changed2 = 0, changed3 = 0;

            bindable1.ValueChanged += _ => changed1++;
            bindable2.ValueChanged += _ => changed2++;
            bindable3.ValueChanged += _ => changed3++;

            bindable1.Value = "new value";

            Assert.AreEqual(1, changed1);
            Assert.AreEqual(1, changed2);
            Assert.AreEqual(1, changed3);

            bindable1.Value = "new value 2";

            Assert.AreEqual(2, changed1);
            Assert.AreEqual(2, changed2);
            Assert.AreEqual(2, changed3);

            // should not re-fire, as the value hasn't changed.
            bindable1.Value = "new value 2";

            Assert.AreEqual(2, changed1);
            Assert.AreEqual(2, changed2);
            Assert.AreEqual(2, changed3);
        }

        [Test]
        public void TestValueChangedWithUpstreamRejection()
        {
            Bindable<string> bindable1 = new Bindable<string>("won't change");
            Bindable<string> bindable2 = bindable1.GetBoundCopy();

            int changed1 = 0, changed2 = 0;

            bindable1.ValueChanged += _ => changed1++;
            bindable2.ValueChanged += _ =>
            {
                bindable2.Value = "won't change";
                changed2++;
            };

            bindable1.Value = "new value";

            Assert.AreEqual("won't change", bindable1.Value);
            Assert.AreEqual(bindable1.Value, bindable2.Value);

            // bindable1 should only receive the final value changed, skipping the intermediary (overidden) one.
            Assert.AreEqual(1, changed1);
            Assert.AreEqual(2, changed2);
        }

        [Test]
        public void TestDisabledChanged()
        {
            Bindable<string> bindable1 = new Bindable<string>("default");
            Bindable<string> bindable2 = bindable1.GetBoundCopy();
            Bindable<string> bindable3 = bindable2.GetBoundCopy();

            bool disabled1 = false, disabled2 = false, disabled3 = false;

            bindable1.DisabledChanged += v => disabled1 = v;
            bindable2.DisabledChanged += v => disabled2 = v;
            bindable3.DisabledChanged += v => disabled3 = v;

            bindable1.Disabled = true;

            Assert.AreEqual(true, disabled1);
            Assert.AreEqual(true, disabled2);
            Assert.AreEqual(true, disabled3);

            bindable1.Disabled = false;

            Assert.AreEqual(false, disabled1);
            Assert.AreEqual(false, disabled2);
            Assert.AreEqual(false, disabled3);
        }

        [Test]
        public void TestDisabledChangedWithUpstreamRejection()
        {
            Bindable<string> bindable1 = new Bindable<string>("won't change");
            Bindable<string> bindable2 = bindable1.GetBoundCopy();

            int changed1 = 0, changed2 = 0;

            bindable1.DisabledChanged += _ => changed1++;
            bindable2.DisabledChanged += _ =>
            {
                bindable2.Disabled = false;
                changed2++;
            };

            bindable1.Disabled = true;

            Assert.IsFalse(bindable1.Disabled);
            Assert.IsFalse(bindable2.Disabled);

            // bindable1 should only receive the final disabled changed, skipping the intermediary (overidden) one.
            Assert.AreEqual(1, changed1);
            Assert.AreEqual(2, changed2);
        }

        [Test]
        public void TestMinValueChanged()
        {
            BindableInt bindable1 = new BindableInt();
            BindableInt bindable2 = new BindableInt();
            bindable2.BindTo(bindable1);

            int minValue1 = 0, minValue2 = 0;

            bindable1.MinValueChanged += v => minValue1 = v;
            bindable2.MinValueChanged += v => minValue2 = v;

            bindable1.MinValue = 1;

            Assert.AreEqual(1, minValue1);
            Assert.AreEqual(1, minValue2);

            bindable1.MinValue = 2;

            Assert.AreEqual(2, minValue1);
            Assert.AreEqual(2, minValue2);
        }

        [Test]
        public void TestMinValueChangedWithUpstreamRejection()
        {
            BindableInt bindable1 = new BindableInt(1337); // Won't change
            BindableInt bindable2 = new BindableInt();
            bindable2.BindTo(bindable1);

            int changed1 = 0, changed2 = 0;

            bindable1.MinValueChanged += _ => changed1++;
            bindable2.MinValueChanged += _ =>
            {
                bindable2.MinValue = 1337;
                changed2++;
            };

            bindable1.MinValue = 2;

            Assert.AreEqual(1337, bindable1.MinValue);
            Assert.AreEqual(bindable1.MinValue, bindable2.MinValue);

            // bindable1 should only receive the final value changed, skipping the intermediary (overidden) one.
            Assert.AreEqual(1, changed1);
            Assert.AreEqual(2, changed2);
        }

        [Test]
        public void TestMaxValueChanged()
        {
            BindableInt bindable1 = new BindableInt();
            BindableInt bindable2 = new BindableInt();
            bindable2.BindTo(bindable1);

            int minValue1 = 0, minValue2 = 0;

            bindable1.MaxValueChanged += v => minValue1 = v;
            bindable2.MaxValueChanged += v => minValue2 = v;

            bindable1.MaxValue = 1;

            Assert.AreEqual(1, minValue1);
            Assert.AreEqual(1, minValue2);

            bindable1.MaxValue = 2;

            Assert.AreEqual(2, minValue1);
            Assert.AreEqual(2, minValue2);
        }

        [Test]
        public void TestMaxValueChangedWithUpstreamRejection()
        {
            BindableInt bindable1 = new BindableInt(1337); // Won't change
            BindableInt bindable2 = new BindableInt();
            bindable2.BindTo(bindable1);

            int changed1 = 0, changed2 = 0;

            bindable1.MaxValueChanged += _ => changed1++;
            bindable2.MaxValueChanged += _ =>
            {
                bindable2.MaxValue = 1337;
                changed2++;
            };

            bindable1.MaxValue = 2;

            Assert.AreEqual(1337, bindable1.MaxValue);
            Assert.AreEqual(bindable1.MaxValue, bindable2.MaxValue);

            // bindable1 should only receive the final value changed, skipping the intermediary (overidden) one.
            Assert.AreEqual(1, changed1);
            Assert.AreEqual(2, changed2);
        }

        [Test]
        public void TestUnbindOnDrawableDispose()
        {
            var drawable = new TestDrawable();

            drawable.SetValue(1);
            Assert.IsTrue(drawable.ValueChanged, "bound correctly");

            drawable.Dispose();
            drawable.ValueChanged = false;

            drawable.SetValue(2);
            Assert.IsFalse(drawable.ValueChanged, "unbound correctly");
        }

        [Test]
        public void TestUnbindOnDrawableDisposeSubClass()
        {
            var drawable = new TestSubDrawable();

            drawable.SetValue(1);
            Assert.IsTrue(drawable.ValueChanged, "bound correctly");
            Assert.IsTrue(drawable.ValueChanged2, "bound correctly");

            drawable.Dispose();
            drawable.ValueChanged = false;
            drawable.ValueChanged2 = false;

            drawable.SetValue(2);
            Assert.IsFalse(drawable.ValueChanged, "unbound correctly");
            Assert.IsFalse(drawable.ValueChanged2, "unbound correctly");
        }

        [Test]
        public void TestUnbindOnDrawableDisposeCached()
        {
            // Build cache
            var drawable = new TestDrawable();
            drawable.Dispose();

            TestUnbindOnDrawableDispose();
        }

        [Test]
        public void TestUnbindOnDrawableDoNotDisposeDelegatingProperty()
        {
            var bindable = new Bindable<int>();

            bool valueChanged = false;
            bindable.ValueChanged += _ => valueChanged = true;

            var drawable = new TestDrawable2 { GetBindable = () => bindable };

            drawable.SetValue(1);
            Assert.IsTrue(valueChanged, "bound correctly");

            drawable.Dispose();

            valueChanged = false;
            bindable.Value = 2;
            Assert.IsTrue(valueChanged, "bound correctly");

            valueChanged = false;
            drawable.SetValue(3);
            Assert.IsTrue(valueChanged, "bound correctly");
        }

        [Test]
        public void TestUnbindOnDrawableDisposeAutoProperty()
        {
            bool valueChanged = false;
            var drawable = new TestDrawable3();
            drawable.Bindable.ValueChanged += _ => valueChanged = true;

            drawable.Bindable.Value = 1;
            Assert.IsTrue(valueChanged, "bound correctly");

            drawable.Dispose();
            valueChanged = false;

            drawable.Bindable.Value = 2;
            Assert.IsFalse(valueChanged, "unbound correctly");
        }

        [Test]
        public void TestUnbindOnDrawableDisposePropertyCached()
        {
            // Build cache
            var drawable = new TestDrawable2();
            drawable.Dispose();

            TestUnbindOnDrawableDispose();
        }

        [Test]
        public void TestUnbindFrom()
        {
            var bindable1 = new Bindable<int>(5);
            var bindable2 = new Bindable<int>();
            bindable2.BindTo(bindable1);

            Assert.AreEqual(bindable1.Value, bindable2.Value);

            bindable2.UnbindFrom(bindable1);
            bindable1.Value = 10;

            Assert.AreNotEqual(bindable1.Value, bindable2.Value);
        }

        [Test]
        public void TestUnbindEvents()
        {
            var bindable = new BindableInt
            {
                Value = 0,
                Default = 0,
                MinValue = -5,
                MaxValue = 5,
                Precision = 1,
                Disabled = false
            };

            bool valueChanged = false;
            bool defaultChanged = false;
            bool disabledChanged = false;
            bool minValueChanged = false;
            bool maxValueChanged = false;
            bool precisionChanged = false;

            bindable.ValueChanged += _ => valueChanged = true;
            bindable.DefaultChanged += _ => defaultChanged = true;
            bindable.DisabledChanged += _ => disabledChanged = true;
            bindable.MinValueChanged += _ => minValueChanged = true;
            bindable.MaxValueChanged += _ => maxValueChanged = true;
            bindable.PrecisionChanged += _ => precisionChanged = true;

            bindable.UnbindEvents();

            bindable.Value = 5;
            bindable.Default = 5;
            bindable.MinValue = 0;
            bindable.MaxValue = 10;
            bindable.Precision = 5;
            bindable.Disabled = true;

            Assert.That(!valueChanged && !defaultChanged && !disabledChanged &&
                        !minValueChanged && !maxValueChanged && !precisionChanged);
        }

        [Test]
        public void TestEventArgs()
        {
            var bindable1 = new Bindable<int>();
            var bindable2 = new Bindable<int>();

            bindable2.BindTo(bindable1);

            ValueChangedEvent<int> event1 = null;
            ValueChangedEvent<int> event2 = null;

            bindable1.BindValueChanged(e => event1 = e);
            bindable2.BindValueChanged(e => event2 = e);

            bindable1.Value = 1;

            Assert.AreEqual(0, event1.OldValue);
            Assert.AreEqual(1, event1.NewValue);
            Assert.AreEqual(0, event2.OldValue);
            Assert.AreEqual(1, event2.NewValue);

            bindable1.Value = 2;

            Assert.AreEqual(1, event1.OldValue);
            Assert.AreEqual(2, event1.NewValue);
            Assert.AreEqual(1, event2.OldValue);
            Assert.AreEqual(2, event2.NewValue);
        }

        [Test]
        public void TestCustomUnbindFromCalledFromUnbindAll()
        {
            var bindable1 = new Bindable<int>();
            var bindable2 = new TestCustomBindable();

            bindable2.BindTo(bindable1);
            Assert.That(bindable2.IsBound, Is.True);

            bindable2.UnbindAll();
            Assert.That(bindable2.IsBound, Is.False);
        }

        private class TestDrawable : Drawable
        {
            public bool ValueChanged;

            private readonly Bindable<int> bindable = new Bindable<int>();

            public TestDrawable()
            {
                bindable.BindValueChanged(_ => ValueChanged = true);

                // because we are run outside of a game instance but need the cached disposal methods.
                Load(new FramedClock(), new DependencyContainer());
            }

            public virtual void SetValue(int value) => bindable.Value = value;
        }

        private class TestSubDrawable : TestDrawable
        {
            public bool ValueChanged2;

            private readonly Bindable<int> bindable = new Bindable<int>();

            public TestSubDrawable()
            {
                bindable.BindValueChanged(_ => ValueChanged2 = true);
            }

            public override void SetValue(int value)
            {
                bindable.Value = value;
                base.SetValue(value);
            }
        }

        private class TestDrawable2 : Drawable
        {
            public Func<Bindable<int>> GetBindable;
            private Bindable<int> bindable => GetBindable();

            public TestDrawable2()
            {
                // because we are run outside of a game instance but need the cached disposal methods.
                Load(new FramedClock(), new DependencyContainer());
            }

            public void SetValue(int value) => bindable.Value = value;
        }

        private class TestDrawable3 : Drawable
        {
            public Bindable<int> Bindable { get; } = new Bindable<int>();

            public TestDrawable3()
            {
                // because we are run outside of a game instance but need the cached disposal methods.
                Load(new FramedClock(), new DependencyContainer());
            }
        }

        private class TestCustomBindable : Bindable<int>
        {
            public bool IsBound { get; private set; }

            public override void BindTo(Bindable<int> them)
            {
                base.BindTo(them);
                IsBound = true;
            }

            public override void UnbindFrom(IUnbindable them)
            {
                base.UnbindFrom(them);
                IsBound = false;
            }

            protected override Bindable<int> CreateInstance() => new TestCustomBindable();
        }
    }
}
