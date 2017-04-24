// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System.Collections.Generic;
using System.Globalization;
using OpenTK;
using OpenTK.Graphics;
using osu.Framework.Configuration;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Localisation;
using osu.Framework.Testing;

namespace osu.Framework.VisualTests.Tests
{
    internal class TestCaseLocalisation : TestCase
    {
        public override string Description => "Localisation engine";

        private TestLocalisationEngine engine; //keep a reference to avoid GC of the engine

        public override void Reset()
        {
            base.Reset();

            var config = new FakeFrameworkConfigManager();
            engine = new TestLocalisationEngine(config);

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
                    new LocalisedSpriteText
                    {
                        Bindable = engine.GetLocalisedString("localisable"),
                        TextSize = 48,
                        Colour = Color4.White
                    },
                    new LocalisedSpriteText
                    {
                        Bindable = engine.GetUnicodePreference("Unicode on", "Unicode off"),
                        TextSize = 48,
                        Colour = Color4.White
                    },
                    new LocalisedSpriteText
                    {
                        Bindable = engine.GetUnicodePreference(null, "I miss unicode"),
                        TextSize = 48,
                        Colour = Color4.White
                    },
                }
            });

            AddStep("English", () => config.Set(FrameworkConfig.Locale, "en"));
            AddStep("Japanese", () => config.Set(FrameworkConfig.Locale, "ja"));
            AddStep("Simplified Chinese", () => config.Set(FrameworkConfig.Locale, "zh-Hans"));
            AddToggleStep("ShowUnicode", b => config.Set(FrameworkConfig.ShowUnicode, b));
        }

        private class FakeFrameworkConfigManager : FrameworkConfigManager
        {
            protected override string Filename => null;

            public FakeFrameworkConfigManager() : base(null) { }

            protected override void InitialiseDefaults()
            {
                Set(FrameworkConfig.Locale, "");
                Set(FrameworkConfig.ShowUnicode, false);
            }
        }

        private class TestLocalisationEngine : LocalisationEngine
        {
            public TestLocalisationEngine(FrameworkConfigManager config) : base(config) { }

            public override IEnumerable<string> SupportedLocales => new[] { "en", "ja", "zh-Hans" };

            protected override string GetLocalised(string key) => $"{key} in {CultureInfo.CurrentCulture.EnglishName}";
        }
    }
}
