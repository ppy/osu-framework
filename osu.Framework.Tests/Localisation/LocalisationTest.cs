// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

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
        private LocalisationEngine engine;

        [SetUp]
        public void Setup()
        {
            config = new FakeFrameworkConfigManager();
            engine = new LocalisationEngine(config);
            engine.AddLanguage("en", new FakeStorage("en"));
        }

        [Test]
        public void TestNotLocalised()
        {
            const string not_localised = "not localised.";
            const string not_localised_2 = "also not localised";

            var localisable = new LocalisableString(string.Empty, false);
            var localisedText = engine.GetLocalisedBindable(localisable);

            localisable.Text.Value = not_localised;
            Assert.AreEqual(not_localised, localisedText.Value);

            localisable.Text.Value = not_localised_2;
            Assert.AreEqual(not_localised_2, localisedText.Value);
        }

        [Test]
        public void TestLocalised()
        {
            engine.AddLanguage("ja-JP", new FakeStorage("ja-JP"));

            var localisable = new LocalisableString(string.Empty);
            var localisedText = engine.GetLocalisedBindable(localisable);

            localisable.Text.Value = FakeStorage.LOCALISABLE_STRING_EN;
            Assert.AreEqual(FakeStorage.LOCALISABLE_STRING_EN, localisedText.Value);

            config.Set(FrameworkSetting.Locale, "ja-JP");
            Assert.AreEqual(FakeStorage.LOCALISABLE_STRING_JA_JP, localisedText.Value);
        }

        [Test]
        public void TestLocalisationFallback()
        {
            engine.AddLanguage("ja", new FakeStorage("ja"));

            config.Set(FrameworkSetting.Locale, "ja-JP");

            var localisable = new LocalisableString(string.Empty);
            var localisedText = engine.GetLocalisedBindable(localisable);

            localisable.Text.Value = FakeStorage.LOCALISABLE_STRING_EN;
            Assert.AreEqual(FakeStorage.LOCALISABLE_STRING_JA, localisedText.Value);
        }

        [Test]
        public void TestFormatted()
        {
            const string to_format = "this {0} {1} formatted";
            const string arg_0 = "has";
            const string arg_1 = "been";

            string expectedResult = string.Format(to_format, arg_0, arg_1);

            var formattable = new LocalisableString(to_format, false, arg_0, arg_1);
            var formattedText = engine.GetLocalisedBindable(formattable);

            Assert.AreEqual(expectedResult, formattedText.Value);
        }

        [Test]
        public void TestFormattedAndLocalised()
        {
            const string arg_0 = "formatted";

            string expectedResult = string.Format(FakeStorage.LOCALISABLE_FORMAT_STRING_JA, arg_0);

            engine.AddLanguage("ja", new FakeStorage("ja"));
            config.Set(FrameworkSetting.Locale, "ja");

            var formattable = new LocalisableString(FakeStorage.LOCALISABLE_FORMAT_STRING_EN, arg_0);
            var formattedText = engine.GetLocalisedBindable(formattable);

            Assert.AreEqual(expectedResult, formattedText.Value);
        }

        [Test]
        public void TestUnicodePreference()
        {
            const string non_unicode = "non unicode";
            const string unicode = "unicode";

            var text = engine.GetUnicodeBindable(unicode, non_unicode);

            config.Set(FrameworkSetting.ShowUnicode, true);
            Assert.AreEqual(unicode, text.Value);

            config.Set(FrameworkSetting.ShowUnicode, false);
            Assert.AreEqual(non_unicode, text.Value);
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
