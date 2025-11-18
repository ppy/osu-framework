// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace osu.Framework.Extensions
{
    public static class StreamExtensions
    {
        /// <summary>
        /// Read the full content of a seekable stream.
        /// </summary>
        /// <remarks>
        /// For a non-seekable stream, use <see cref="ReadAllRemainingBytesToArray"/> instead.
        /// </remarks>
        /// <param name="stream">The stream to read.</param>
        /// <returns>The full byte content.</returns>
        /// <exception cref="ArgumentException">The <paramref name="stream"/> does not allow seeking, or is too long.</exception>
        public static byte[] ReadAllBytesToArray(this Stream stream)
        {
            if (!stream.CanSeek)
                throw new ArgumentException($"Stream must be seekable to use this function. Consider using {nameof(ReadAllRemainingBytesToArray)} instead.", nameof(stream));

            if (stream.Length >= Array.MaxLength)
                throw new ArgumentException("The stream is too long for an array.", nameof(stream));

            stream.Seek(0, SeekOrigin.Begin);
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
        /// <exception cref="ArgumentException">The <paramref name="stream"/> does not allow seeking, or is too long.</exception>
        public static Task<byte[]> ReadAllBytesToArrayAsync(this Stream stream, CancellationToken cancellationToken = default)
        {
            if (!stream.CanSeek)
                throw new ArgumentException($"Stream must be seekable to use this function. Consider using {nameof(ReadAllRemainingBytesToArray)} instead.", nameof(stream));

            if (stream.Length >= Array.MaxLength)
                throw new ArgumentException("The stream is too long for an array.", nameof(stream));

            stream.Seek(0, SeekOrigin.Begin);
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
            byte[] bytes = new byte[length];
            stream.ReadExactly(bytes);
            return bytes;
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
            byte[] bytes = new byte[length];
            await stream.ReadExactlyAsync(bytes, cancellationToken).ConfigureAwait(false);
            return bytes;
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
