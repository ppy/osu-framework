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
            if (stream.CanSeek)
            {
                stream.Seek(0, SeekOrigin.Begin);
                Debug.Assert(stream.Length < int.MaxValue);
                return stream.ReadBytesToArray((int)stream.Length);
            }

            return stream.ReadAllRemainingBytesToArray();
        }

        /// <summary>
        /// Read the full content of a stream.
        /// </summary>
        /// <param name="stream">The stream to read.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns>The full byte content.</returns>
        public static Task<byte[]> ReadAllBytesToArrayAsync(this Stream stream, CancellationToken cancellationToken = default)
        {
            if (stream.CanSeek)
            {
                stream.Seek(0, SeekOrigin.Begin);
                Debug.Assert(stream.Length < int.MaxValue);
                return stream.ReadBytesToArrayAsync((int)stream.Length, cancellationToken);
            }

            return stream.ReadAllRemainingBytesToArrayAsync(cancellationToken);
        }

        /// <summary>
        /// Read specified length from current position in stream.
        /// </summary>
        /// <param name="stream">The stream to read.</param>
        /// <param name="length">The length to read.</param>
        /// <returns>The full byte content.</returns>
        public static byte[] ReadBytesToArray(this Stream stream, int length)
        {
            byte[] buffer = new byte[16 * 1024];

            int remainingRead = length;

            using (var ms = new MemoryStream(length))
            {
                int read;

                while ((read = stream.Read(buffer, 0, Math.Min(remainingRead, buffer.Length))) > 0)
                {
                    ms.Write(buffer, 0, read);
                    remainingRead -= read;
                }

                if (remainingRead != 0)
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
        public static async Task<byte[]> ReadBytesToArrayAsync(this Stream stream, int length, CancellationToken cancellationToken = default)
        {
            byte[] buffer = new byte[16 * 1024];

            int remainingRead = length;

            using (var ms = new MemoryStream(length))
            {
                int read;

                while ((read = await stream.ReadAsync(buffer, 0, Math.Min(buffer.Length, remainingRead), cancellationToken).ConfigureAwait(false)) > 0)
                {
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
            byte[] buffer = new byte[16 * 1024];

            using (var ms = new MemoryStream())
            {
                int read;

                while ((read = stream.Read(buffer, 0, buffer.Length)) > 0)
                    ms.Write(buffer, 0, read);

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
            byte[] buffer = new byte[16 * 1024];

            using (var ms = new MemoryStream())
            {
                int read;

                while ((read = await stream.ReadAsync(buffer.AsMemory(), cancellationToken).ConfigureAwait(false)) > 0)
                    await ms.WriteAsync(buffer, 0, read, cancellationToken).ConfigureAwait(false);

                return ms.ToArray();
            }
        }
    }
}
