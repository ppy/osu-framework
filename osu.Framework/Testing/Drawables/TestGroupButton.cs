// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Localisation;
using System.Collections.Generic;
using System.Linq;
using osu.Framework.Extensions.IEnumerableExtensions;

namespace osu.Framework.Testing.Drawables
{
    internal class TestGroupButton : VisibilityContainer, IHasFilterableChildren
    {
        public IEnumerable<LocalisableString> FilterTerms => headerButton?.FilterTerms ?? Enumerable.Empty<LocalisableString>();

        public bool MatchingFilter
        {
            set => Alpha = value ? 1 : 0;
        }

        public bool FilteringActive { get; set; }

        public IEnumerable<IFilterable> FilterableChildren => buttonFlow.Children;

        private readonly FillFlowContainer<TestButtonBase> buttonFlow;
        private readonly TestButton headerButton;

        public readonly TestGroup Group;

        public Type Current
        {
            set
            {
                bool contains = Group.TestTypes.Contains(value);
                if (contains) Show();

                buttonFlow.ForEach(btn => btn.Current = btn.TestType == value);
                headerButton.Current = contains;
            }
        }

        public TestGroupButton(Action<Type> loadTest, TestGroup group)
        {
            var tests = group.TestTypes;

            if (tests.Length == 0)
                throw new ArgumentOutOfRangeException(nameof(group), tests.Length, "Type array must not be empty!");

            Group = group;

            RelativeSizeAxes = Axes.X;
            AutoSizeAxes = Axes.Y;

            Child = buttonFlow = new FillFlowContainer<TestButtonBase>
            {
                Direction = FillDirection.Vertical,
                AutoSizeAxes = Axes.Y,
                RelativeSizeAxes = Axes.X
            };

            buttonFlow.Add(headerButton = new TestButton(group.Name)
            {
                Action = ToggleVisibility
            });

            foreach (var test in tests)
            {
                buttonFlow.Add(new TestSubButton(test, 1)
                {
                    Action = () => loadTest(test)
                });
            }
        }

        public override bool PropagatePositionalInputSubTree => true;

        protected override void PopIn() => buttonFlow.ForEach(b => b.Collapsed = false);

        protected override void PopOut() => buttonFlow.ForEach(b => b.Collapsed = true);
    }
}
