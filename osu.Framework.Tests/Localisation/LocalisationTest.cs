// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using osu.Framework.Configuration;
using osu.Framework.Extensions.LocalisationExtensions;
using osu.Framework.Localisation;

namespace osu.Framework.Tests.Localisation
{
    [TestFixture]
    public class LocalisationTest
    {
        private const string default_locale = "";

        private FrameworkConfigManager config;
        private LocalisationManager manager;

        [SetUp]
        public void Setup()
        {
            config = new FakeFrameworkConfigManager();
            manager = new LocalisationManager(config);
            manager.AddLanguage("en", new FakeStorage("en"));
        }

        [TearDown]
        public void Teardown()
        {
            manager?.Dispose();
            config?.Dispose();
        }

        [Test]
        public void TestNoLanguagesAdded()
        {
            // reinitialise without the default language
            manager.Dispose();
            manager = new LocalisationManager(config);

            var localisedText = manager.GetLocalisedBindableString(new TranslatableString(FakeStorage.LOCALISABLE_STRING_EN, FakeStorage.LOCALISABLE_STRING_EN));
            Assert.AreEqual(FakeStorage.LOCALISABLE_STRING_EN, localisedText.Value);
        }

        [Test]
        public void TestConfigSettingRetainedWhenAddingNewLanguage()
        {
            config.SetValue(FrameworkSetting.Locale, "ja-JP");

            // ensure that adding a new language which doesn't match the user's choice doesn't cause the configuration value to get reset.
            manager.AddLanguage("po", new FakeStorage("po-OP"));
            Assert.AreEqual("ja-JP", config.Get<string>(FrameworkSetting.Locale));

            var localisedText = manager.GetLocalisedBindableString(new TranslatableString(FakeStorage.LOCALISABLE_STRING_EN, FakeStorage.LOCALISABLE_STRING_EN));
            Assert.AreEqual(FakeStorage.LOCALISABLE_STRING_EN, localisedText.Value);

            // ensure that if the user's selection is added in a further AddLanguage call, the manager correctly translates strings.
            manager.AddLanguage("ja-JP", new FakeStorage("ja-JP"));
            Assert.AreEqual(FakeStorage.LOCALISABLE_STRING_JA_JP, localisedText.Value);
        }

        [Test]
        public void TestConfigSettingRetainedWhenAddingLocaleMappings()
        {
            config.SetValue(FrameworkSetting.Locale, "ja-JP");

            // ensure that adding a new language which doesn't match the user's choice doesn't cause the configuration value to get reset.
            manager.AddLocaleMappings(new[]
            {
                new LocaleMapping("po", new FakeStorage("po-OP")),
                new LocaleMapping("wa", new FakeStorage("wa-NG"))
            });

            Assert.AreEqual("ja-JP", config.Get<string>(FrameworkSetting.Locale));

            var localisedText = manager.GetLocalisedBindableString(new TranslatableString(FakeStorage.LOCALISABLE_STRING_EN, FakeStorage.LOCALISABLE_STRING_EN));
            Assert.AreEqual(FakeStorage.LOCALISABLE_STRING_EN, localisedText.Value);

            // ensure that if the user's selection is added in a further AddLanguage call, the manager correctly translates strings.
            manager.AddLocaleMappings(new[]
            {
                new LocaleMapping("ja-JP", new FakeStorage("ja-JP"))
            });

            Assert.AreEqual(FakeStorage.LOCALISABLE_STRING_JA_JP, localisedText.Value);
        }

        [Test]
        public void TestNotLocalised()
        {
            manager.AddLanguage("ja-JP", new FakeStorage("ja-JP"));
            config.SetValue(FrameworkSetting.Locale, "ja-JP");

            var localisedText = manager.GetLocalisedBindableString(FakeStorage.LOCALISABLE_STRING_EN);

            Assert.AreEqual(FakeStorage.LOCALISABLE_STRING_EN, localisedText.Value);

            localisedText.Text = FakeStorage.LOCALISABLE_STRING_JA;

            Assert.AreEqual(FakeStorage.LOCALISABLE_STRING_JA, localisedText.Value);
        }

        [Test]
        public void TestLocalised()
        {
            manager.AddLanguage("ja-JP", new FakeStorage("ja-JP"));

            var localisedText = manager.GetLocalisedBindableString(new TranslatableString(FakeStorage.LOCALISABLE_STRING_EN, FakeStorage.LOCALISABLE_STRING_EN));

            Assert.AreEqual(FakeStorage.LOCALISABLE_STRING_EN, localisedText.Value);

            config.SetValue(FrameworkSetting.Locale, "ja-JP");
            Assert.AreEqual(FakeStorage.LOCALISABLE_STRING_JA_JP, localisedText.Value);
        }

        [Test]
        public void TestLocalisationFallback()
        {
            manager.AddLanguage("ja", new FakeStorage("ja"));

            config.SetValue(FrameworkSetting.Locale, "ja-JP");

            var localisedText = manager.GetLocalisedBindableString(new TranslatableString(FakeStorage.LOCALISABLE_STRING_EN, FakeStorage.LOCALISABLE_STRING_EN));

            Assert.AreEqual(FakeStorage.LOCALISABLE_STRING_JA, localisedText.Value);
        }

        [Test]
        public void TestFormatted()
        {
            const string to_format = "this {0} {1} formatted";
            const string arg_0 = "has";
            const string arg_1 = "been";

            string expectedResult = string.Format(to_format, arg_0, arg_1);

            var formattedText = manager.GetLocalisedBindableString(string.Format(to_format, arg_0, arg_1));

            Assert.AreEqual(expectedResult, formattedText.Value);
        }

        [Test]
        public void TestFormattedInterpolation()
        {
            const string arg_0 = "formatted";

            manager.AddLanguage("ja-JP", new FakeStorage("ja"));
            config.SetValue(FrameworkSetting.Locale, "ja-JP");

            string expectedResult = string.Format(FakeStorage.LOCALISABLE_FORMAT_STRING_JA, arg_0);

            var formattedText = manager.GetLocalisedBindableString(new TranslatableString(FakeStorage.LOCALISABLE_FORMAT_STRING_EN,
                interpolation: $"The {arg_0} fallback should only matches argument count"));

            Assert.AreEqual(expectedResult, formattedText.Value);
        }

        [Test]
        public void TestFormattedAndLocalised()
        {
            const string arg_0 = "formatted";

            string expectedResult = string.Format(FakeStorage.LOCALISABLE_FORMAT_STRING_JA, arg_0);

            manager.AddLanguage("ja", new FakeStorage("ja"));
            config.SetValue(FrameworkSetting.Locale, "ja");

            var formattedText = manager.GetLocalisedBindableString(new TranslatableString(FakeStorage.LOCALISABLE_FORMAT_STRING_EN, FakeStorage.LOCALISABLE_FORMAT_STRING_EN, arg_0));

            Assert.AreEqual(expectedResult, formattedText.Value);
        }

        [Test]
        public void TestFormattedAndLocalisedUsingInterpolate()
        {
            var formattable = LocalisableString.Format("{0:0.00%}", 0.1234);
            var translatable1 = new TranslatableString(FakeStorage.LOCALISABLE_STRING_EN, FakeStorage.LOCALISABLE_STRING_EN);
            var translatable2 = new TranslatableString(FakeStorage.LOCALISABLE_FORMAT_STRING_EN, FakeStorage.LOCALISABLE_FORMAT_STRING_EN, formattable);

            manager.AddLanguage("ja", new FakeStorage("ja"));
            config.SetValue(FrameworkSetting.Locale, "ja");

            var formattedText = manager.GetLocalisedBindableString(LocalisableString.Interpolate($"{translatable1} -> {translatable2}"));

            Assert.AreEqual("localised JA -> 12.34% localised JA", formattedText.Value);
        }

        [Test]
        public void TestFormattedAndLocalisedUsingFormat()
        {
            var formattable = LocalisableString.Format("{0:0.00%}", 0.1234);
            var translatable1 = new TranslatableString(FakeStorage.LOCALISABLE_STRING_EN, FakeStorage.LOCALISABLE_STRING_EN);
            var translatable2 = new TranslatableString(FakeStorage.LOCALISABLE_FORMAT_STRING_EN, FakeStorage.LOCALISABLE_FORMAT_STRING_EN, formattable);

            manager.AddLanguage("ja", new FakeStorage("ja"));
            config.SetValue(FrameworkSetting.Locale, "ja");

            var formattedText = manager.GetLocalisedBindableString(LocalisableString.Format("{0} -> {1}", translatable1, translatable2));

            Assert.AreEqual("localised JA -> 12.34% localised JA", formattedText.Value);
        }

        [Test]
        public void TestNumberCultureAware()
        {
            const double value = 1.23;

            manager.AddLanguage("fr", new FakeStorage("fr"));
            config.SetValue(FrameworkSetting.Locale, "fr");

            string expectedResult = string.Format(new CultureInfo("fr"), FakeStorage.LOCALISABLE_NUMBER_FORMAT_STRING_FR, value);
            Assert.AreEqual("number 1,23 FR", expectedResult); // FR uses comma for decimal point.

            var formattedText = manager.GetLocalisedBindableString(new TranslatableString(FakeStorage.LOCALISABLE_NUMBER_FORMAT_STRING_EN, null, value));

            Assert.AreEqual(expectedResult, formattedText.Value);
        }

        [Test]
        public void TestStorageNotFound()
        {
            manager.AddLanguage("ja", new FakeStorage("ja"));
            config.SetValue(FrameworkSetting.Locale, "ja");

            const string expected_fallback = "fallback string";

            var formattedText = manager.GetLocalisedBindableString(new TranslatableString("no such key", expected_fallback));

            Assert.AreEqual(expected_fallback, formattedText.Value);
        }

        [Test]
        public void TestUnicodePreference()
        {
            const string non_unicode = "non unicode";
            const string unicode = "unicode";

            var text = manager.GetLocalisedBindableString(new RomanisableString(unicode, non_unicode));

            config.SetValue(FrameworkSetting.ShowUnicode, true);
            Assert.AreEqual(unicode, text.Value);

            config.SetValue(FrameworkSetting.ShowUnicode, false);
            Assert.AreEqual(non_unicode, text.Value);
        }

        [Test]
        public void TestUnicodeStringChanging()
        {
            const string non_unicode_1 = "non unicode 1";
            const string non_unicode_2 = "non unicode 2";
            const string unicode_1 = "unicode 1";
            const string unicode_2 = "unicode 2";

            var text = manager.GetLocalisedBindableString(new RomanisableString(unicode_1, non_unicode_1));

            config.SetValue(FrameworkSetting.ShowUnicode, false);
            Assert.AreEqual(non_unicode_1, text.Value);

            text.Text = new RomanisableString(unicode_1, non_unicode_2);
            Assert.AreEqual(non_unicode_2, text.Value);

            config.SetValue(FrameworkSetting.ShowUnicode, true);
            Assert.AreEqual(unicode_1, text.Value);

            text.Text = new RomanisableString(unicode_2, non_unicode_2);
            Assert.AreEqual(unicode_2, text.Value);
        }

        [Test]
        public void TestEmptyStringFallback([Values("", null)] string emptyValue)
        {
            const string non_unicode_fallback = "non unicode";
            const string unicode_fallback = "unicode";

            var text = manager.GetLocalisedBindableString(new RomanisableString(unicode_fallback, emptyValue));

            config.SetValue(FrameworkSetting.ShowUnicode, false);
            Assert.AreEqual(unicode_fallback, text.Value);

            text = manager.GetLocalisedBindableString(new RomanisableString(emptyValue, non_unicode_fallback));

            config.SetValue(FrameworkSetting.ShowUnicode, true);
            Assert.AreEqual(non_unicode_fallback, text.Value);
        }

        /// <summary>
        /// This tests the <see cref="LocalisableFormattableString"/>, which allows for formatting <see cref="IFormattable"/>s,
        /// without necessarily being in a <see cref="TranslatableString"/> which requires keys mapping to strings from localistaion stores.
        /// </summary>
        [Test]
        public void TestLocalisableFormattableString()
        {
            manager.AddLanguage("fr", new FakeStorage("fr"));

            var dateTime = new DateTime(1);
            const string format = "MMM yyyy";

            var text = manager.GetLocalisedBindableString(dateTime.ToLocalisableString(format));

            Assert.AreEqual("Jan 0001", text.Value);

            config.SetValue(FrameworkSetting.Locale, "fr");
            Assert.AreEqual("janv. 0001", text.Value);
        }

        [Test]
        public void TestCaseTransformableString()
        {
            const string localisable_string_en_title_case = "Localised EN";

            config.SetValue(FrameworkSetting.Locale, "en");

            var uppercasedText = manager.GetLocalisedBindableString(new TranslatableString(FakeStorage.LOCALISABLE_STRING_EN, FakeStorage.LOCALISABLE_STRING_EN).ToUpper());
            var titleText = manager.GetLocalisedBindableString(new TranslatableString(FakeStorage.LOCALISABLE_STRING_EN, FakeStorage.LOCALISABLE_STRING_EN).ToTitle());

            Assert.AreEqual("LOCALISED EN", uppercasedText.Value);
            Assert.AreEqual(localisable_string_en_title_case, titleText.Value);
        }

        [Test]
        public void TestCaseTransformableStringNonEnglishCultureCasing()
        {
            manager.AddLanguage("tr", new FakeStorage("tr"));

            var uppercasedText = manager.GetLocalisedBindableString(new TranslatableString(FakeStorage.LOCALISABLE_STRING_EN, FakeStorage.LOCALISABLE_STRING_EN).ToUpper());
            var lowercasedText = manager.GetLocalisedBindableString(new TranslatableString(FakeStorage.LOCALISABLE_STRING_EN, FakeStorage.LOCALISABLE_STRING_EN).ToLower());
            var titleCasedText = manager.GetLocalisedBindableString(new TranslatableString(FakeStorage.LOCALISABLE_STRING_EN, FakeStorage.LOCALISABLE_STRING_EN).ToTitle());
            var sentenceCasedText = manager.GetLocalisedBindableString(new TranslatableString(FakeStorage.LOCALISABLE_STRING_EN, FakeStorage.LOCALISABLE_STRING_EN).ToSentence());

            config.SetValue(FrameworkSetting.Locale, "en");

            Assert.AreEqual("LOCALISED EN", uppercasedText.Value);
            Assert.AreEqual("localised en", lowercasedText.Value);
            Assert.AreEqual("Localised EN", titleCasedText.Value);
            Assert.AreEqual("Localised EN", sentenceCasedText.Value);

            config.SetValue(FrameworkSetting.Locale, "tr");

            Assert.AreEqual("LOCALİSED TR (İ/I)", uppercasedText.Value);
            Assert.AreEqual("localised tr (i/ı)", lowercasedText.Value);
            Assert.AreEqual("Localised TR (İ/I)", titleCasedText.Value);
            Assert.AreEqual("Localised TR (i/I)", sentenceCasedText.Value);
        }

        [Test]
        public void TestTranslatableEvaluatingLocalisableFormattableString()
        {
            const string key = FakeStorage.LOCALISABLE_NUMBER_FORMAT_STRING_EN;

            manager.AddLanguage("fr", new FakeStorage("fr"));

            var arg = LocalisableString.Format("{0:0.00%}", 0.1234);
            var text = manager.GetLocalisedBindableString(new TranslatableString(key, key, arg));

            Assert.AreEqual("number 12.34% EN", text.Value);

            config.SetValue(FrameworkSetting.Locale, "fr");

            Assert.AreEqual("number 12,34% FR", text.Value);
        }

        [Test]
        public void TestTranslatableEvaluatingRomanisableString()
        {
            const string key = FakeStorage.LOCALISABLE_FORMAT_STRING_EN;

            var text = manager.GetLocalisedBindableString(new TranslatableString(key, key, new RomanisableString("unicode", "romanised")));

            Assert.AreEqual("unicode localised EN", text.Value);

            config.SetValue(FrameworkSetting.ShowUnicode, false);

            Assert.AreEqual("romanised localised EN", text.Value);
        }

        [Test]
        public void TestTranslatableEvaluatingTranslatableString()
        {
            const string key = FakeStorage.LOCALISABLE_FORMAT_STRING_EN;
            const string nested_key = FakeStorage.LOCALISABLE_STRING_EN;

            manager.AddLanguage("ja", new FakeStorage("ja"));

            var text = manager.GetLocalisedBindableString(new TranslatableString(key, key, new TranslatableString(nested_key, nested_key)));

            Assert.AreEqual("localised EN localised EN", text.Value);

            config.SetValue(FrameworkSetting.Locale, "ja");

            Assert.AreEqual("localised JA localised JA", text.Value);
        }

        [Test]
        public void TestTranslatableEvaluatingComplexString()
        {
            const string key = FakeStorage.LOCALISABLE_COMPLEX_FORMAT_STRING_EN;
            const string nested_key = FakeStorage.LOCALISABLE_NUMBER_FORMAT_STRING_EN;

            manager.AddLanguage("fr", new FakeStorage("fr"));

            var text = manager.GetLocalisedBindableString(new TranslatableString(key, key,
                LocalisableString.Interpolate($"{12.34:0.00}"),
                new TranslatableString(nested_key, nested_key, LocalisableString.Interpolate($"{0.9876:0.00%}")),
                new TranslatableString(nested_key, nested_key, new RomanisableString("unicode", "romanised"))));

            Assert.AreEqual("number 12.34 with number 98.76% EN and number unicode EN EN", text.Value);

            config.SetValue(FrameworkSetting.Locale, "fr");

            Assert.AreEqual("number 12,34 with number 98,76% FR and number unicode FR FR", text.Value);

            config.SetValue(FrameworkSetting.ShowUnicode, false);

            Assert.AreEqual("number 12,34 with number 98,76% FR and number romanised FR FR", text.Value);

            config.SetValue(FrameworkSetting.Locale, "en");

            Assert.AreEqual("number 12.34 with number 98.76% EN and number romanised EN EN", text.Value);
        }

        [Test]
        public void TestFormatComplexStringUsesFallbackFormatWithTranslatedParts()
        {
            const string nested_key = FakeStorage.LOCALISABLE_NUMBER_FORMAT_STRING_EN;

            manager.AddLanguage("fr", new FakeStorage("fr"));

            var text = manager.GetLocalisedBindableString(LocalisableString.Format("{0} / {1} / {2}",
                LocalisableString.Interpolate($"{12.34:0.00}"),
                new TranslatableString(nested_key, nested_key, LocalisableString.Interpolate($"{0.9876:0.00%}")),
                new TranslatableString(nested_key, nested_key, new RomanisableString("unicode", "romanised"))));

            Assert.AreEqual("12.34 / number 98.76% EN / number unicode EN", text.Value);

            config.SetValue(FrameworkSetting.Locale, "fr");

            Assert.AreEqual("12,34 / number 98,76% FR / number unicode FR", text.Value);

            config.SetValue(FrameworkSetting.ShowUnicode, false);

            Assert.AreEqual("12,34 / number 98,76% FR / number romanised FR", text.Value);

            config.SetValue(FrameworkSetting.Locale, "en");

            Assert.AreEqual("12.34 / number 98.76% EN / number romanised EN", text.Value);
        }

        [Test]
        public void TestInvalidLocaleWhileRunning()
        {
            string localeBefore = config.Get<string>(FrameworkSetting.Locale);
            config.SetValue(FrameworkSetting.Locale, "invalid locale");
            string localeAfter = config.Get<string>(FrameworkSetting.Locale);

            Assert.That(localeAfter, Is.EqualTo(localeBefore));
        }

        [Test]
        public void TestInvalidLocaleDuringStartup()
        {
            // dispose the old manager so it doesn't change the config value.
            manager.Dispose();
            // simulate an invalid locale being set on startup.
            config.SetValue(FrameworkSetting.Locale, "invalid locale");
            manager = new LocalisationManager(config);
            // add a language to trigger a locale update
            manager.AddLanguage("en", new FakeStorage("en"));
            // the manager should reset the locale to the default value if it can't parse the locale.
            Assert.That(config.Get<string>(FrameworkSetting.Locale), Is.EqualTo(default_locale));
        }

        /// <summary>
        /// Tests a possible edge case where both the old and new locales could be invalid in the 'revert to previous value' logic in <see cref="LocalisationManager.updateLocale"/>.
        /// </summary>
        [Test]
        public void TestInvalidLocaleToInvalid()
        {
            // dispose the old manager so it doesn't change the config value.
            manager.Dispose();
            // simulate an invalid locale being set on startup.
            config.SetValue(FrameworkSetting.Locale, "invalid locale");
            manager = new LocalisationManager(config);
            // set another invalid locale to generate a ValueChanged event with both locales invalid. (possible infinite back-and-forth between the two locales)
            config.SetValue(FrameworkSetting.Locale, "another invalid locale");
            // add a language to make sure everything still works.
            manager.AddLanguage("en", new FakeStorage("en"));
            Assert.That(config.Get<string>(FrameworkSetting.Locale), Is.EqualTo(default_locale));
        }

        private class FakeFrameworkConfigManager : FrameworkConfigManager
        {
            protected override string Filename => null;

            public FakeFrameworkConfigManager()
                : base(null)
            {
            }

            protected override void InitialiseDefaults()
            {
                SetDefault(FrameworkSetting.Locale, default_locale);
                SetDefault(FrameworkSetting.ShowUnicode, true);
            }
        }

        private class FakeStorage : ILocalisationStore
        {
            public const string LOCALISABLE_STRING_EN = "localised EN";
            public const string LOCALISABLE_STRING_JA = "localised JA";
            public const string LOCALISABLE_STRING_JA_JP = "localised JA-JP";
            public const string LOCALISABLE_STRING_TR = "localised TR (i/I)";
            public const string LOCALISABLE_FORMAT_STRING_EN = "{0} localised EN";
            public const string LOCALISABLE_FORMAT_STRING_JA = "{0} localised JA";
            public const string LOCALISABLE_NUMBER_FORMAT_STRING_EN = "number {0} EN";
            public const string LOCALISABLE_NUMBER_FORMAT_STRING_FR = "number {0} FR";
            public const string LOCALISABLE_COMPLEX_FORMAT_STRING_EN = "number {0} with {1} and {2} EN";
            public const string LOCALISABLE_COMPLEX_FORMAT_STRING_FR = "number {0} with {1} and {2} FR";

            public CultureInfo EffectiveCulture { get; }

            private readonly string locale;

            public FakeStorage(string locale)
            {
                this.locale = locale;
                EffectiveCulture = new CultureInfo(locale);
            }

            public async Task<string> GetAsync(string name, CancellationToken cancellationToken = default) =>
                await Task.Run(() => Get(name), cancellationToken).ConfigureAwait(false);

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

                            case "tr":
                                return LOCALISABLE_STRING_TR;
                        }

                    case LOCALISABLE_FORMAT_STRING_EN:
                        switch (locale)
                        {
                            default:
                                return LOCALISABLE_FORMAT_STRING_EN;

                            case "ja":
                                return LOCALISABLE_FORMAT_STRING_JA;
                        }

                    case LOCALISABLE_NUMBER_FORMAT_STRING_EN:
                        switch (locale)
                        {
                            default:
                                return LOCALISABLE_NUMBER_FORMAT_STRING_EN;

                            case "fr":
                                return LOCALISABLE_NUMBER_FORMAT_STRING_FR;
                        }

                    case LOCALISABLE_COMPLEX_FORMAT_STRING_EN:
                        switch (locale)
                        {
                            default:
                                return LOCALISABLE_COMPLEX_FORMAT_STRING_EN;

                            case "fr":
                                return LOCALISABLE_COMPLEX_FORMAT_STRING_FR;
                        }

                    default:
                        return null;
                }
            }

            public Stream GetStream(string name) => throw new NotSupportedException();

            public void Dispose()
            {
            }

            public IEnumerable<string> GetAvailableResources() => Enumerable.Empty<string>();
        }
    }
}
