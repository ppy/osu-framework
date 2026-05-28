// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using System.Threading;
using NUnit.Framework;
using osu.Framework.Audio;
using osu.Framework.IO.Stores;
using osu.Framework.Threading;

namespace osu.Framework.Tests.Audio
{
    [TestFixture]
    public class AsioDeviceSwitchingTest
    {
        [Test]
        public void TestAsioPcmModeReconfigurationPathsAndEvents()
        {
            if (RuntimeInfo.OS != RuntimeInfo.Platform.Windows)
                Assert.Ignore("ASIO is only supported on Windows");

            AudioThread.PreloadBass();

            var audioThread = new AudioThread();
            var trackStore = new ResourceStore<byte[]>(new DllResourceStore(typeof(AsioDeviceSwitchingTest).Assembly));
            var sampleStore = new ResourceStore<byte[]>(new DllResourceStore(typeof(AsioDeviceSwitchingTest).Assembly));

            try
            {
                using (var audioManager = new AudioManager(audioThread, trackStore, sampleStore, null))
                {
                    audioThread.Start();

                    var asioDevices = audioManager.AudioDeviceNames.Where(name => name.Contains("(ASIO)")).ToList();

                    if (asioDevices.Count == 0)
                        Assert.Ignore("No ASIO devices available for testing");

                    var eventState = new AsioEventState();

                    audioManager.OnAsioDeviceConfigurationChanged += (_, buffer, _) =>
                    {
                        Interlocked.Increment(ref eventState.ConfigurationChangedCount);
                        Volatile.Write(ref eventState.LastBufferSize, buffer);
                    };
                    audioManager.OnAsioDeviceInitialized += _ => Interlocked.Increment(ref eventState.InitializedCount);

                    string? activeAsioDevice = null;

                    foreach (string candidate in asioDevices)
                    {
                        audioManager.AudioDevice.Value = candidate;
                        Thread.Sleep(1200);

                        if (!audioManager.IsAsioOutputActive())
                            continue;

                        activeAsioDevice = candidate;
                        break;
                    }

                    if (activeAsioDevice == null)
                        Assert.Ignore("No ASIO device could be started in this environment");

                    // (2) Internal PCM should use buffer-only short path for buffer-only changes.
                    audioThread.ResetLastAsioReconfigurePath();
                    audioManager.SetAsioUseExternalPCM(false);
                    audioManager.SetAsioBufferSize(audioManager.AsioBufferSize.Value == 128 ? 256 : 128);
                    waitForPath(audioThread, AudioThread.AsioReconfigurePath.BufferRestart);

                    // (1)(2) External PCM should force full reconfigure, skipping buffer-only path.
                    audioThread.ResetLastAsioReconfigurePath();
                    audioManager.SetAsioUseExternalPCM(true);
                    waitForPath(audioThread, AudioThread.AsioReconfigurePath.FullReconfigure);

                    audioThread.ResetLastAsioReconfigurePath();
                    audioManager.SetAsioBufferSize(audioManager.AsioBufferSize.Value == 128 ? 256 : 128);
                    waitForPath(audioThread, AudioThread.AsioReconfigurePath.FullReconfigure);

                    // (3) Successful start reports valid buffer and emits init/config events.
                    Assert.That(Volatile.Read(ref eventState.LastBufferSize), Is.GreaterThan(0), "Expected non-zero active buffer size in ASIO configuration callback.");
                    Assert.That(Volatile.Read(ref eventState.ConfigurationChangedCount), Is.GreaterThan(0), "Expected ASIO configuration changed event.");
                    Assert.That(Volatile.Read(ref eventState.InitializedCount), Is.GreaterThan(0), "Expected ASIO initialized event.");
                }
            }
            finally
            {
                audioThread.Exit();
            }
        }

        [Test]
        public void TestAsioDeviceSwitching()
        {
            // Skip if not on Windows
            if (RuntimeInfo.OS != RuntimeInfo.Platform.Windows)
                Assert.Ignore("ASIO is only supported on Windows");

            AudioThread.PreloadBass();

            var audioThread = new AudioThread();
            var trackStore = new ResourceStore<byte[]>(new DllResourceStore(typeof(AsioDeviceSwitchingTest).Assembly));
            var sampleStore = new ResourceStore<byte[]>(new DllResourceStore(typeof(AsioDeviceSwitchingTest).Assembly));

            using (var audioManager = new AudioManager(audioThread, trackStore, sampleStore, null))
            {
                var deviceNames = audioManager.AudioDeviceNames.ToList();
                var asioDevices = deviceNames.Where(name => name.Contains("(ASIO)")).ToList();

                if (asioDevices.Count == 0)
                    Assert.Ignore("No ASIO devices available for testing");

                // Test switching to each ASIO device
                foreach (string? asioDevice in asioDevices)
                {
                    TestContext.WriteLine($"Testing switch to ASIO device: {asioDevice}");

                    // Set the audio device - this should trigger device initialization
                    audioManager.AudioDevice.Value = asioDevice;

                    // Wait for device initialization to complete
                    Thread.Sleep(500);

                    // Verify the device was set
                    Assert.AreEqual(asioDevice, audioManager.AudioDevice.Value, $"Failed to switch to {asioDevice}");

                    TestContext.WriteLine($"Successfully switched to {asioDevice}");
                }

                // Test switching back to default device
                TestContext.WriteLine("Testing switch back to default device");
                audioManager.AudioDevice.Value = string.Empty;

                Thread.Sleep(500);
                Assert.AreEqual(string.Empty, audioManager.AudioDevice.Value, "Failed to switch back to default device");

                TestContext.WriteLine("ASIO device switching test completed successfully");
            }
        }

        private static void waitForPath(AudioThread thread, AudioThread.AsioReconfigurePath expectedPath, int timeoutMs = 5000)
        {
            int waited = 0;

            while (waited < timeoutMs)
            {
                if (thread.LastAsioReconfigurePath == expectedPath)
                    return;

                Thread.Sleep(50);
                waited += 50;
            }

            Assert.Fail($"Timed out waiting for ASIO reconfigure path {expectedPath}. Last path: {thread.LastAsioReconfigurePath?.ToString() ?? "null"}");
        }

        private class AsioEventState
        {
            public int ConfigurationChangedCount;
            public int InitializedCount;
            public int LastBufferSize;
        }
    }
}
