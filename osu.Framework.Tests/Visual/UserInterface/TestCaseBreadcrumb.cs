// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.UserInterface;
using System;
using System.Collections.Generic;
using System.Linq;

namespace osu.Framework.Tests.Visual.UserInterface
{
    public class TestCaseBreadcrumb : FrameworkTestScene
    {
        public override IReadOnlyList<Type> RequiredTypes => new[]
        {
            typeof(BreadcrumbNavigation<string>),
            typeof(BasicBreadcrumbNavigation<string>)
        };

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
            BreadcrumbNavigation<string> navigation;

            Children = new Drawable[]
            {
                navigation = new BasicBreadcrumbNavigation<string>
                {
                    Height = 20,
                    Origin = Anchor.Centre,
                    Anchor = Anchor.Centre,
                },
            };

            AddStep("Add range of values", () => navigation.Items.AddRange(testValues));

            AddStep($"Click on the last one of the elements", () =>
            {
                (navigation.InternalChild as CompositeDrawable)?.InternalChildren.Last().Click();
            });

            AddStep($"Click on the first one of the elements", () =>
            {
                (navigation.InternalChild as CompositeDrawable)?.InternalChildren.First().Click();
            });
        }
    }
}
