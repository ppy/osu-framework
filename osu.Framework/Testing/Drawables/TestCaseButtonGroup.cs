// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using System.Collections.Generic;
using System.Linq;
using osu.Framework.Extensions.IEnumerableExtensions;

namespace osu.Framework.Testing.Drawables
{
    internal class TestCaseButtonGroup : VisibilityContainer, IHasFilterableChildren
    {
        public IEnumerable<string> FilterTerms => new string[0];

        public bool MatchingFilter
        {
            set { Alpha = value ? 1 : 0; }
        }

        public IEnumerable<IFilterable> FilterableChildren => buttonFlow.Children;

        private readonly FillFlowContainer<TestCaseButton> buttonFlow;

        public readonly TestGroup Group;

        public Type Current
        {
            set
            {
                var contains = Group.TestTypes.Contains(value);
                if (contains) Show();

                buttonFlow.ForEach(btn => btn.Current = btn.TestType == value);
            }
        }

        public TestCaseButtonGroup(Action<Type> loadTest, TestGroup group)
        {
            var tests = group.TestTypes;

            if (tests.Length == 0)
                throw new ArgumentOutOfRangeException(nameof(tests), tests.Length, "Type array must not be empty!");

            Group = group;

            RelativeSizeAxes = Axes.X;
            AutoSizeAxes = Axes.Y;

            Child = buttonFlow = new FillFlowContainer<TestCaseButton>
            {
                Direction = FillDirection.Vertical,
                AutoSizeAxes = Axes.Y,
                RelativeSizeAxes = Axes.X
            };

            bool hasHeader = tests.Length > 1;

            if (hasHeader)
            {
                buttonFlow.Add(new TestCaseHeaderButton(group.Name.Replace("TestCase", ""))
                {
                    Action = ToggleVisibility
                });
            }

            foreach (var test in tests)
            {
                buttonFlow.Add(new TestCaseButton(test)
                {
                    Anchor = Anchor.TopRight,
                    Origin = Anchor.TopRight,
                    Width = hasHeader ? 0.95f : 1,
                    Action = () => loadTest(test)
                });
            }
        }

        public override bool PropagatePositionalInputSubTree => true;

        protected override void PopIn()
        {
            if (Group.TestTypes.Length > 1)
                buttonFlow.ForEach(b => b.Collapsed = false);
        }

        protected override void PopOut()
        {
            if (Group.TestTypes.Length > 1)
                buttonFlow.ForEach(b => b.Collapsed = true);
        }

        public Type SelectFirst()
        {
            if (Group.TestTypes.Length == 1) return Group.TestTypes[0];

            for (int i = 1; i < buttonFlow.Count; i++)
            {
                if (buttonFlow[i].IsPresent)
                    return Group.TestTypes[i - 1];
            }

            return Group.TestTypes[0];
        }
    }
}
