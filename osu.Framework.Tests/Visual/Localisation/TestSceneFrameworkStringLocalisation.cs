// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Configuration;
using osu.Framework.Extensions.LocalisationExtensions;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Localisation.Strings;

namespace osu.Framework.Tests.Visual.Localisation
{
    public partial class TestSceneFrameworkStringLocalisation : LocalisationTestScene
    {
        [BackgroundDependencyLoader]
        private void load()
        {
            Manager.AddLanguage("en-US", new TestLocalisationStore("en-US", new Dictionary<string, string>
            {
                // no translations, use the fallback value
            }));

            Manager.AddLanguage("en-GB", new TestLocalisationStore("en-GB", new Dictionary<string, string>
            {
                [WindowModeStrings.Windowed.GetTranslationKey()] = "Windowed",
                [WindowModeStrings.Borderless.GetTranslationKey()] = "Borderless",
                [WindowModeStrings.Fullscreen.GetTranslationKey()] = "Full screen",
            }));

            Manager.AddLanguage("hr-HR", new TestLocalisationStore("hr-HR", new Dictionary<string, string>
            {
                [WindowModeStrings.Windowed.GetTranslationKey()] = "Prozor",
                [WindowModeStrings.Borderless.GetTranslationKey()] = "Bez ruba",
                [WindowModeStrings.Fullscreen.GetTranslationKey()] = "Puni zaslon",
            }));
        }

        [Test]
        public void TestDropdown()
        {
            AddStep("add dropdown", () =>
            {
                Child = new BasicDropdown<WindowMode>
                {
                    Width = 200,
                    Items = Enum.GetValues<WindowMode>()
                };
            });

            SetLocale("en-US");
            SetLocale("en-GB");
            SetLocale("hr-HR");
        }
    }
}
