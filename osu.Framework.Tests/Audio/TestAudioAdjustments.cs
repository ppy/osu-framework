// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Runtime.CompilerServices;
using NUnit.Framework;
using osu.Framework.Audio;
using osu.Framework.Bindables;

namespace osu.Framework.Tests.Audio
{
    [TestFixture]
    public class TestAudioAdjustments
    {
        [Test]
        public void TestAddAdjustment([Values] AdjustableProperty type)
        {
            var adjustments = new AudioAdjustments();

            Assert.IsTrue(adjustments.GetAggregate(type).IsDefault);

            adjustments.AddAdjustment(type, new BindableDouble(0.5));

            Assert.IsFalse(adjustments.GetAggregate(type).IsDefault);
        }

        [Test]
        public void TestRemoveAdjustment([Values] AdjustableProperty type)
        {
            var adjustments = new AudioAdjustments();

            Assert.IsTrue(adjustments.GetAggregate(type).IsDefault);

            var adjustment = new BindableDouble(0.5);

            adjustments.AddAdjustment(type, adjustment);
            adjustments.RemoveAdjustment(type, adjustment);

            Assert.IsTrue(adjustments.GetAggregate(type).IsDefault);
        }

        [Test]
        public void TestRemoveAllAdjustmentsRemovesExternalBindings([Values] AdjustableProperty type)
        {
            var adjustments = new AudioAdjustments();

            Assert.IsTrue(adjustments.GetAggregate(type).IsDefault);

            adjustments.AddAdjustment(type, new BindableDouble(0.5));
            adjustments.AddAdjustment(type, new BindableDouble(0.5));

            adjustments.RemoveAllAdjustments(type);

            Assert.IsTrue(adjustments.GetAggregate(type).IsDefault);
        }

        [Test]
        public void TestRemoveAllAdjustmentsPreservesInternalBinding()
        {
            var adjustments = new AudioAdjustments();

            adjustments.RemoveAllAdjustments(AdjustableProperty.Volume);

            adjustments.Volume.Value = 0.5;

            Assert.AreEqual(0.5, adjustments.GetAggregate(AdjustableProperty.Volume).Value);
        }

        [TestCase(AdjustableProperty.Balance)]
        public void TestAdditiveComponent(AdjustableProperty type)
        {
            var adjustments = new AudioAdjustments();

            adjustments.AddAdjustment(type, new BindableDouble(0.5));
            adjustments.AddAdjustment(type, new BindableDouble(0.5));

            Assert.AreEqual(1, adjustments.GetAggregate(type).Value);
        }

        [TestCase(AdjustableProperty.Volume)]
        [TestCase(AdjustableProperty.Frequency)]
        [TestCase(AdjustableProperty.Tempo)]
        public void TestMultiplicativeComponent(AdjustableProperty type)
        {
            var adjustments = new AudioAdjustments();

            adjustments.AddAdjustment(type, new BindableDouble(0.5));
            adjustments.AddAdjustment(type, new BindableDouble(0.5));

            Assert.AreEqual(0.25, adjustments.GetAggregate(type).Value);
        }

        [Test]
        public void TestAdjustLocalBindable()
        {
            var adjustments = new AudioAdjustments();

            Assert.AreEqual(1.0, adjustments.Volume.Value);
            Assert.AreEqual(1.0, adjustments.AggregateVolume.Value);

            adjustments.Volume.Value = 0.5d;

            Assert.AreEqual(0.5, adjustments.Volume.Value);
            Assert.AreEqual(0.5, adjustments.AggregateVolume.Value);
        }

        [Test]
        public void TestAdjustBoundBindable()
        {
            var adjustments = new AudioAdjustments();
            var volumeAdjustment = new BindableDouble(0.5);

            adjustments.AddAdjustment(AdjustableProperty.Volume, volumeAdjustment);

            Assert.AreEqual(1.0, adjustments.Volume.Value);
            Assert.AreEqual(0.5, adjustments.AggregateVolume.Value);

            volumeAdjustment.Value = 0.25;

            Assert.AreEqual(1.0, adjustments.Volume.Value);
            Assert.AreEqual(0.25, adjustments.AggregateVolume.Value);
        }

        [Test]
        public void TestAdjustBoundComponentBeforeBind()
        {
            var adjustments = new AudioAdjustments();

            var adjustments2 = new AudioAdjustments
            {
                Volume =
                {
                    Value = 0.5f
                }
            };

            adjustments.BindAdjustments(adjustments2);

            Assert.AreEqual(0.5, adjustments.AggregateVolume.Value);
        }

        [Test]
        public void TestAdjustBoundComponentAfterBind()
        {
            var adjustments = new AudioAdjustments();
            var adjustments2 = new AudioAdjustments();

            adjustments.BindAdjustments(adjustments2);

            adjustments2.Volume.Value = 0.5f;

            Assert.AreEqual(0.5, adjustments.AggregateVolume.Value);
        }

        [Test]
        public void TestValueRestoredAfterComponentUnind()
        {
            var adjustments = new AudioAdjustments();
            var adjustments2 = new AudioAdjustments();

            adjustments.BindAdjustments(adjustments2);

            adjustments2.Volume.Value = 0.5f;

            adjustments.UnbindAdjustments(adjustments2);

            Assert.AreEqual(1.0, adjustments.AggregateVolume.Value);
        }

        [Test]
        public void TestBoundComponentScopeLost()
        {
            var adjustments = new AudioAdjustments();

            bindWithoutScope(adjustments);

            GC.Collect();

            adjustments.Volume.Value = 0.5;

            // would be 0.25 if the weak reference wasn't working.
            Assert.AreEqual(0.5, adjustments.AggregateVolume.Value);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void bindWithoutScope(AudioAdjustments adjustments)
        {
            adjustments.BindAdjustments(new AudioAdjustments { Volume = { Value = 0.5 } });
        }

        [Test]
        public void TestInterfaceMappings()
        {
            var adjustments = new AudioAdjustments();

            Assert.AreEqual(adjustments.AggregateVolume, adjustments.GetAggregate(AdjustableProperty.Volume));
            Assert.AreEqual(adjustments.AggregateBalance, adjustments.GetAggregate(AdjustableProperty.Balance));
            Assert.AreEqual(adjustments.AggregateFrequency, adjustments.GetAggregate(AdjustableProperty.Frequency));
            Assert.AreEqual(adjustments.AggregateTempo, adjustments.GetAggregate(AdjustableProperty.Tempo));
        }
    }
}
