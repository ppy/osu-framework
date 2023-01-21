// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Extensions.LocalisationExtensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Localisation;
using osuTK.Graphics;

namespace osu.Framework.Tests.Visual.Localisation
{
    public partial class TestSceneCompositeChunkLocalisation : LocalisationTestScene
    {
        private const string rank = "rank";
        private const string rank_default = "[0] achieved rank #[1] on [2] ([3])";
        private const string simple = "simple";

        private CompositeTextChunk<SpriteText>? chunk;
        private Color4 color = Color4.Cyan;

        [BackgroundDependencyLoader]
        private void load()
        {
            // strings sourced from osu-web crowdin (https://crowdin.com/translate/osu-web/)
            Manager.AddLanguage("en", new TestLocalisationStore("en", new Dictionary<string, string>
            {
                [rank] = rank_default,
                [simple] = "simple english",
            }));

            Manager.AddLanguage("fr", new TestLocalisationStore("fr", new Dictionary<string, string>
            {
                [rank] = "[0] a atteint le rang #[1] sur [2] ([3])",
                [simple] = "simple french",
            }));

            Manager.AddLanguage("tr", new TestLocalisationStore("tr", new Dictionary<string, string>
            {
                [rank] = "[0] [2] ([3]) beatmapinde #[1] sıralamaya ulaştı",
                [simple] = "simple turkish",
            }));
        }

        [Test]
        public void TestTextFlowLocalisation()
        {
            Container? textFlowParent = null;
            TextFlowContainer? textFlowContainer = null;

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

                chunk = textFlowContainer!.AddText(new TranslatableString(rank, rank_default), new (LocalisableString, Action<SpriteText>)[]
                {
                    (player, applyColors),
                    (rankAchieved, applyColors),
                    (beatmap, applyColors),
                    (mode, applyColors)
                }, text =>
                {
                    text.Font = FontUsage.Default;
                    text.Colour = Colour4.LimeGreen;
                });
            });

            SetLocale("en");
            SetLocale("fr");
            SetLocale("tr");

            AddToggleStep("toggle romanisation", romanised => ShowUnicode.Value = romanised);

            AddSliderStep("color of inner parts", 0f, 1f, 0.5f, c =>
            {
                color = new Color4(1f, c, c, 1f);
                updColors();
            });

            AddSliderStep("change text flow width", 0, 1f, 1f, width =>
            {
                if (textFlowParent != null)
                    textFlowParent.Width = width;
            });
        }

        private void applyColors(SpriteText text) => text.Colour = color;

        private void updColors()
        {
            if (chunk == null)
                return;

            foreach (var subPart in chunk.Children)
            {
                foreach (var drawable in subPart.Drawables)
                {
                    drawable.Colour = color;
                }
            }
        }
    }
}
