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
        private const int buffer_size = 16 * 1024; // Matches generally what .NET uses internally.

        /// <summary>
        /// Read the full content of a seekable stream.
        /// </summary>
        /// <remarks>
        /// For a non-seekable stream, use <see cref="ReadAllRemainingBytesToArray"/> instead.
        /// </remarks>
        /// <param name="stream">The stream to read.</param>
        /// <returns>The full byte content.</returns>
        /// <exception cref="ArgumentException">The <paramref name="stream"/> provided must allow seeking.</exception>
        public static byte[] ReadAllBytesToArray(this Stream stream)
        {
            if (!stream.CanSeek)
                throw new ArgumentException($"Stream must be seekable to use this function. Consider using {nameof(ReadAllRemainingBytesToArray)} instead.", nameof(stream));

            stream.Seek(0, SeekOrigin.Begin);
            Debug.Assert(stream.Length < int.MaxValue);
            return stream.ReadBytesToArray((int)stream.Length);
        }

        /// <summary>
        /// Read the full content of a seekable stream.
        /// </summary>
        /// <remarks>
        /// For a non-seekable stream, use <see cref="ReadAllRemainingBytesToArray"/> instead.
        /// </remarks>
        /// <param name="stream">The stream to read.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns>The full byte content.</returns>
        /// <exception cref="ArgumentException">The <paramref name="stream"/> provided must allow seeking.</exception>
        public static Task<byte[]> ReadAllBytesToArrayAsync(this Stream stream, CancellationToken cancellationToken = default)
        {
            if (!stream.CanSeek)
                throw new ArgumentException($"Stream must be seekable to use this function. Consider using {nameof(ReadAllRemainingBytesToArray)} instead.", nameof(stream));

            stream.Seek(0, SeekOrigin.Begin);
            Debug.Assert(stream.Length < int.MaxValue);
            return stream.ReadBytesToArrayAsync((int)stream.Length, cancellationToken);
        }

        /// <summary>
        /// Read specified length from current position in stream.
        /// </summary>
        /// <param name="stream">The stream to read.</param>
        /// <param name="length">The length to read.</param>
        /// <returns>The full byte content.</returns>
        /// <exception cref="EndOfStreamException">The <paramref name="length"/> specified exceeded the available data in the stream.</exception>
        public static byte[] ReadBytesToArray(this Stream stream, int length)
        {
            byte[] buffer = new byte[buffer_size];

            int remainingRead = length;

            using (var ms = new MemoryStream(length))
            {
                while (remainingRead > 0)
                {
                    int read = stream.Read(buffer, 0, Math.Min(remainingRead, buffer.Length));
                    if (read == 0)
                        break;

                    ms.Write(buffer, 0, read);
                    remainingRead -= read;
                }

                if (remainingRead != 0)
                    throw new EndOfStreamException();

                byte[] bytes = ms.GetBuffer();

                // We are guaranteed that the buffer is the correct length due to the above length checks along with the ctor length specification.
                Debug.Assert(bytes.Length == length);

                return bytes;
            }
        }

        /// <summary>
        /// Read the full content of a stream.
        /// </summary>
        /// <param name="stream">The stream to read.</param>
        /// <param name="length">The length to read.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns>The full byte content.</returns>
        /// <exception cref="EndOfStreamException">The <paramref name="length"/> specified exceeded the available data in the stream.</exception>
        public static async Task<byte[]> ReadBytesToArrayAsync(this Stream stream, int length, CancellationToken cancellationToken = default)
        {
            byte[] buffer = new byte[buffer_size];

            int remainingRead = length;

            using (var ms = new MemoryStream(length))
            {
                while (remainingRead > 0)
                {
                    int read = await stream.ReadAsync(buffer, 0, Math.Min(buffer.Length, remainingRead), cancellationToken).ConfigureAwait(false);
                    if (read == 0)
                        break;

                    await ms.WriteAsync(buffer, 0, read, cancellationToken).ConfigureAwait(false);
                    remainingRead -= read;
                }

                if (remainingRead != 0)
                    throw new EndOfStreamException();

                return ms.GetBuffer();
            }
        }

        /// <summary>
        /// Read all bytes from a non-seekable stream.
        /// </summary>
        /// <param name="stream">The stream to read.</param>
        /// <returns>The full byte content.</returns>
        public static byte[] ReadAllRemainingBytesToArray(this Stream stream)
        {
            using (var ms = new MemoryStream())
            {
                stream.CopyTo(ms);
                return ms.ToArray();
            }
        }

        /// <summary>
        /// Read all bytes from a non-seekable stream.
        /// </summary>
        /// <param name="stream">The stream to read.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns>The full byte content.</returns>
        public static async Task<byte[]> ReadAllRemainingBytesToArrayAsync(this Stream stream, CancellationToken cancellationToken = default)
        {
            using (var ms = new MemoryStream())
            {
                await stream.CopyToAsync(ms, cancellationToken).ConfigureAwait(false);
                return ms.ToArray();
            }
        }
    }
}
