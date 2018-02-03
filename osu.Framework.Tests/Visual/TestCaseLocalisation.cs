// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Globalization;
using System.IO;
using osu.Framework.Allocation;
using osu.Framework.Configuration;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Framework.IO.Stores;
using osu.Framework.Localisation;
using osu.Framework.Testing;
using OpenTK;
using OpenTK.Graphics;

namespace osu.Framework.Tests.Visual
{
    public class TestCaseLocalisation : TestCase
    {
        private DependencyContainer dependencies;
        protected override IReadOnlyDependencyContainer CreateLocalDependencies(IReadOnlyDependencyContainer parent) => dependencies = new DependencyContainer(parent);

        private readonly LocalisationEngine engine;

        public TestCaseLocalisation()
        {
            var config = new FakeFrameworkConfigManager();
            engine = new LocalisationEngine(config);

            engine.AddLanguage("en", new FakeStorage());
            engine.AddLanguage("zh-CHS", new FakeStorage());
            engine.AddLanguage("ja", new FakeStorage());

            AddStep("English", () => config.Set(FrameworkSetting.Locale, "en"));
            AddStep("Japanese", () => config.Set(FrameworkSetting.Locale, "ja"));
            AddStep("Simplified Chinese", () => config.Set(FrameworkSetting.Locale, "zh-CHS"));
            AddToggleStep("ShowUnicode", b => config.Set(FrameworkSetting.ShowUnicode, b));
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            dependencies.Cache(engine);

            Add(new FillFlowContainer<SpriteText>
            {
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                Direction = FillDirection.Vertical,
                Spacing = new Vector2(0, 10),
                Padding = new MarginPadding(10),
                AutoSizeAxes = Axes.Both,
                Children = new[]
                {
                    new SpriteText
                    {
                        Text = "Not localisable",
                        TextSize = 48,
                        Colour = Color4.White
                    },
                    new SpriteText
                    {
                        LocalisableText = new LocalisableString("localisable", LocalisationType.Localised),
                        TextSize = 48,
                        Colour = Color4.White
                    },
                    new SpriteText
                    {
                        LocalisableText = new LocalisableString("localisableformat", LocalisationType.Localised | LocalisationType.Formatted, args: "localisable & formattable"),
                        TextSize = 48,
                        Colour = Color4.White
                    },
                    new SpriteText
                    {
                        LocalisableText = new LocalisableString("Unicode on", LocalisationType.UnicodePreference, "Unicode off"),
                        TextSize = 48,
                        Colour = Color4.White
                    },
                    new SpriteText
                    {
                        LocalisableText = new LocalisableString(null, LocalisationType.UnicodePreference, "I miss unicode"),
                        TextSize = 48,
                        Colour = Color4.White
                    },
                    new SpriteText
                    {
                        LocalisableText = new LocalisableString("{0}", LocalisationType.Formatted, args: DateTime.Now),
                        TextSize = 48,
                        Colour = Color4.White
                    },
                }
            });
        }

        private class FakeFrameworkConfigManager : FrameworkConfigManager
        {
            protected override string Filename => null;

            public FakeFrameworkConfigManager() : base(null) { }

            protected override void InitialiseDefaults()
            {
                Set(FrameworkSetting.Locale, "");
                Set(FrameworkSetting.ShowUnicode, false);
            }
        }

        private class FakeStorage : IResourceStore<string>
        {
            public string Get(string name)
            {
                switch (name)
                {
                    case "localisable":
                        return $"{name} in {CultureInfo.CurrentCulture.EnglishName}";
                    case "localisableformat":
                        return $"{{0}} in locale {CultureInfo.CurrentCulture.Name}";
                    default:
                        throw new ArgumentException();
                }
            }
            public Stream GetStream(string name) => throw new NotSupportedException();
        }
    }
}
