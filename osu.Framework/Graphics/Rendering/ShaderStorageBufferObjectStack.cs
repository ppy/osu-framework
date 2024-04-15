// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;

namespace osu.Framework.Graphics.Rendering
{
    /// <summary>
    /// A wrapper around <see cref="IShaderStorageBufferObject{TData}"/> providing push/pop semantics for writing
    /// an arbitrary amount of data to an unbounded set of shader storage buffer objects.
    /// </summary>
    public class ShaderStorageBufferObjectStack<TData> : IDisposable
        where TData : unmanaged, IEquatable<TData>
    {
        /// <summary>
        /// The index of the current item inside <see cref="CurrentBuffer"/>.
        /// </summary>
        public int CurrentOffset => currentBufferOffset;

        /// <summary>
        /// The buffer that contains the current object.
        /// </summary>
        public IShaderStorageBufferObject<TData> CurrentBuffer => buffers[currentBufferIndex];

        /// <summary>
        /// The index of the item inside the buffer containing it.
        /// </summary>
        private int currentBufferOffset => currentIndex == -1 ? 0 : currentIndex % bufferSize;

        /// <summary>
        /// The index of the buffer containing the current item.
        /// </summary>
        private int currentBufferIndex => currentIndex == -1 ? 0 : currentIndex / bufferSize;

        private readonly List<IShaderStorageBufferObject<TData>> buffers = new List<IShaderStorageBufferObject<TData>>();
        private readonly Stack<int> lastIndices = new Stack<int>();

        /// <summary>
        /// A monotonically increasing (during a frame) index at which items are added to the stack.
        /// </summary>
        private int nextAdditionIndex;

        /// <summary>
        /// The index of the current item, based on the total size of this stack.
        /// This is incremented and decremented during a frame through <see cref="Push"/> and <see cref="Pop"/>.
        /// </summary>
        private int currentIndex = -1;

        /// <summary>
        /// The size of an individual buffer of this stack.
        /// </summary>
        private readonly int bufferSize;

        private readonly IRenderer renderer;
        private readonly int uboSize;
        private readonly int ssboSize;

        /// <summary>
        /// Creates a new <see cref="ShaderStorageBufferObjectStack{TData}"/>.
        /// </summary>
        /// <param name="renderer">The <see cref="IRenderer"/>.</param>
        /// <param name="uboSize">Must be at least 2. See <see cref="IRenderer.CreateShaderStorageBufferObject{TData}"/></param>
        /// <param name="ssboSize">Must be at least 2. See <see cref="IRenderer.CreateShaderStorageBufferObject{TData}"/></param>
        public ShaderStorageBufferObjectStack(IRenderer renderer, int uboSize, int ssboSize)
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(uboSize, 2);
            ArgumentOutOfRangeException.ThrowIfLessThan(ssboSize, 2);

            this.renderer = renderer;
            this.uboSize = uboSize;
            this.ssboSize = ssboSize;

            ensureCapacity(1);

            bufferSize = buffers[0].Size;
        }

        /// <summary>
        /// Pushes a new item to this stack.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns>The index inside the resulting buffer at which the item is located.</returns>
        public int Push(TData item)
        {
            lastIndices.Push(currentIndex);

            int newIndex = nextAdditionIndex++;
            int newBufferIndex = newIndex / bufferSize;
            int newBufferOffset = newIndex % bufferSize;

            // Ensure that the item can be stored.
            ensureCapacity(newBufferIndex + 1);

            // Flush the pipeline if this invocation transitions to a new buffer.
            if (newBufferIndex != currentBufferIndex)
            {
                renderer.FlushCurrentBatch(FlushBatchSource.StorageBufferOverflow);

                //
                // When transitioning to a new buffer, we want to minimise a certain "thrashing" effect that occurs with successive push/pops.
                // For example, suppose the sequence: push -> draw -> pop -> push -> draw -> pop -> etc...
                // Each push transitions to buffer X+1 and each pop transitions back to buffer X, resulting in many pipeline flushes.
                //
                // This is a very specific use case that arises when several consumers push items anonymously from one other.
                //
                // A little hack is employed to alleviate this issue for ONE push-pop sequence:
                // When transitioning to a new buffer, we copy the last item from the last buffer into the new buffer,
                // and adjust the stack so that we no longer refer to a position inside the last buffer upon a pop.
                //
                // If the item to be copied would end up at the last index in the new buffer, then we also need to advance the buffer itself,
                // otherwise the user's item would be placed in a new buffer anyway and undo this optimisation.
                //
                // This is a trade-off of space for performance (by reducing flushes).
                //

                // If the copy would be placed at the end of the new buffer, advance the buffer.
                if (newBufferOffset == bufferSize - 1)
                {
                    nextAdditionIndex++;
                    newIndex++;
                    newBufferIndex++;
                    newBufferOffset = 0;

                    ensureCapacity(newBufferIndex + 1);
                }

                // Copy the current item from the last buffer into the new buffer.
                buffers[newBufferIndex][newBufferOffset] = buffers[currentBufferIndex][currentBufferOffset];

                // Adjust the stack so the last index points to the index in the new buffer, instead of currentIndex (from the old buffer).
                lastIndices.Pop();
                lastIndices.Push(newIndex);

                nextAdditionIndex++;
                newIndex++;
                newBufferOffset++;
            }

            // Add the item.
            buffers[newBufferIndex][newBufferOffset] = item;
            currentIndex = newIndex;

            return newBufferOffset;
        }

        /// <summary>
        /// Pops the last item from the stack.
        /// </summary>
        /// <remarks>
        /// This does not remove the item from the stack or the underlying buffer,
        /// but adjusts <see cref="CurrentOffset"/> and <see cref="CurrentBuffer"/>.
        /// </remarks>
        public void Pop()
        {
            if (currentIndex == -1)
                throw new InvalidOperationException("There are no items in the stack to pop.");

            int newIndex = lastIndices.Pop();
            int newBufferIndex = newIndex / bufferSize;

            // Flush the pipeline if this invocation transitions to a new buffer.
            if (newBufferIndex != currentBufferIndex)
                renderer.FlushCurrentBatch(FlushBatchSource.StorageBufferOverflow);

            currentIndex = newIndex;
        }

        /// <summary>
        /// Clears the stack. This should be called at the start of every frame to prevent runaway VRAM usage.
        /// </summary>
        public void Clear()
        {
            nextAdditionIndex = 0;
            currentIndex = -1;
            lastIndices.Clear();
        }

        private void ensureCapacity(int size)
        {
            while (buffers.Count < size)
                buffers.Add(renderer.CreateShaderStorageBufferObject<TData>(uboSize, ssboSize));
        }

        public void Dispose()
        {
            foreach (var buffer in buffers)
                buffer.Dispose();
        }
    }
}
