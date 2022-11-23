// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Extensions.LocalisationExtensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Localisation;

namespace osu.Framework.Tests.Visual.Localisation
{
    [TestFixture]
    public class TestSceneTextFlowContainerLocalisation : LocalisationTestScene
    {
        private const string rank = "rank";
        private const string rank_lost = "rank_lost";

        private const string rank_default = "{0} achieved rank #{1} on {2} ({3})";
        private const string rank_lost_default = "{0} has lost first place on {1} ({2})";

        private const string simple = "simple";

        [BackgroundDependencyLoader]
        private void load()
        {
            // strings sourced from osu-web crowdin (https://crowdin.com/translate/osu-web/)
            Manager.AddLanguage("en", new TestLocalisationStore("en", new Dictionary<string, string>
            {
                [rank] = rank_default,
                [rank_lost] = rank_lost_default,
                [simple] = "simple english",
            }));

            Manager.AddLanguage("fr", new TestLocalisationStore("fr", new Dictionary<string, string>
            {
                [rank] = "{0} a atteint le rang #{1} sur {2} ({3})",
                [rank_lost] = "{0} a perdu la première place sur {1} ({2})",
                [simple] = "simple french",
            }));

            Manager.AddLanguage("tr", new TestLocalisationStore("tr", new Dictionary<string, string>
            {
                [rank] = "{0} {2} ({3}) beatmapinde #{1} sıralamaya ulaştı",
                [rank_lost] = "{0} {1} ({2}) beatmapinde birinciliği kaybetti",
                [simple] = "simple turkish",
            }));
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

            SetLocale("en");
            SetLocale("fr");
            SetLocale("tr");

            AddToggleStep("toggle romanisation", romanised => ShowUnicode.Value = romanised);

            AddSliderStep("change text flow width", 0, 1f, 1f, width =>
            {
                if (textFlowParent != null)
                    textFlowParent.Width = width;
            });
        }

        [Test]
        public void TestChangeLocalisationBeforeAsyncLoad()
        {
            SetLocale("en");

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
            SetLocale("en");

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

            SetLocale("fr");

            AddStep("Add text flow to hierarchy", () => Child = textFlowContainer);

            AddAssert("Ensure parts are correct", () =>
                textPart.Drawables.OfType<SpriteText>().ElementAtOrDefault(0)?.Text == "simple " &&
                textPart.Drawables.OfType<SpriteText>().ElementAtOrDefault(1)?.Text == "french"
            );
        }

        [Test]
        public void TestChangeLocalisationBeforeAfterLoadComplete()
        {
            SetLocale("en");

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

            SetLocale("fr");

            AddAssert("Ensure parts are correct", () =>
                textPart.Drawables.OfType<SpriteText>().ElementAtOrDefault(0)?.Text == "simple " &&
                textPart.Drawables.OfType<SpriteText>().ElementAtOrDefault(1)?.Text == "french"
            );
        }
    }
}
