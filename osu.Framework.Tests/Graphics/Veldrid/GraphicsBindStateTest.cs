// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using NUnit.Framework;
using osu.Framework.Graphics.Veldrid.Pipelines;

namespace osu.Framework.Tests.Graphics.Veldrid
{
    [TestFixture]
    public class GraphicsBindStateTest
    {
        [Test]
        public void TestFirstPipelineBindEmits()
        {
            var state = new GraphicsBindState();

            Assert.That(state.ShouldBindPipeline(new object()), Is.True);
        }

        [Test]
        public void TestRepeatedPipelineBindSkipped()
        {
            var state = new GraphicsBindState();
            object pipeline = new object();

            Assert.That(state.ShouldBindPipeline(pipeline), Is.True);
            Assert.That(state.ShouldBindPipeline(pipeline), Is.False);
        }

        [Test]
        public void TestChangedPipelineBindEmits()
        {
            var state = new GraphicsBindState();

            Assert.That(state.ShouldBindPipeline(new object()), Is.True);
            Assert.That(state.ShouldBindPipeline(new object()), Is.True);
        }

        [Test]
        public void TestFirstResourceSetBindEmits()
        {
            var state = new GraphicsBindState();

            Assert.That(state.ShouldBindResourceSet(0, new object()), Is.True);
        }

        [Test]
        public void TestRepeatedResourceSetBindSkipped()
        {
            var state = new GraphicsBindState();
            object resourceSet = new object();

            Assert.That(state.ShouldBindResourceSet(0, resourceSet), Is.True);
            Assert.That(state.ShouldBindResourceSet(0, resourceSet), Is.False);
        }

        [Test]
        public void TestDifferentResourceSetBindEmits()
        {
            var state = new GraphicsBindState();

            Assert.That(state.ShouldBindResourceSet(0, new object()), Is.True);
            Assert.That(state.ShouldBindResourceSet(0, new object()), Is.True);
        }

        [Test]
        public void TestDifferentResourceSetSlotBindEmits()
        {
            var state = new GraphicsBindState();
            object resourceSet = new object();

            Assert.That(state.ShouldBindResourceSet(0, resourceSet), Is.True);
            Assert.That(state.ShouldBindResourceSet(1, resourceSet), Is.True);
        }

        [Test]
        public void TestDifferentDynamicOffsetBindEmits()
        {
            var state = new GraphicsBindState();
            object resourceSet = new object();

            Assert.That(state.ShouldBindResourceSet(0, resourceSet, 4), Is.True);
            Assert.That(state.ShouldBindResourceSet(0, resourceSet, 8), Is.True);
        }

        [Test]
        public void TestRepeatedDynamicOffsetBindSkipped()
        {
            var state = new GraphicsBindState();
            object resourceSet = new object();

            Assert.That(state.ShouldBindResourceSet(0, resourceSet, 4), Is.True);
            Assert.That(state.ShouldBindResourceSet(0, resourceSet, 4), Is.False);
        }

        [Test]
        public void TestResetRequiresBindsToEmitAgain()
        {
            var state = new GraphicsBindState();
            object pipeline = new object();
            object resourceSet = new object();

            Assert.That(state.ShouldBindPipeline(pipeline), Is.True);
            Assert.That(state.ShouldBindResourceSet(0, resourceSet), Is.True);

            state.Reset();

            Assert.That(state.ShouldBindPipeline(pipeline), Is.True);
            Assert.That(state.ShouldBindResourceSet(0, resourceSet), Is.True);
        }

        [Test]
        public void TestPipelineChangeClearsResourceSetBindings()
        {
            var state = new GraphicsBindState();
            object resourceSet = new object();

            Assert.That(state.ShouldBindPipeline(new object()), Is.True);
            Assert.That(state.ShouldBindResourceSet(0, resourceSet), Is.True);
            Assert.That(state.ShouldBindResourceSet(0, resourceSet), Is.False);

            Assert.That(state.ShouldBindPipeline(new object()), Is.True);
            Assert.That(state.ShouldBindResourceSet(0, resourceSet), Is.True);
        }
    }
}
