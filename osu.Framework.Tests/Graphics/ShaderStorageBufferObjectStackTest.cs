// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Runtime.InteropServices;
using NUnit.Framework;
using osu.Framework.Graphics.Rendering;
using osu.Framework.Graphics.Rendering.Dummy;
using osu.Framework.Graphics.Shaders.Types;

namespace osu.Framework.Tests.Graphics
{
    public class ShaderStorageBufferObjectStackTest
    {
        private const int size = 10;

        private ShaderStorageBufferObjectStack<TestUniformData> stack = null!;

        [SetUp]
        public void Setup()
        {
            stack = new ShaderStorageBufferObjectStack<TestUniformData>(new DummyRenderer(), 2, size);
        }

        [Test]
        public void TestBufferMustBeAtLeast2Elements()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => _ = new ShaderStorageBufferObjectStack<TestUniformData>(new DummyRenderer(), 1, 100));
            Assert.Throws<ArgumentOutOfRangeException>(() => _ = new ShaderStorageBufferObjectStack<TestUniformData>(new DummyRenderer(), 100, 1));
            Assert.DoesNotThrow(() => _ = new ShaderStorageBufferObjectStack<TestUniformData>(new DummyRenderer(), 2, 100));
            Assert.DoesNotThrow(() => _ = new ShaderStorageBufferObjectStack<TestUniformData>(new DummyRenderer(), 100, 2));
        }

        [Test]
        public void TestInitialState()
        {
            Assert.That(stack.CurrentOffset, Is.Zero);
            Assert.That(stack.CurrentBuffer, Is.Not.Null);
            Assert.That(stack.CurrentBuffer[stack.CurrentOffset].Int.Value, Is.EqualTo(0));
        }

        [Test]
        public void TestPopWithNoItems()
        {
            Assert.Throws<InvalidOperationException>(() => stack.Pop());
        }

        [Test]
        public void TestAddInitialItem()
        {
            var firstBuffer = stack.CurrentBuffer;

            stack.Push(new TestUniformData { Int = 1 });

            Assert.That(stack.CurrentOffset, Is.Zero);
            Assert.That(stack.CurrentBuffer, Is.EqualTo(firstBuffer));
            Assert.That(stack.CurrentBuffer[stack.CurrentOffset].Int.Value, Is.EqualTo(1));
        }

        [Test]
        public void TestPushToFillOneBuffer()
        {
            var firstBuffer = stack.CurrentBuffer;
            int expectedIndex = 0;

            for (int i = 0; i < size; i++)
            {
                stack.Push(new TestUniformData { Int = i });
                Assert.That(stack.CurrentOffset, Is.EqualTo(expectedIndex++));
                Assert.That(stack.CurrentBuffer, Is.EqualTo(firstBuffer));
                Assert.That(stack.CurrentBuffer[stack.CurrentOffset].Int.Value, Is.EqualTo(i));
            }
        }

        [Test]
        public void TestPopEntireBuffer()
        {
            for (int i = 0; i < size; i++)
                stack.Push(new TestUniformData { Int = i });

            var firstBuffer = stack.CurrentBuffer;

            for (int i = size - 1; i >= 0; i--)
            {
                Assert.That(stack.CurrentOffset, Is.EqualTo(i));
                Assert.That(stack.CurrentBuffer, Is.EqualTo(firstBuffer));
                Assert.That(stack.CurrentBuffer[stack.CurrentOffset].Int.Value, Is.EqualTo(i));
                stack.Pop();
            }
        }

        [Test]
        public void TestTransitionToBufferOnPush()
        {
            for (int i = 0; i < size; i++)
                stack.Push(new TestUniformData { Int = i });

            var firstBuffer = stack.CurrentBuffer;
            int copiedItem = stack.CurrentBuffer[stack.CurrentOffset].Int.Value;

            // Transition to a new buffer...
            stack.Push(new TestUniformData { Int = size });
            Assert.That(stack.CurrentBuffer, Is.Not.EqualTo(firstBuffer));

            // ... where the "hack" employed by the queue means that after a transition, the new item is added at index 1...
            Assert.That(stack.CurrentOffset, Is.EqualTo(1));
            Assert.That(stack.CurrentBuffer[1].Int.Value, Is.EqualTo(size));

            // ... and the first item in the new buffer is a copy of the last referenced item before the push.
            Assert.That(stack.CurrentBuffer[0].Int.Value, Is.EqualTo(copiedItem));
        }

        [Test]
        public void TestTransitionToBufferOnPop()
        {
            for (int i = 0; i < size; i++)
                stack.Push(new TestUniformData { Int = i });

            var firstBuffer = stack.CurrentBuffer;
            int copiedItem = stack.CurrentBuffer[stack.CurrentOffset].Int.Value;

            // Transition to the new buffer.
            stack.Push(new TestUniformData { Int = size });

            // The "hack" employed means that on the first pop, the index moves to the 0th index in the new buffer.
            stack.Pop();
            Assert.That(stack.CurrentBuffer, Is.Not.EqualTo(firstBuffer));
            Assert.That(stack.CurrentOffset, Is.Zero);
            Assert.That(stack.CurrentBuffer[stack.CurrentOffset].Int.Value, Is.EqualTo(copiedItem));

            // After a subsequent pop, we transition to the previous buffer and move to the index prior to the copied item.
            // We've already seen the copied item in the new buffer with the above pop, so we should not see it again here.
            stack.Pop();
            Assert.That(stack.CurrentBuffer, Is.EqualTo(firstBuffer));
            Assert.That(stack.CurrentOffset, Is.EqualTo(copiedItem - 1));
            Assert.That(stack.CurrentBuffer[stack.CurrentOffset].Int.Value, Is.EqualTo(copiedItem - 1));

            // Popping once again should move the index further backwards.
            stack.Pop();
            Assert.That(stack.CurrentBuffer, Is.EqualTo(firstBuffer));
            Assert.That(stack.CurrentOffset, Is.EqualTo(copiedItem - 2));
        }

        [Test]
        public void TestTransitionToAndFromNewBufferFromMiddle()
        {
            for (int i = 0; i < size; i++)
                stack.Push(new TestUniformData { Int = i });

            // Move to the middle of the current buffer (it can not take up any new items at this point).
            stack.Pop();
            stack.Pop();

            var firstBuffer = stack.CurrentBuffer;
            int copiedItem = stack.CurrentOffset;

            // Transition to the new buffer...
            stack.Push(new TestUniformData { Int = size });

            // ... and as above, we arrive at index 1 in the new buffer.
            Assert.That(stack.CurrentBuffer, Is.Not.EqualTo(firstBuffer));
            Assert.That(stack.CurrentOffset, Is.EqualTo(1));
            Assert.That(stack.CurrentBuffer[1].Int.Value, Is.EqualTo(size));
            Assert.That(stack.CurrentBuffer[0].Int.Value, Is.EqualTo(copiedItem));

            // Transition to the previous buffer...
            stack.Pop();
            stack.Pop();

            // ... noting that this is the same as the above "normal" pop case, except that item arrived at is in the middle of the previous buffer.
            Assert.That(stack.CurrentBuffer, Is.EqualTo(firstBuffer));
            Assert.That(stack.CurrentOffset, Is.EqualTo(copiedItem - 1));
            Assert.That(stack.CurrentBuffer[stack.CurrentOffset].Int.Value, Is.EqualTo(copiedItem - 1));

            // Popping once again from this state should move further backwards.
            stack.Pop();
            Assert.That(stack.CurrentBuffer, Is.EqualTo(firstBuffer));
            Assert.That(stack.CurrentOffset, Is.EqualTo(copiedItem - 2));
        }

        [Test]
        public void TestMoveToAndFromMiddleOfNewBuffer()
        {
            for (int i = 0; i < size; i++)
                stack.Push(new TestUniformData { Int = i });

            var lastBuffer = stack.CurrentBuffer;
            int copiedItem1 = stack.CurrentBuffer[stack.CurrentOffset].Int.Value;

            // Transition to the middle of the new buffer.
            stack.Push(new TestUniformData { Int = size });
            stack.Push(new TestUniformData { Int = size + 1 });
            Assert.That(stack.CurrentBuffer, Is.Not.EqualTo(lastBuffer));
            Assert.That(stack.CurrentOffset, Is.EqualTo(2));
            Assert.That(stack.CurrentBuffer[2].Int.Value, Is.EqualTo(size + 1));
            Assert.That(stack.CurrentBuffer[1].Int.Value, Is.EqualTo(size));
            Assert.That(stack.CurrentBuffer[0].Int.Value, Is.EqualTo(copiedItem1));

            // Transition to the previous buffer.
            stack.Pop();
            stack.Pop();
            stack.Pop();
            Assert.That(stack.CurrentBuffer, Is.EqualTo(lastBuffer));

            // The item that will be copied into the new buffer.
            int copiedItem2 = stack.CurrentBuffer[stack.CurrentOffset].Int.Value;

            // Transition to the new buffer...
            stack.Push(new TestUniformData { Int = size + 2 });
            Assert.That(stack.CurrentBuffer, Is.Not.EqualTo(lastBuffer));

            // ... noting that this is the same as the normal case of transitioning to a new buffer, except arriving in the middle of it...
            Assert.That(stack.CurrentOffset, Is.EqualTo(4));
            Assert.That(stack.CurrentBuffer[4].Int.Value, Is.EqualTo(size + 2));

            // ... where this is the copied item as a result of the immediate push...
            Assert.That(stack.CurrentBuffer[3].Int.Value, Is.EqualTo(copiedItem2));

            // ... and these are the same items from the first pushes above.
            Assert.That(stack.CurrentBuffer[2].Int.Value, Is.EqualTo(size + 1));
            Assert.That(stack.CurrentBuffer[1].Int.Value, Is.EqualTo(size));
            Assert.That(stack.CurrentBuffer[0].Int.Value, Is.EqualTo(copiedItem1));

            // Transition to the previous buffer...
            stack.Pop();
            stack.Pop();
            Assert.That(stack.CurrentBuffer, Is.EqualTo(lastBuffer));

            // ... but this one's a little tricky. The entire process up to this point is:
            // 1. From index N-1 -> transition to new buffer.
            // 2. Transition to old buffer, arrive at index N-2 (N-1 was copied into the new buffer).
            // 3. From index N-2 -> transition to new buffer.
            // 4. Transition to old buffer, arrive at index N-3 (N-2 was copied into the new buffer).
            Assert.That(stack.CurrentOffset, Is.EqualTo(size - 3));
            Assert.That(stack.CurrentBuffer[stack.CurrentOffset].Int.Value, Is.EqualTo(size - 3));
        }

        [Test]
        public void TestTransitionFromEmptyStack()
        {
            for (int i = 0; i < size * 2; i++)
            {
                var lastBuffer = stack.CurrentBuffer;

                // Push one item.
                stack.Push(new TestUniformData { Int = i });

                // On a buffer transition, test that the item at the 0-th index of the first buffer was correct copied to the new buffer.
                if (stack.CurrentBuffer != lastBuffer)
                    Assert.That(stack.CurrentBuffer[stack.CurrentOffset - 1].Int.Value, Is.EqualTo(0));

                // Test that the item was correctly placed in the new buffer
                Assert.That(stack.CurrentBuffer[stack.CurrentOffset].Int.Value, Is.EqualTo(i));

                // Return to an empty stack.
                stack.Pop();
            }
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private record struct TestUniformData
        {
            public UniformInt Int;
            private UniformPadding12 pad;
        }
    }
}
