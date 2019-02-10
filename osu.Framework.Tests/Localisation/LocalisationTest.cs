// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.IO;
using System.Threading.Tasks;
using NUnit.Framework;
using osu.Framework.Configuration;
using osu.Framework.IO.Stores;
using osu.Framework.Localisation;

namespace osu.Framework.Tests.Localisation
{
    [TestFixture]
    public class LocalisationTest
    {
        private FrameworkConfigManager config;
        private LocalisationManager manager;

        [SetUp]
        public void Setup()
        {
            config = new FakeFrameworkConfigManager();
            manager = new LocalisationManager(config);
            manager.AddLanguage("en", new FakeStorage("en"));
        }

        [Test]
        public void TestNotLocalised()
        {
            manager.AddLanguage("ja-JP", new FakeStorage("ja-JP"));
            config.Set(FrameworkSetting.Locale, "ja-JP");

            var localisedText = manager.GetLocalisedString(FakeStorage.LOCALISABLE_STRING_EN);

            Assert.AreEqual(FakeStorage.LOCALISABLE_STRING_EN, localisedText.Value);

            localisedText.Text = FakeStorage.LOCALISABLE_STRING_JA;

            Assert.AreEqual(FakeStorage.LOCALISABLE_STRING_JA, localisedText.Value);
        }

        [Test]
        public void TestLocalised()
        {
            manager.AddLanguage("ja-JP", new FakeStorage("ja-JP"));

            var localisedText = manager.GetLocalisedString(new LocalisedString(FakeStorage.LOCALISABLE_STRING_EN));

            Assert.AreEqual(FakeStorage.LOCALISABLE_STRING_EN, localisedText.Value);

            config.Set(FrameworkSetting.Locale, "ja-JP");
            Assert.AreEqual(FakeStorage.LOCALISABLE_STRING_JA_JP, localisedText.Value);
        }

        [Test]
        public void TestLocalisationFallback()
        {
            manager.AddLanguage("ja", new FakeStorage("ja"));

            config.Set(FrameworkSetting.Locale, "ja-JP");

            var localisedText = manager.GetLocalisedString(new LocalisedString(FakeStorage.LOCALISABLE_STRING_EN));

            Assert.AreEqual(FakeStorage.LOCALISABLE_STRING_JA, localisedText.Value);
        }

        [Test]
        public void TestFormatted()
        {
            const string to_format = "this {0} {1} formatted";
            const string arg_0 = "has";
            const string arg_1 = "been";

            string expectedResult = string.Format(to_format, arg_0, arg_1);

            var formattedText = manager.GetLocalisedString(string.Format(to_format, arg_0, arg_1));

            Assert.AreEqual(expectedResult, formattedText.Value);
        }

        [Test]
        public void TestFormattedAndLocalised()
        {
            const string arg_0 = "formatted";

            string expectedResult = string.Format(FakeStorage.LOCALISABLE_FORMAT_STRING_JA, arg_0);

            manager.AddLanguage("ja", new FakeStorage("ja"));
            config.Set(FrameworkSetting.Locale, "ja");

            var formattedText = manager.GetLocalisedString(new LocalisedString(FakeStorage.LOCALISABLE_FORMAT_STRING_EN, arg_0));

            Assert.AreEqual(expectedResult, formattedText.Value);
        }

        [Test]
        public void TestUnicodePreference()
        {
            const string non_unicode = "non unicode";
            const string unicode = "unicode";

            var text = manager.GetLocalisedString(new LocalisedString((unicode, non_unicode)));

            config.Set(FrameworkSetting.ShowUnicode, true);
            Assert.AreEqual(unicode, text.Value);

            config.Set(FrameworkSetting.ShowUnicode, false);
            Assert.AreEqual(non_unicode, text.Value);
        }

        [Test]
        public void TestUnicodeStringChanging()
        {
            const string non_unicode_1 = "non unicode 1";
            const string non_unicode_2 = "non unicode 2";
            const string unicode_1 = "unicode 1";
            const string unicode_2 = "unicode 2";

            var text = manager.GetLocalisedString(new LocalisedString((unicode_1, non_unicode_1)));

            config.Set(FrameworkSetting.ShowUnicode, false);
            Assert.AreEqual(non_unicode_1, text.Value);

            text.Text = new LocalisedString((unicode_1, non_unicode_2));
            Assert.AreEqual(non_unicode_2, text.Value);

            config.Set(FrameworkSetting.ShowUnicode, true);
            Assert.AreEqual(unicode_1, text.Value);

            text.Text = new LocalisedString((unicode_2, non_unicode_2));
            Assert.AreEqual(unicode_2, text.Value);
        }

        [Test]
        public void TestEmptyStringFallback([Values("", null)] string emptyValue)
        {
            const string non_unicode_fallback = "non unicode";
            const string unicode_fallback = "unicode";

            var text = manager.GetLocalisedString(new LocalisedString((unicode_fallback, emptyValue)));

            config.Set(FrameworkSetting.ShowUnicode, false);
            Assert.AreEqual(unicode_fallback, text.Value);

            text = manager.GetLocalisedString(new LocalisedString((emptyValue, non_unicode_fallback)));

            config.Set(FrameworkSetting.ShowUnicode, true);
            Assert.AreEqual(non_unicode_fallback, text.Value);
        }

        private class FakeFrameworkConfigManager : FrameworkConfigManager
        {
            protected override string Filename => null;

            public FakeFrameworkConfigManager() : base(null) { }

            protected override void InitialiseDefaults()
            {
                Set(FrameworkSetting.Locale, "");
                Set(FrameworkSetting.ShowUnicode, true);
            }
        }

        private class FakeStorage : IResourceStore<string>
        {
            public const string LOCALISABLE_STRING_EN = "localised EN";
            public const string LOCALISABLE_STRING_JA = "localised JA";
            public const string LOCALISABLE_STRING_JA_JP = "localised JA-JP";
            public const string LOCALISABLE_FORMAT_STRING_EN = "{0} localised EN";
            public const string LOCALISABLE_FORMAT_STRING_JA = "{0} localised JA";

            private readonly string locale;

            public FakeStorage(string locale)
            {
                this.locale = locale;
            }

            public async Task<string> GetAsync(string name) => await Task.Run(() => Get(name));

            public string Get(string name)
            {
                switch (name)
                {
                    case LOCALISABLE_STRING_EN:
                        switch (locale)
                        {
                            default:
                                return LOCALISABLE_STRING_EN;
                            case "ja":
                                return LOCALISABLE_STRING_JA;
                            case "ja-JP":
                                return LOCALISABLE_STRING_JA_JP;
                        }
                    case LOCALISABLE_FORMAT_STRING_EN:
                        switch (locale)
                        {
                            default:
                                return LOCALISABLE_FORMAT_STRING_EN;
                            case "ja":
                                return LOCALISABLE_FORMAT_STRING_JA;
                        }
                    default:
                        return name;
                }
            }

            public Stream GetStream(string name) => throw new NotSupportedException();

            public void Dispose()
            {
            }
        }
    }
}
