// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Text;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Utils;

namespace osu.Framework.Tests.Visual.Performance
{
    public partial class TestSceneSpriteTextPerformance : PerformanceTestScene
    {
        private int wordLength;
        private int wordsCount;
        private int paragraphsCount;
        private int characterVariance;

        protected override void LoadComplete()
        {
            base.LoadComplete();

            AddLabel("Sprite Texts");
            AddSliderStep("word length", 1, 10, 5, v =>
            {
                wordLength = v;
                recreate();
            });

            AddSliderStep("words count", 1, 1000, 256, v =>
            {
                wordsCount = v;
                recreate();
            });

            AddSliderStep("paragraphs count", 1, 20, 2, v =>
            {
                paragraphsCount = v;
                recreate();
            });

            AddSliderStep("character variance", 1, 26, 26, v =>
            {
                characterVariance = v;
                recreate();
            });
        }

        private void recreate()
        {
            var text = new StringBuilder();

            for (int p = 0; p < paragraphsCount; p++)
            {
                for (int w = 0; w < wordsCount; w++)
                {
                    for (int c = 0; c < wordLength; c++)
                    {
                        char character = (char)RNG.Next('a', 'a' + characterVariance);
                        text.Append(character);
                    }

                    text.Append(' ');
                }

                text.AppendLine();
                text.AppendLine();
            }

            Child = new TextFlowContainer
            {
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                RelativeSizeAxes = Axes.Both,
                Text = text.ToString(),
            };
        }
    }
}
