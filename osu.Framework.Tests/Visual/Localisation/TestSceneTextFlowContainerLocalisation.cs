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
using osu.Framework.Allocation;
using osu.Framework.Configuration;
using osu.Framework.Extensions.LocalisationExtensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Localisation;

namespace osu.Framework.Tests.Visual.Localisation
{
    [TestFixture]
    public class TestSceneTextFlowContainerLocalisation : FrameworkTestScene
    {
        private FrameworkConfigManager configManager { get; set; }

        [Cached]
        private LocalisationManager manager;

        protected override IReadOnlyDependencyContainer CreateChildDependencies(IReadOnlyDependencyContainer parent)
        {
            var dependencies = new DependencyContainer(parent);

            configManager = parent.Get<FrameworkConfigManager>();
            dependencies.Cache(manager = new LocalisationManager(configManager));

            return dependencies;
        }

        private const string rank = "rank";
        private const string rank_lost = "rank_lost";

        private const string rank_default = "{0} achieved rank #{1} on {2} ({3})";
        private const string rank_lost_default = "{0} has lost first place on {1} ({2})";

        private const string simple = "simple";

        [BackgroundDependencyLoader]
        private void load()
        {
            // strings sourced from osu-web crowdin (https://crowdin.com/translate/osu-web/)
            manager.AddLanguage("en", new TestLocalisationStore("en", new Dictionary<string, string>
            {
                [rank] = rank_default,
                [rank_lost] = rank_lost_default,
                [simple] = "simple english",
            }));

            manager.AddLanguage("fr", new TestLocalisationStore("fr", new Dictionary<string, string>
            {
                [rank] = "{0} a atteint le rang #{1} sur {2} ({3})",
                [rank_lost] = "{0} a perdu la première place sur {1} ({2})",
                [simple] = "simple french",
            }));

            manager.AddLanguage("tr", new TestLocalisationStore("tr", new Dictionary<string, string>
            {
                [rank] = "{0} {2} ({3}) beatmapinde #{1} sıralamaya ulaştı",
                [rank_lost] = "{0} {1} ({2}) beatmapinde birinciliği kaybetti",
                [simple] = "simple turkish",
            }));
        }

        protected override void Dispose(bool isDisposing)
        {
            manager?.Dispose();
            base.Dispose(isDisposing);
        }

        [Test]
        public void TestTextFlowLocalisation()
        {
            Container textFlowParent = null;
            TextFlowContainer textFlowContainer = null;

            AddStep("create text flow", () => Child = textFlowParent = new Container
            {
                RelativeSizeAxes = Axes.X,
                AutoSizeAxes = Axes.Y,
                Origin = Anchor.Centre,
                Anchor = Anchor.Centre,
                Children = new Drawable[]
                {
                    new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = Colour4.FromHex("#333")
                    },
                    textFlowContainer = new TextFlowContainer(text => text.Font = FrameworkFont.Condensed)
                    {
                        RelativeSizeAxes = Axes.X,
                        AutoSizeAxes = Axes.Y,
                        Origin = Anchor.Centre,
                        Anchor = Anchor.Centre,
                    },
                }
            });
            AddStep("add text", () =>
            {
                const string player = "spaceman_atlas";
                var rankAchieved = 12_345_678.ToLocalisableString("N0");
                var beatmap = new RomanisableString(
                    "ELFENSJóN - ASH OF ROUGE (HeTo's Normal)",
                    "ELFENSJoN - ASH OF ROUGE (HeTo's Normal)");
                const string mode = "osu!";

                textFlowContainer.AddText(new TranslatableString(rank, rank_default, player, rankAchieved, beatmap, mode));
                textFlowContainer.NewParagraph();
                textFlowContainer.AddText(new TranslatableString(rank_lost, rank_lost_default, player, beatmap, mode), text =>
                {
                    text.Font = FontUsage.Default;
                    text.Colour = Colour4.Red;
                });
            });

            AddStep("change locale to en", () => configManager.SetValue(FrameworkSetting.Locale, "en"));
            AddStep("change locale to fr", () => configManager.SetValue(FrameworkSetting.Locale, "fr"));
            AddStep("change locale to tr", () => configManager.SetValue(FrameworkSetting.Locale, "tr"));

            AddToggleStep("toggle romanisation", romanised => configManager.SetValue(FrameworkSetting.ShowUnicode, romanised));

            AddSliderStep("change text flow width", 0, 1f, 1f, width =>
            {
                if (textFlowParent != null)
                    textFlowParent.Width = width;
            });
        }

        [Test]
        public void TestChangeLocalisationBeforeAsyncLoad()
        {
            AddStep("change locale to en", () => configManager.SetValue(FrameworkSetting.Locale, "en"));

            TextFlowContainer textFlowContainer = null;
            ITextPart textPart = null;

            AddStep("create text flow", () =>
            {
                textFlowContainer = new TextFlowContainer(text => text.Font = FrameworkFont.Condensed)
                {
                    RelativeSizeAxes = Axes.X,
                    AutoSizeAxes = Axes.Y,
                    Origin = Anchor.Centre,
                    Anchor = Anchor.Centre,
                };
            });

            AddStep("Add text", () => textPart = textFlowContainer.AddText(new TranslatableString(simple, "fallback")));

            AddStep("Add text flow to hierarchy", () => Child = textFlowContainer);

            AddAssert("Ensure parts are correct", () =>
                textPart.Drawables.OfType<SpriteText>().ElementAtOrDefault(0)?.Text == "simple " &&
                textPart.Drawables.OfType<SpriteText>().ElementAtOrDefault(1)?.Text == "english"
            );
        }

        [Test]
        public void TestChangeLocalisationAfterAsyncLoad()
        {
            AddStep("change locale to en", () => configManager.SetValue(FrameworkSetting.Locale, "en"));

            TextFlowContainer textFlowContainer = null;
            ITextPart textPart = null;

            AddStep("create text flow", () =>
            {
                textFlowContainer = new TextFlowContainer(text => text.Font = FrameworkFont.Condensed)
                {
                    RelativeSizeAxes = Axes.X,
                    AutoSizeAxes = Axes.Y,
                    Origin = Anchor.Centre,
                    Anchor = Anchor.Centre,
                };
            });

            AddStep("Add text", () => textPart = textFlowContainer.AddText(new TranslatableString(simple, "fallback")));

            AddStep("Load async ahead of time", () => LoadComponent(textFlowContainer));

            // Parts are created eagerly during async load to alleviate synchronous overhead.
            AddAssert("Ensure parts are correct", () =>
                textPart.Drawables.OfType<SpriteText>().ElementAtOrDefault(0)?.Text == "simple " &&
                textPart.Drawables.OfType<SpriteText>().ElementAtOrDefault(1)?.Text == "english");

            AddStep("change locale to fr", () => configManager.SetValue(FrameworkSetting.Locale, "fr"));

            AddStep("Add text flow to hierarchy", () => Child = textFlowContainer);

            AddAssert("Ensure parts are correct", () =>
                textPart.Drawables.OfType<SpriteText>().ElementAtOrDefault(0)?.Text == "simple " &&
                textPart.Drawables.OfType<SpriteText>().ElementAtOrDefault(1)?.Text == "french"
            );
        }

        [Test]
        public void TestChangeLocalisationBeforeAfterLoadComplete()
        {
            AddStep("change locale to en", () => configManager.SetValue(FrameworkSetting.Locale, "en"));

            TextFlowContainer textFlowContainer = null;
            ITextPart textPart = null;

            AddStep("create text flow", () =>
            {
                textFlowContainer = new TextFlowContainer(text => text.Font = FrameworkFont.Condensed)
                {
                    RelativeSizeAxes = Axes.X,
                    AutoSizeAxes = Axes.Y,
                    Origin = Anchor.Centre,
                    Anchor = Anchor.Centre,
                };
            });

            AddStep("Add text", () => textPart = textFlowContainer.AddText(new TranslatableString(simple, "fallback")));

            AddStep("Add text flow to hierarchy", () => Child = textFlowContainer);

            // Parts are created eagerly during async load to alleviate synchronous overhead.
            AddAssert("Ensure parts are correct", () =>
                textPart.Drawables.OfType<SpriteText>().ElementAtOrDefault(0)?.Text == "simple " &&
                textPart.Drawables.OfType<SpriteText>().ElementAtOrDefault(1)?.Text == "english");

            AddStep("change locale to fr", () => configManager.SetValue(FrameworkSetting.Locale, "fr"));

            AddAssert("Ensure parts are correct", () =>
                textPart.Drawables.OfType<SpriteText>().ElementAtOrDefault(0)?.Text == "simple " &&
                textPart.Drawables.OfType<SpriteText>().ElementAtOrDefault(1)?.Text == "french"
            );
        }

        private class TestLocalisationStore : ILocalisationStore
        {
            public CultureInfo EffectiveCulture { get; }

            private readonly IDictionary<string, string> translations;

            public TestLocalisationStore(string locale, IDictionary<string, string> translations)
            {
                EffectiveCulture = new CultureInfo(locale);

                this.translations = translations;
            }

            public string Get(string key) => translations.TryGetValue(key, out string value) ? value : null;

            public Task<string> GetAsync(string key, CancellationToken cancellationToken = default) => Task.FromResult(Get(key));

            public Stream GetStream(string name) => throw new NotSupportedException();

            public IEnumerable<string> GetAvailableResources() => Array.Empty<string>();

            public void Dispose()
            {
            }
        }
    }
}
