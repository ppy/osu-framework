// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using System.Collections.Generic;
using System.Linq;
using osu.Framework.Bindables;
using osu.Framework.Extensions.IEnumerableExtensions;

namespace osu.Framework.Testing.Drawables
{
    internal class TestGroupButton : Container, IFilterable
    {
        public IEnumerable<string> FilterTerms => headerButton?.FilterTerms ?? Enumerable.Empty<string>();

        public bool MatchingFilter
        {
            set => Alpha = value ? 1 : 0;
        }

        public bool FilteringActive { get; set; }

        private readonly FillFlowContainer<TestButtonBase> buttonFlow;
        private readonly TestButton headerButton;

        private readonly BindableBool expanded = new BindableBool();

        public readonly TestGroup Group;

        public Type Current
        {
            set
            {
                bool contains = Group.TestTypes.Contains(value);

                if (contains)
                    expanded.Value = true;

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
                Action = expanded.Toggle,
            });

            foreach (var test in tests)
            {
                buttonFlow.Add(new TestSubButton(test, 1)
                {
                    Action = () => loadTest(test),
                });
            }
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();
            expanded.BindValueChanged(e => buttonFlow.ForEach(b => b.Collapsed = !e.NewValue), true);
        }

        public override bool PropagatePositionalInputSubTree => true;
    }
}
