// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace osu.Framework.Extensions
{
    public static class StreamExtensions
    {
        /// <summary>
        /// Read the full content of a stream.
        /// </summary>
        /// <param name="stream">The stream to read.</param>
        /// <returns>The full byte content.</returns>
        public static byte[] ReadAllBytesToArray(this Stream stream)
        {
            Debug.Assert(stream.Length < int.MaxValue);

            return stream.ReadBytesToArray((int)stream.Length);
        }

        /// <summary>
        /// Read the full content of a stream.
        /// </summary>
        /// <param name="stream">The stream to read.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns>The full byte content.</returns>
        public static Task<byte[]> ReadAllBytesToArrayAsync(this Stream stream, CancellationToken cancellationToken)
        {
            Debug.Assert(stream.Length < int.MaxValue);

            return stream.ReadBytesToArrayAsync((int)stream.Length, cancellationToken);
        }

        /// <summary>
        /// Read specified length from current position in stream.
        /// </summary>
        /// <param name="stream">The stream to read.</param>
        /// <param name="length">The length to read.</param>
        /// <returns>The full byte content.</returns>
        public static byte[] ReadBytesToArray(this Stream stream, int length)
        {
            if (stream is MemoryStream ms && length == ms.GetBuffer().Length)
                return ms.GetBuffer();

            byte[] buffer = new byte[16 * 1024];

            using (ms = new MemoryStream(length))
            {
                int read;
                int totalRead = 0;

                while ((read = stream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    ms.Write(buffer, 0, read);
                    totalRead += read;
                }

                if (totalRead != length)
                    throw new EndOfStreamException();

                return ms.GetBuffer();
            }
        }

        /// <summary>
        /// Read the full content of a stream.
        /// </summary>
        /// <param name="stream">The stream to read.</param>
        /// <param name="length">The length to read.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns>The full byte content.</returns>
        public static async Task<byte[]> ReadBytesToArrayAsync(this Stream stream, int length, CancellationToken cancellationToken)
        {
            if (stream is MemoryStream ms && length == ms.GetBuffer().Length)
                return ms.GetBuffer();

            byte[] buffer = new byte[16 * 1024];

            using (ms = new MemoryStream(length))
            {
                int read;
                int totalRead = 0;

                while ((read = await stream.ReadAsync(buffer.AsMemory(), cancellationToken).ConfigureAwait(false)) > 0)
                {
                    await ms.WriteAsync(buffer, 0, read, cancellationToken).ConfigureAwait(false);
                    totalRead += read;
                }

                if (totalRead != length)
                    throw new EndOfStreamException();

                return ms.GetBuffer();
            }
        }
    }
}
