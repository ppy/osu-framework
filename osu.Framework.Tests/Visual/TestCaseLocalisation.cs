// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Globalization;
using System.IO;
using NUnit.Framework;
using System.Threading.Tasks;
using osu.Framework.Allocation;
using osu.Framework.Configuration;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Framework.IO.Stores;
using osu.Framework.Localisation;
using osu.Framework.Testing;
using OpenTK.Graphics;
using System.Collections.Generic;

namespace osu.Framework.Tests.Visual
{
    public class TestCaseLocalisation : TestCase
    {
        public override IReadOnlyList<Type> RequiredTypes => new[]
        {
            typeof(LocalisationEngine),
            typeof(LocalisableString),
        };

        private readonly FrameworkConfigManager config = new FakeFrameworkConfigManager();
        private readonly LocalisationEngine engine;
        private SpriteText sprite;

        public TestCaseLocalisation()
        {
            engine = new LocalisationEngine(config);
            engine.AddLanguage("en", new FakeStorage(config));
            engine.AddLanguage("zh-CHS", new FakeStorage(config));
            engine.AddLanguage("ja", new FakeStorage(config));
        }

        [SetUp]
        public new void SetupTest()
        {
            Clear();
            Add(new FillFlowContainer<SpriteText>
            {
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                Direction = FillDirection.Vertical,
                Padding = new MarginPadding(10),
                AutoSizeAxes = Axes.Both,
                Child = sprite = new CustomEngineSpriteText(engine)
                {
                    Text = string.Empty,
                    TextSize = 48,
                    Colour = Color4.White
                },
            });
        }

        [Test]
        public void TestNeverLocalised()
        {
            const string never_localised_text = "this is and will not be localised.";
            AddStep("set never localised", () => sprite.Text = never_localised_text);
            AddAssert("text correct", () => sprite.Text == never_localised_text);

            AddStep("change existing", () =>
            {
                sprite.LocalisableText.Type.Value = LocalisationType.Localised;
                sprite.LocalisableText.Text.Value = "localised now?";
            });
            AddAssert("text didn't change", () => sprite.Text == never_localised_text);

            const string test2 = "different text";
            AddStep("manual text setter", () => sprite.Text = test2);
            AddAssert("text changed", () => sprite.Text == test2);
        }

        [Test]
        public void TestUnlocalised()
        {
            const string unlocalised_text = "not localised (for now)";
            AddStep("make not localised", () => sprite.LocalisableText = unlocalised_text);
            AddAssert("text correct", () => sprite.Text == unlocalised_text);

            // this should never be done (recreate the LocalisableString when changing 2+ properties)
            // this just makes sure nothing crashes even if you do
            var formattedDate = DateTime.Now;
            AddStep("change existing", () =>
            {
                sprite.LocalisableText.Type.Value = LocalisationType.Localised | LocalisationType.Formatted;
                sprite.LocalisableText.Text.Value = "new {0} {1}";
                sprite.LocalisableText.Args.Value = new object[] { "string value! Time:", formattedDate };
            });
            AddAssert("text changed", () => sprite.Text == $"new string value! Time: {formattedDate}");
        }

        [Test]
        public void TestLocalised()
        {
            AddStep("make localised", () =>
            {
                sprite.LocalisableText = new LocalisableString("localised");
            });
            changeLanguage("english", "en");
            AddAssert("text localised", () => sprite.Text == "localised in English");
        }

        [Test]
        [SetCulture("ja")]
        public void TestFormatted()
        {
            var formattedDate = DateTime.Now;
            AddStep("make formatted", () =>
            {
                sprite.LocalisableText = new LocalisableString("{0}", LocalisationType.Formatted, formattedDate);
            });
            changeLanguage("japanese", "ja");
            AddAssert("text formatted correctly", () => sprite.Text == formattedDate.ToString(CultureInfo.CurrentCulture));

            const string formattable_string = "{0}";
            AddStep("fail formatting on purpose", () =>
            {
                // no args for formatting, this will throw internally but should not crash / fail the test
                sprite.LocalisableText = new LocalisableString(formattable_string, LocalisationType.Formatted);
            });
            AddAssert("text reverted", () => sprite.Text == formattable_string);
        }

        [Test]
        public void TestFormattedLocalised()
        {
            AddStep("Make localised & formatted", () =>
            {
                sprite.LocalisableText = new LocalisableString("localisedformat", LocalisationType.Localised | LocalisationType.Formatted, "formatted");
            });
            changeLanguage("chinese", "zh-CHS");
            AddAssert("text localised & formatted", () => sprite.Text == "formatted in locale zh-CHS");
        }

        [Test]
        public void TestUnicodePreference()
        {
            IBindable<string> bindable;
            const string unicode = "this is the unicode text!";
            const string non_unicode = "this is the non-unicode alternative!";

            AddStep("setup unicode", () =>
            {
                bindable = engine.GetUnicodeBindable(unicode, non_unicode);
                bindable.ValueChanged += newText => sprite.Text = newText;
                sprite.Text = bindable.Value;
            });

            AddStep("show unicode", () => config.Set(FrameworkSetting.ShowUnicode, true));
            AddAssert("check for unicode", () => sprite.Text == unicode);
            AddStep("show non-unicode", () => config.Set(FrameworkSetting.ShowUnicode, false));
            AddAssert("check for non-unicode", () => sprite.Text == non_unicode);
        }

        private void changeLanguage(string language, string locale)
        {
            AddStep($"language: {language}", () => config.Set(FrameworkSetting.Locale, locale));
        }

        private class CustomEngineSpriteText : SpriteText
        {
            [Cached(Type = typeof(ILocalisationEngine))]
            private readonly LocalisationEngine engine;

            public CustomEngineSpriteText(LocalisationEngine engine)
            {
                this.engine = engine;
            }
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
            private readonly FrameworkConfigManager config;

            public FakeStorage(FrameworkConfigManager config)
            {
                this.config = config;
            }

            public async Task<string> GetAsync(string name) => await Task.Run(() => Get(name));

            public string Get(string name)
            {
                string locale = config.Get<string>(FrameworkSetting.Locale);

                switch (name)
                {
                    case "localised":
                        return $"{name} in {new CultureInfo(locale).EnglishName}";
                    case "localisedformat":
                        return $"{{0}} in locale {locale}";
                    case "no Unicode":
                        return "non-Unicode localised!";
                    case "yes Unicode":
                        return "Unicode localised!";
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
