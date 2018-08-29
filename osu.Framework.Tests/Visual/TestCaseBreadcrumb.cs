// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Graphics;
using osu.Framework.Graphics.UserInterface;
using OpenTK;

namespace osu.Framework.Tests.Visual
{
    public class TestCaseBreadcrumb : FrameworkTestCase
    {
        private readonly string[] testValues =
        {
            "c://",
            "Windows",
            "Sytem32",
            "drivers",
            "etc",
            "hosts"
        };

        public TestCaseBreadcrumb()
        {
            BreadcrumbNavigation navigation;

            Children = new Drawable[]
            {
                navigation = new BreadcrumbNavigation
                {
                    Height = 20,
                    AutoSizeAxes = Axes.X,
                    Origin = Anchor.Centre,
                    Anchor = Anchor.Centre,
                    Spacing = new Vector2(3, 0),
                    Items = testValues
                },
            };

            AddStep("Add range of values", () => navigation.Items = testValues);

            AddAssert("Check if all breadcrumbs have the correct value", () =>
            {
                for (int i = 0; i < navigation.InternalChildren.Count; i++)
                {
                    Breadcrumb child = (Breadcrumb)navigation.InternalChildren[i];

                    if (!child.Current.Value.Equals(testValues[i]))
                        return false;
                }

                return navigation.InternalChildren.Count == testValues.Length ;
            });

            const int testIndex = 2;

            AddStep($"Click on {testValues[testIndex]} one of the elements", () => { navigation.InternalChildren[testIndex].TriggerOnClick(); });

            AddAssert($"Assert that the breadcrumps got truncated to {testValues[testIndex]}", () =>
            {
                for (int i = 0; i < testIndex + 1; i++)
                {
                    Breadcrumb child = (Breadcrumb) navigation.InternalChildren[i];

                    if (!child.Current.Value.Equals(testValues[i]))
                        return false;
                }

                return navigation.InternalChildren.Count == testIndex + 1;
            });
        }
    }
}
