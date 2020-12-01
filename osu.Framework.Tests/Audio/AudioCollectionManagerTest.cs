// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using NUnit.Framework;
using osu.Framework.Audio;

namespace osu.Framework.Tests.Audio
{
    [TestFixture]
    public class AudioCollectionManagerTest
    {
        private class TestingAdjustableAudioComponent : AdjustableAudioComponent
        {

        }

        [Test]
        public void TestDisposalWhileItemsAreAddedDoesNotThrowInvalidOperationException()
        {
            var manager = new AudioCollectionManager<AdjustableAudioComponent>();

            var components = new List<TestingAdjustableAudioComponent>();
            for (int i = 0; i < 10000; i++)
            {
                var component = new TestingAdjustableAudioComponent();
                components.Add(component);
                manager.AddItem(component);
            }

            new Thread(() => manager.Update()).Start();
 
            Thread.Sleep(2);
            Assert.DoesNotThrow(() =>
            {
                manager.Dispose();
            });
        }
    }
}
