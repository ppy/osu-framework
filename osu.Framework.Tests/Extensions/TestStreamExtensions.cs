// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System.IO;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using osu.Framework.Extensions;

namespace osu.Framework.Tests.Extensions
{
    [TestFixture]
    public class TestStreamExtensions
    {
        private readonly byte[] sampleData = File.ReadAllBytes("osu.Framework.dll");

        [Test]
        public void TestReadMemoryStream()
        {
            var ms = new MemoryStream(sampleData);

            byte[] readBytes = ms.ReadAllBytesToArray();

            Assert.That(ms.Length, Is.EqualTo(readBytes.Length));
            Assert.That(ms.ComputeMD5Hash(), Is.EqualTo(new MemoryStream(readBytes).ComputeMD5Hash()));
        }

        [Test]
        public async Task TestReadMemoryStreamAsync()
        {
            var ms = new MemoryStream(sampleData);

            byte[] readBytes = await ms.ReadAllBytesToArrayAsync().ConfigureAwait(false);

            Assert.That(ms.Length, Is.EqualTo(readBytes.Length));
            Assert.That(ms.ComputeMD5Hash(), Is.EqualTo(new MemoryStream(readBytes).ComputeMD5Hash()));
        }

        [Test]
        public void TestReadPartialMemoryStream()
        {
            const int read_length = 32;

            var ms = new MemoryStream(sampleData);

            byte[] readBytes = ms.ReadBytesToArray(read_length);

            Assert.That(readBytes.Length, Is.EqualTo(read_length));

            Assert.That(readBytes, Is.EqualTo(ms.ToArray().Take(read_length)));
        }

        [Test]
        public async Task TestReadPartialMemoryStreamAsync()
        {
            const int read_length = 32;

            var ms = new MemoryStream(sampleData);

            byte[] readBytes = await ms.ReadBytesToArrayAsync(read_length).ConfigureAwait(false);

            Assert.That(readBytes.Length, Is.EqualTo(read_length));

            Assert.That(readBytes, Is.EqualTo(ms.ToArray().Take(read_length)));
        }

        [Test]
        public void TestReadPartialSeekedMemoryStream()
        {
            const int seek_amount = 50;
            const int read_length = 32;

            var ms = new MemoryStream(sampleData);

            // seek should affect ReadBytesToArray operation.
            ms.Seek(seek_amount, SeekOrigin.Begin);
            byte[] readBytes = ms.ReadBytesToArray(read_length);

            Assert.That(readBytes.Length, Is.EqualTo(read_length));

            Assert.That(readBytes, Is.EqualTo(ms.ToArray().Skip(seek_amount).Take(read_length)));
        }

        [Test]
        public async Task TestReadPartialSeekedMemoryStreamAsync()
        {
            const int seek_amount = 50;
            const int read_length = 32;

            var ms = new MemoryStream(sampleData);

            // seek should affect ReadBytesToArray operation.
            ms.Seek(seek_amount, SeekOrigin.Begin);

            byte[] readBytes = await ms.ReadBytesToArrayAsync(read_length).ConfigureAwait(false);

            Assert.That(readBytes.Length, Is.EqualTo(read_length));

            Assert.That(readBytes, Is.EqualTo(ms.ToArray().Skip(seek_amount).Take(read_length)));
        }

        [Test]
        public void TestReadSeekedMemoryStream()
        {
            var ms = new MemoryStream(sampleData);

            // seek should not affect ReadAllBytes operation.
            ms.Seek(50, SeekOrigin.Begin);

            byte[] readBytes = ms.ReadAllBytesToArray();

            Assert.That(ms.Length, Is.EqualTo(readBytes.Length));
            Assert.That(ms.ComputeMD5Hash(), Is.EqualTo(new MemoryStream(readBytes).ComputeMD5Hash()));
        }

        [Test]
        public async Task TestReadSeekedMemoryStreamAsync()
        {
            var ms = new MemoryStream(sampleData);

            // seek should not affect ReadAllBytes operation.
            ms.Seek(50, SeekOrigin.Begin);

            byte[] readBytes = await ms.ReadAllBytesToArrayAsync().ConfigureAwait(false);

            Assert.That(ms.Length, Is.EqualTo(readBytes.Length));
            Assert.That(ms.ComputeMD5Hash(), Is.EqualTo(new MemoryStream(readBytes).ComputeMD5Hash()));
        }

        [Test]
        public void TestReadRemainingBytes()
        {
            const int read_last_byte_count = 128;

            var ms = new MemoryStream(sampleData);

            ms.Seek(-read_last_byte_count, SeekOrigin.End);

            byte[] readBytes = ms.ReadAllRemainingBytesToArray();

            Assert.That(readBytes.Length, Is.EqualTo(read_last_byte_count));
            Assert.That(readBytes, Is.EqualTo(ms.ToArray().TakeLast(read_last_byte_count)));
        }

        [Test]
        public async Task TestReadRemainingBytesAsync()
        {
            const int read_last_byte_count = 128;

            var ms = new MemoryStream(sampleData);

            ms.Seek(-read_last_byte_count, SeekOrigin.End);

            byte[] readBytes = await ms.ReadAllRemainingBytesToArrayAsync().ConfigureAwait(false);

            Assert.That(readBytes.Length, Is.EqualTo(read_last_byte_count));
            Assert.That(readBytes, Is.EqualTo(ms.ToArray().TakeLast(read_last_byte_count)));
        }
    }
}
