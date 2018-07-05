// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using NUnit.Framework;
using osu.Framework.Configuration;

namespace osu.Framework.Tests.Bindables
{
    [TestFixture]
    public class BindableBindingTest
    {
        [Test]
        public void TestPropagation()
        {
            Bindable<string> bindable1 = new Bindable<string>("default");
            Bindable<string> bindable2 = bindable1.GetBoundCopy();

            Assert.AreEqual("default", bindable1.Value);
            Assert.AreEqual(bindable2.Value, bindable1.Value);

            bindable1.Value = "new value";

            Assert.AreEqual("new value", bindable1.Value);
            Assert.AreEqual(bindable2.Value, bindable1.Value);
        }

        [Test]
        public void TestDisabled()
        {
            Bindable<string> bindable1 = new Bindable<string>("default");
            Bindable<string> bindable2 = bindable1.GetBoundCopy();

            bindable1.Disabled = true;

            Assert.Throws<InvalidOperationException>(() => bindable1.Value = "new value");
            Assert.Throws<InvalidOperationException>(() => bindable2.Value = "new value");

            bindable1.Disabled = false;

            bindable1.Value = "new value";

            Assert.AreEqual("new value", bindable1.Value);
            Assert.AreEqual("new value", bindable2.Value);

            bindable2.Value = "new value 2";

            Assert.AreEqual("new value 2", bindable1.Value);
            Assert.AreEqual("new value 2", bindable2.Value);
        }

        [Test]
        public void TestValueChanged()
        {
            Bindable<string> bindable1 = new Bindable<string>("default");
            Bindable<string> bindable2 = bindable1.GetBoundCopy();

            int changed1 = 0, changed2 = 0;

            bindable1.ValueChanged += _ => changed1++;
            bindable2.ValueChanged += _ => changed2++;

            bindable1.Value = "new value";

            Assert.AreEqual(1, changed1);
            Assert.AreEqual(1, changed2);

            bindable1.Value = "new value 2";

            Assert.AreEqual(2, changed1);
            Assert.AreEqual(2, changed2);

            // should not re-fire, as the value hasn't changed.
            bindable1.Value = "new value 2";

            Assert.AreEqual(2, changed1);
            Assert.AreEqual(2, changed2);
        }

        [Test]
        public void TestValueChangedWithUpstreamRejection()
        {
            Bindable<string> bindable1 = new Bindable<string>("won't change");
            Bindable<string> bindable2 = bindable1.GetBoundCopy();

            int changed1 = 0, changed2 = 0;

            bindable1.ValueChanged += v => changed1++;
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

            bool disabled1 = false, disabled2 = false;

            bindable1.DisabledChanged += v => disabled1 = v;
            bindable2.DisabledChanged += v => disabled2 = v;

            bindable1.Disabled = true;

            Assert.AreEqual(true, disabled1);
            Assert.AreEqual(true, disabled2);

            bindable1.Disabled = false;

            Assert.AreEqual(false, disabled1);
            Assert.AreEqual(false, disabled2);
        }

        [Test]
        public void TestDisabledChangedWithUpstreamRejection()
        {
            Bindable<string> bindable1 = new Bindable<string>("won't change");
            Bindable<string> bindable2 = bindable1.GetBoundCopy();

            int changed1 = 0, changed2 = 0;

            bindable1.DisabledChanged += v => changed1++;
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
    }
}
