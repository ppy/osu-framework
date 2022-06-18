// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System.Threading;
using NUnit.Framework;
using osu.Framework.Bindables;

namespace osu.Framework.Tests.Bindables
{
    [TestFixture]
    public class AggregateBindableTest
    {
        [Test]
        public void TestMultiplicationAggregate()
        {
            var aggregate = new AggregateBindable<double>((a, b) => a * b, new Bindable<double>(1));

            Assert.AreEqual(1, aggregate.Result.Value);

            var bindable1 = new BindableDouble(0.5);
            aggregate.AddSource(bindable1);
            Assert.AreEqual(0.5, aggregate.Result.Value);

            var bindable2 = new BindableDouble(0.25);
            aggregate.AddSource(bindable2);
            Assert.AreEqual(0.125, aggregate.Result.Value);
        }

        [Test]
        public void TestResultBounds()
        {
            var aggregate = new AggregateBindable<double>((a, b) => a * b, new BindableDouble(1)
            {
                Default = 1,
                MinValue = 0,
                MaxValue = 2
            });

            Assert.AreEqual(1, aggregate.Result.Value);

            var bindable1 = new BindableDouble(-1);
            aggregate.AddSource(bindable1);
            Assert.AreEqual(0, aggregate.Result.Value);

            var bindable2 = new BindableDouble(-4);
            aggregate.AddSource(bindable2);
            Assert.AreEqual(2, aggregate.Result.Value);
        }

        [Test]
        public void TestClassAggregate()
        {
            var aggregate = new AggregateBindable<BoxedInt>((a, b) => new BoxedInt((a?.Value ?? 0) + (b?.Value ?? 0)));

            Assert.AreEqual(null, aggregate.Result.Value);

            var bindable1 = new Bindable<BoxedInt>(new BoxedInt(1));
            aggregate.AddSource(bindable1);
            Assert.AreEqual(1, aggregate.Result.Value.Value);

            var bindable2 = new Bindable<BoxedInt>(new BoxedInt(2));
            aggregate.AddSource(bindable2);
            Assert.AreEqual(3, aggregate.Result.Value.Value);
        }

        private class BoxedInt
        {
            public readonly int Value;

            public BoxedInt(int value)
            {
                Value = value;
            }
        }

        [Test]
        public void TestSourceChanged()
        {
            var aggregate = new AggregateBindable<double>((a, b) => a * b, new Bindable<double>(1));

            var bindable1 = new BindableDouble(0.5);
            aggregate.AddSource(bindable1);

            var bindable2 = new BindableDouble(0.5);
            aggregate.AddSource(bindable2);

            Assert.AreEqual(0.25, aggregate.Result.Value);

            bindable1.Value = 0.25;
            Assert.AreEqual(0.125, aggregate.Result.Value);

            bindable2.Value = 0.25;
            Assert.AreEqual(0.0625, aggregate.Result.Value);
        }

        [Test]
        public void TestSourceRemoved()
        {
            var aggregate = new AggregateBindable<double>((a, b) => a * b, new Bindable<double>(1));

            var bindable1 = new BindableDouble(0.5);
            aggregate.AddSource(bindable1);

            var bindable2 = new BindableDouble(0.5);
            aggregate.AddSource(bindable2);

            Assert.AreEqual(0.25, aggregate.Result.Value);

            aggregate.RemoveSource(bindable1);
            Assert.AreEqual(0.5, aggregate.Result.Value);

            aggregate.RemoveSource(bindable2);
            Assert.AreEqual(1, aggregate.Result.Value);
        }

        [Test]
        public void TestValueChangedFirings()
        {
            int aggregateResultFireCount = 0, bindable1FireCount = 0, bindable2FireCount = 0;

            var aggregate = new AggregateBindable<double>((a, b) => a * b, new Bindable<double>(1));
            aggregate.Result.BindValueChanged(_ => Interlocked.Increment(ref aggregateResultFireCount));

            Assert.AreEqual(0, aggregateResultFireCount);

            var bindable1 = new BindableDouble(0.5);
            bindable1.BindValueChanged(_ => Interlocked.Increment(ref bindable1FireCount));
            aggregate.AddSource(bindable1);

            Assert.AreEqual(1, aggregateResultFireCount);

            var bindable2 = new BindableDouble(0.5);
            bindable2.BindValueChanged(_ => Interlocked.Increment(ref bindable2FireCount));
            aggregate.AddSource(bindable2);

            Assert.AreEqual(2, aggregateResultFireCount);

            bindable1.Value = 0.25;

            Assert.AreEqual(3, aggregateResultFireCount);
            Assert.AreEqual(1, bindable1FireCount);
            Assert.AreEqual(0, bindable2FireCount);

            bindable2.Value = 0.25;

            Assert.AreEqual(4, aggregateResultFireCount);
            Assert.AreEqual(1, bindable1FireCount);
            Assert.AreEqual(1, bindable2FireCount);

            aggregate.RemoveSource(bindable2);

            Assert.AreEqual(5, aggregateResultFireCount);
            Assert.AreEqual(1, bindable1FireCount);
            Assert.AreEqual(1, bindable2FireCount);

            bindable2.Value = 0.5;

            Assert.AreEqual(5, aggregateResultFireCount);
            Assert.AreEqual(1, bindable1FireCount);
            Assert.AreEqual(2, bindable2FireCount);
        }
    }
}
