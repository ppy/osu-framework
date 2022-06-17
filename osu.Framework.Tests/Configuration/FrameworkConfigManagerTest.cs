// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

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
