// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using NUnit.Framework;
using osu.Framework.Configuration;
using osu.Framework.Testing;

namespace osu.Framework.Tests.Configuration
{
    [TestFixture]
    public class FrameworkConfigManagerTest
    {
        [Test]
        public void TestAsioConfigurationSaveLoad()
        {
            using var storage = new TemporaryNativeStorage(Guid.NewGuid().ToString());

            using (var config = new FrameworkConfigManager(storage))
            {
                Assert.That(config.Get<bool>(FrameworkSetting.AudioUseAsio), Is.False);
                Assert.That(config.Get<string>(FrameworkSetting.AudioAsioDevice), Is.Empty);
                Assert.That(config.Get<int>(FrameworkSetting.AudioAsioSampleRate), Is.EqualTo(48000));
                Assert.That(config.Get<int>(FrameworkSetting.AudioAsioBufferSize), Is.Zero);

                config.SetValue(FrameworkSetting.AudioUseAsio, true);
                config.SetValue(FrameworkSetting.AudioAsioDevice, "Test ASIO driver");
                config.SetValue(FrameworkSetting.AudioAsioSampleRate, 96000);
                config.SetValue(FrameworkSetting.AudioAsioBufferSize, 128);
            }

            using (var config = new FrameworkConfigManager(storage))
            {
                Assert.That(config.Get<bool>(FrameworkSetting.AudioUseAsio), Is.True);
                Assert.That(config.Get<string>(FrameworkSetting.AudioAsioDevice), Is.EqualTo("Test ASIO driver"));
                Assert.That(config.Get<int>(FrameworkSetting.AudioAsioSampleRate), Is.EqualTo(96000));
                Assert.That(config.Get<int>(FrameworkSetting.AudioAsioBufferSize), Is.EqualTo(128));
            }
        }

        [Test]
        public void TestDefault()
        {
            using (var storage = new TemporaryNativeStorage(Guid.NewGuid().ToString()))
            {
                using (var configManager = new FrameworkConfigManager(storage))
                {
                    var bindable = configManager.GetBindable<string>(FrameworkSetting.Locale);

                    Assert.AreEqual(string.Empty, bindable.Value);
                    Assert.AreEqual(string.Empty, bindable.Default);
                    Assert.IsTrue(bindable.IsDefault);
                }
            }
        }

        [Test]
        public void TestSaveLoad()
        {
            const double test_volume = 0.65;

            using (var storage = new TemporaryNativeStorage(Guid.NewGuid().ToString()))
            {
                using (var configManager = new FrameworkConfigManager(storage))
                {
                    var bindable = configManager.GetBindable<double>(FrameworkSetting.VolumeMusic);

                    Assert.AreEqual(bindable.Default, bindable.Value);
                    Assert.IsTrue(bindable.IsDefault);

                    bindable.Value = test_volume;
                    Assert.AreEqual(test_volume, bindable.Value);
                }

                using (var configManager = new FrameworkConfigManager(storage))
                {
                    var bindable = configManager.GetBindable<double>(FrameworkSetting.VolumeMusic);

                    Assert.IsFalse(bindable.IsDefault);
                    Assert.AreEqual(test_volume, bindable.Value);
                }
            }
        }

        [Test]
        public void TestDefaultOverrides()
        {
            const string test_locale = "override test";

            using (var storage = new TemporaryNativeStorage(Guid.NewGuid().ToString()))
            {
                using (var configManager = new FrameworkConfigManager(storage, new Dictionary<FrameworkSetting, object> { { FrameworkSetting.Locale, test_locale } }))
                {
                    var bindable = configManager.GetBindable<string>(FrameworkSetting.Locale);
                    Assert.AreEqual(test_locale, bindable.Value);
                    Assert.AreEqual(test_locale, bindable.Default);
                    Assert.IsTrue(bindable.IsDefault);

                    bindable.Value = string.Empty;
                    Assert.IsFalse(bindable.IsDefault);
                }

                // ensure correct after save
                using (var configManager = new FrameworkConfigManager(storage, new Dictionary<FrameworkSetting, object> { { FrameworkSetting.Locale, test_locale } }))
                {
                    var bindable = configManager.GetBindable<string>(FrameworkSetting.Locale);
                    Assert.AreEqual(test_locale, bindable.Default);
                    Assert.AreEqual(string.Empty, bindable.Value);
                    Assert.IsFalse(bindable.IsDefault);

                    bindable.Value = test_locale;
                    Assert.IsTrue(bindable.IsDefault);
                }

                // ensure correct after save with default
                using (var configManager = new FrameworkConfigManager(storage, new Dictionary<FrameworkSetting, object> { { FrameworkSetting.Locale, test_locale } }))
                {
                    var bindable = configManager.GetBindable<string>(FrameworkSetting.Locale);
                    Assert.AreEqual(test_locale, bindable.Value);
                    Assert.AreEqual(test_locale, bindable.Default);
                    Assert.IsTrue(bindable.IsDefault);
                }
            }
        }
    }
}
