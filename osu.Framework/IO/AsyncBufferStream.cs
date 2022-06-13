// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace osu.Framework.IO
{
    internal class AsyncBufferStream : Stream
    {
        private const int block_size = 32768;

        #region Concurrent access

        private readonly byte[] data;

        private readonly bool[] blockLoadedStatus;

        private volatile bool isClosed;
        private volatile bool isLoaded;

        private volatile int position;
        private volatile int amountBytesToRead;

        #endregion

        private readonly int blocksToReadAhead;

        private readonly Stream underlyingStream;

        private CancellationTokenSource cancellationToken;

        /// <summary>
        /// A stream that buffers the underlying stream to contiguous memory, reading until the whole file is eventually memory-backed.
        /// </summary>
        /// <param name="stream">The underlying stream to read from.</param>
        /// <param name="blocksToReadAhead">The amount of blocks to read ahead of the read position.</param>
        /// <param name="shared">Another AsyncBufferStream which is backing the same underlying stream. Allows shared usage of memory-backing.</param>
        public AsyncBufferStream(Stream stream, int blocksToReadAhead, AsyncBufferStream shared = null)
        {
            underlyingStream = stream ?? throw new ArgumentNullException(nameof(stream));
            this.blocksToReadAhead = blocksToReadAhead;

            if (underlyingStream.CanSeek)
                underlyingStream.Seek(0, SeekOrigin.Begin);

            if (shared?.Length != stream.Length)
            {
                data = new byte[underlyingStream.Length];
                blockLoadedStatus = new bool[data.Length / block_size + 1];
            }
            else
            {
                data = shared.data;
                blockLoadedStatus = shared.blockLoadedStatus;
                isLoaded = shared.isLoaded;
            }

            cancellationToken = new CancellationTokenSource();
            Task.Factory.StartNew(loadRequiredBlocks, cancellationToken.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
        }

        private void loadRequiredBlocks()
        {
            if (isLoaded)
                return;

            int last = -1;

            while (!isLoaded && !isClosed)
            {
                cancellationToken.Token.ThrowIfCancellationRequested();

                int curr = nextBlockToLoad;

                if (curr < 0)
                {
                    Thread.Sleep(1);
                    continue;
                }

                int readStart = curr * block_size;

                if (last + 1 != curr)
                {
                    //follow along with a seek.
                    Debug.Assert(underlyingStream.CanSeek);
                    underlyingStream.Seek(readStart, SeekOrigin.Begin);
                }

                Trace.Assert(underlyingStream.Position == readStart);

                int readSize = Math.Min(data.Length - readStart, block_size);
                int read = underlyingStream.Read(data, readStart, readSize);

                Trace.Assert(read == readSize);

                blockLoadedStatus[curr] = true;
                last = curr;

                isLoaded = blockLoadedStatus.All(loaded => loaded);
            }

            if (!isClosed) underlyingStream?.Close();
        }

        private int nextBlockToLoad
        {
            get
            {
                if (isClosed) return -1;

                int start = underlyingStream.CanSeek ? position / block_size : 0;

                int end = blockLoadedStatus.Length;
                if (blocksToReadAhead > -1)
                    end = Math.Min(end, (position + amountBytesToRead) / block_size + blocksToReadAhead + 1);

                for (int i = start; i < end; i++)
                {
                    if (!blockLoadedStatus[i])
                        return i;
                }

                return -1;
            }
        }

        private volatile bool isDisposed;

        protected override void Dispose(bool disposing)
        {
            isDisposed = true;

            cancellationToken?.Cancel();
            cancellationToken?.Dispose();
            cancellationToken = null;

            if (!isClosed) Close();
            base.Dispose(disposing);
        }

        public override void Close()
        {
            isClosed = true;

            underlyingStream?.Close();

            base.Close();
        }

        public override bool CanRead => true;

        public override bool CanSeek => true;

        public override bool CanWrite => false;

        public override long Length => data.Length;

        public override long Position
        {
            get => position;
            set => position = Math.Clamp((int)value, 0, data.Length);
        }

        public override void Flush()
        {
            throw new NotSupportedException();
        }

        public override int Read(byte[] buffer, int offset, int count) => Read(buffer.AsSpan(offset, count));

        public override int Read(Span<byte> buffer)
        {
            amountBytesToRead = Math.Min(buffer.Length, data.Length - position);

            int startBlock = position / block_size;
            int endBlock = (position + amountBytesToRead) / block_size;

            //ensure all required buffers are loaded
            for (int i = startBlock; i <= endBlock; i++)
            {
                while (!blockLoadedStatus[i])
                {
                    Thread.Sleep(1);
                    if (isDisposed)
                        return 0;
                }
            }

            data.AsSpan(position, amountBytesToRead).CopyTo(buffer);

            int bytesRead = amountBytesToRead;

            amountBytesToRead = 0;

            Interlocked.Add(ref position, bytesRead);

            return bytesRead;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            switch (origin)
            {
                case SeekOrigin.Begin:
                    Position = offset;
                    break;

                case SeekOrigin.Current:
                    Position += offset;
                    break;

                case SeekOrigin.End:
                    Position = data.Length + offset;
                    break;
            }

            return position;
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }
    }
}
