// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using NUnit.Framework;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Testing;
using OpenTK;

namespace osu.Framework.Tests.Visual
{
    public class TestCaseCircularContainerSizing : TestCase
    {
        [Test]
        public void TestLateSizing()
        {
            HookedContainer container;
            CircularContainer circular;
            Child = container = new HookedContainer
            {
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                Child = circular = new CircularContainer
                {
                    RelativeSizeAxes = Axes.Both,
                    Masking = true,
                    Child = new Box { RelativeSizeAxes = Axes.Both }
                }
            };

            container.OnUpdate = () => onUpdate(container);
            container.OnUpdateAfterChildren = () => onUpdateAfterChildren(container, circular);

            bool hasCorrectCornerRadius = false;

            AddAssert("has correct corner radius", () => hasCorrectCornerRadius);

            void onUpdate(Container parent)
            {
                // Suppose the parent has some arbitrary size prior to the child being updated...
                parent.Size = Vector2.One;
            }

            void onUpdateAfterChildren(Container parent, CircularContainer nested)
            {
                // ... and the size of the parent is changed to the desired value after the child has been updated
                // This could happen just by ordering of events in the hierarchy, regardless of auto or relative size
                parent.Size = new Vector2(200);
                hasCorrectCornerRadius = nested.CornerRadius == 100;
            }
        }

        private class HookedContainer : Container
        {
            public new Action OnUpdate;
            public Action OnUpdateAfterChildren;

            protected override void Update()
            {
                base.Update();
                OnUpdate?.Invoke();
            }

            protected override void UpdateAfterChildren()
            {
                OnUpdateAfterChildren?.Invoke();
                base.UpdateAfterChildren();
            }
        }
    }
}
