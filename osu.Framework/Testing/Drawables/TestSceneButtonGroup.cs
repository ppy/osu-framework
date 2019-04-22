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
    internal class TestSceneButtonGroup : VisibilityContainer, IHasFilterableChildren
    {
        public IEnumerable<string> FilterTerms => headerButton?.FilterTerms ?? Enumerable.Empty<string>();

        public bool MatchingFilter
        {
            set => Alpha = value ? 1 : 0;
        }

        public bool FilteringActive { get; set; }

        public IEnumerable<IFilterable> FilterableChildren => buttonFlow.Children;

        private readonly FillFlowContainer<TestSceneButton> buttonFlow;
        private readonly TestSceneHeaderButton headerButton;

        public readonly TestGroup Group;

        public Type Current
        {
            set
            {
                var contains = Group.TestTypes.Contains(value);
                if (contains) Show();

                buttonFlow.ForEach(btn => btn.Current = btn.TestType == value);
                if (headerButton != null)
                    headerButton.Current = contains;
            }
        }

        public TestSceneButtonGroup(Action<Type> loadTest, TestGroup group)
        {
            var tests = group.TestTypes;

            if (tests.Length == 0)
                throw new ArgumentOutOfRangeException(nameof(tests), tests.Length, "Type array must not be empty!");

            Group = group;

            RelativeSizeAxes = Axes.X;
            AutoSizeAxes = Axes.Y;

            Child = buttonFlow = new FillFlowContainer<TestSceneButton>
            {
                Direction = FillDirection.Vertical,
                AutoSizeAxes = Axes.Y,
                RelativeSizeAxes = Axes.X
            };

            bool hasHeader = tests.Length > 1;

            if (hasHeader)
                buttonFlow.Add(headerButton = new TestSceneHeaderButton(group.Name)
                {
                    Action = ToggleVisibility
                });

            foreach (var test in tests)
            {
                buttonFlow.Add(new TestSceneSubButton(test, hasHeader ? 1 : 0)
                {
                    Action = () => loadTest(test)
                });
            }
        }

        public override bool PropagatePositionalInputSubTree => true;

        protected override void PopIn() => buttonFlow.ForEach(b => b.Collapsed = false);

        protected override void PopOut()
        {
            if (headerButton != null)
                buttonFlow.ForEach(b => b.Collapsed = true);
        }
    }
}
