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
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Configuration;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Localisation;
using osu.Framework.Testing;
using osuTK;
using osuTK.Graphics;

namespace osu.Framework.Tests.Visual.Sprites
{
    [System.ComponentModel.Description("various visual SpriteText displays")]
    public class TestSceneSpriteTextScenarios : GridTestScene
    {
        public TestSceneSpriteTextScenarios()
            : base(4, 5)
        {
            Cell(0, 0).Child = new SpriteText { Text = "Basic: Hello world!" };

            Cell(1, 0).Child = new SpriteText
            {
                Text = "Font size = 15",
                Font = new FontUsage(size: 15),
            };

            Cell(2, 0).Child = new SpriteText
            {
                Text = "Colour = green",
                Colour = Color4.Green
            };

            Cell(3, 0).Child = new SpriteText
            {
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                Text = "Rotation = 45",
                Rotation = 45
            };

            Cell(0, 1).Child = new SpriteText
            {
                Text = "Scale = 2",
                Scale = new Vector2(2)
            };

            Cell(1, 1).Child = new CircularContainer
            {
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                Masking = true,
                AutoSizeAxes = Axes.Both,
                Children = new Drawable[]
                {
                    new Box { RelativeSizeAxes = Axes.Both },
                    new SpriteText
                    {
                        Colour = Color4.Red,
                        Text = "||MASKED||"
                    }
                }
            };

            Cell(2, 1).Child = new SpriteText
            {
                Text = "Explicit width",
                Width = 50
            };

            Cell(3, 1).Child = new SpriteText
            {
                Text = "AllowMultiline = false",
                Width = 50,
                AllowMultiline = false
            };

            Cell(0, 2).Child = new Container
            {
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                Width = 50,
                AutoSizeAxes = Axes.Y,
                Child = new SpriteText
                {
                    Text = "Relative size",
                    RelativeSizeAxes = Axes.X
                }
            };

            Cell(1, 2).Child = new Container
            {
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                Width = 50,
                AutoSizeAxes = Axes.Y,
                Child = new SpriteText
                {
                    Text = "GlyphHeight = false",
                    RelativeSizeAxes = Axes.X,
                    UseFullGlyphHeight = false
                }
            };

            Cell(2, 2).Child = new SpriteText
            {
                Text = "Fixed width",
                Font = new FontUsage(fixedWidth: true),
            };

            Cell(3, 2).Child = new SpriteText
            {
                Text = "Scale = -1",
                Y = 20,
                Scale = new Vector2(-1)
            };

            Cell(0, 3).Child = new Container
            {
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                AutoSizeAxes = Axes.Both,
                Children = new Drawable[]
                {
                    new Box { RelativeSizeAxes = Axes.Both },
                    new SpriteText
                    {
                        Text = "Shadow = true",
                        Shadow = true
                    }
                }
            };

            Cell(1, 3).Child = new Container
            {
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                AutoSizeAxes = Axes.Both,
                Children = new Drawable[]
                {
                    new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = Color4.SlateGray
                    },
                    new SpriteText
                    {
                        Text = "Padded (autosize)",
                        Padding = new MarginPadding(10)
                    },
                }
            };

            Cell(2, 3).Child = new Container
            {
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                AutoSizeAxes = Axes.Both,
                Children = new Drawable[]
                {
                    new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = Color4.SlateGray
                    },
                    new SpriteText
                    {
                        Text = "Padded (fixed size)",
                        Width = 50,
                        Padding = new MarginPadding(10)
                    },
                }
            };

            Cell(3, 3).Child = new Container
            {
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                AutoSizeAxes = Axes.Both,
                Children = new Drawable[]
                {
                    new Box { RelativeSizeAxes = Axes.Both },
                    new SpriteText
                    {
                        Text = "Red text + pink shadow",
                        Shadow = true,
                        Colour = Color4.Red,
                        ShadowColour = Color4.Pink.Opacity(0.5f)
                    }
                }
            };

            Cell(0, 4).Child = new NoFixedWidthSpaceText { Text = "No fixed width spaces" };

            Cell(1, 4).Child = new LocalisableTestContainer
            {
                RelativeSizeAxes = Axes.Both,
                Child = new FillFlowContainer
                {
                    RelativeSizeAxes = Axes.Both,
                    Direction = FillDirection.Vertical,
                    Spacing = new Vector2(0, 10),
                    Children = new[]
                    {
                        new SpriteText { Text = FakeStorage.LOCALISABLE_STRING_EN },
                        new SpriteText { Text = new TranslatableString(FakeStorage.LOCALISABLE_STRING_EN, FakeStorage.LOCALISABLE_STRING_EN) },
                    }
                }
            };

            Bindable<string> boundString = new Bindable<string>("bindable: 0");
            int boundStringValue = 0;

            Cell(2, 4).Child = new LocalisableTestContainer
            {
                RelativeSizeAxes = Axes.Both,
                Child = new SpriteText { Current = boundString },
            };

            Scheduler.AddDelayed(() => boundString.Value = $"bindable: {++boundStringValue}", 200, true);
        }

        private class NoFixedWidthSpaceText : SpriteText
        {
            public NoFixedWidthSpaceText()
            {
                Font = new FontUsage(fixedWidth: true);
            }

            protected override char[] FixedWidthExcludeCharacters { get; } = { ' ' };
        }

        private class LocalisableTestContainer : Container
        {
            [Cached]
            private readonly LocalisationManager localisation;

            public LocalisableTestContainer()
            {
                var config = new FakeFrameworkConfigManager();

                localisation = new LocalisationManager(config);
                localisation.AddLanguage("en", new FakeStorage("en"));
                localisation.AddLanguage("ja", new FakeStorage("ja"));

                config.SetValue(FrameworkSetting.Locale, "ja");
            }

            protected override void Dispose(bool isDisposing)
            {
                localisation?.Dispose();
                base.Dispose(isDisposing);
            }
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
                SetDefault(FrameworkSetting.Locale, "ja");
                SetDefault(FrameworkSetting.ShowUnicode, false);
            }
        }

        private class FakeStorage : ILocalisationStore
        {
            public const string LOCALISABLE_STRING_EN = "localised EN";
            public const string LOCALISABLE_STRING_JA = "localised JA";

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
                        }

                    default:
                        return name;
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
