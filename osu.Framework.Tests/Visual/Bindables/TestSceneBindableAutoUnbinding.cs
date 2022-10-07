// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osuTK;
using osuTK.Graphics;

namespace osu.Framework.Tests.Visual.Bindables
{
    public class TestSceneBindableAutoUnbinding : FrameworkTestScene
    {
        [Test]
        public void TestBindableAutoUnbindingAssign()
        {
            TestExposedBindableDrawable drawable1 = null, drawable2 = null, drawable3 = null, drawable4 = null;

            AddStep("add drawables", () =>
            {
                Child = new FillFlowContainer
                {
                    RelativeSizeAxes = Axes.Both,
                    Direction = FillDirection.Vertical,
                    Spacing = new Vector2(5),
                    Children = new Drawable[]
                    {
                        drawable1 = new TestExposedBindableDrawable { Bindable = new Bindable<int>() },
                        drawable2 = new TestExposedBindableDrawable { Bindable = drawable1.Bindable.GetBoundCopy() },
                        drawable3 = new TestExposedBindableDrawable(true) { Bindable = drawable1.Bindable }, // an example of a bad usage of bindables
                        drawable4 = new TestExposedBindableDrawable { Bindable = drawable1.Bindable.GetBoundCopy() },
                    }
                };
            });

            AddStep("attempt value transfer", () => drawable1.Bindable.Value = 10);

            AddAssert("transfer 1-2 completed", () => drawable1.Bindable.Value == drawable2.Bindable.Value);
            AddAssert("transfer 1-3 completed", () => drawable1.Bindable.Value == drawable3.Bindable.Value);
            AddAssert("transfer 1-4 completed", () => drawable1.Bindable.Value == drawable4.Bindable.Value);

            AddStep("expire child 4", () => drawable4.Expire());

            AddStep("attempt value transfer", () => drawable1.Bindable.Value = 20);

            AddAssert("transfer 1-2 completed", () => drawable1.Bindable.Value == drawable2.Bindable.Value);
            AddAssert("transfer 1-3 completed", () => drawable1.Bindable.Value == drawable3.Bindable.Value);
            AddAssert("transfer 1-4 skipped", () => drawable1.Bindable.Value != drawable4.Bindable.Value);

            AddStep("expire child 3", () => drawable3.Expire());

            AddStep("attempt value transfer", () => drawable1.Bindable.Value = 10);

            // fails due to drawable3 being expired/disposed with a direct reference to drawable1's bindable.
            AddAssert("transfer 1-2 fails", () => drawable1.Bindable.Value != drawable2.Bindable.Value);
        }

        [Test]
        public void TestBindableAutoUnbindingResolution()
        {
            TestResolvedBindableDrawable drawable1 = null, drawable2 = null, drawable3 = null, drawable4 = null;

            AddStep("add drawables", () =>
            {
                Child = new BindableExposingFillFlowContainer
                {
                    RelativeSizeAxes = Axes.Both,
                    Direction = FillDirection.Vertical,
                    Spacing = new Vector2(5),
                    Children = new Drawable[]
                    {
                        drawable1 = new TestResolvedBindableDrawable(),
                        drawable2 = new TestResolvedBindableDrawable(),
                        drawable3 = new TestResolvedBindableDrawable(true), // an example of a bad usage of bindables
                        drawable4 = new TestResolvedBindableDrawable(),
                    }
                };
            });

            AddStep("attempt value transfer", () => drawable1.Bindable.Value = 10);

            AddAssert("transfer 1-2 completed", () => drawable1.Bindable.Value == drawable2.Bindable.Value);
            AddAssert("transfer 1-3 completed", () => drawable1.Bindable.Value == drawable3.Bindable.Value);
            AddAssert("transfer 1-4 completed", () => drawable1.Bindable.Value == drawable4.Bindable.Value);

            AddStep("expire child 4", () => drawable4.Expire());

            AddStep("attempt value transfer", () => drawable1.Bindable.Value = 20);

            AddAssert("transfer 1-2 completed", () => drawable1.Bindable.Value == drawable2.Bindable.Value);
            AddAssert("transfer 1-3 completed", () => drawable1.Bindable.Value == drawable3.Bindable.Value);
            AddAssert("transfer 1-4 skipped", () => drawable1.Bindable.Value != drawable4.Bindable.Value);

            AddStep("expire child 3", () => drawable3.Expire());

            AddStep("attempt value transfer", () => drawable1.Bindable.Value = 10);

            // fails due to drawable3 being expired/disposed with a direct reference to drawable1's bindable.
            AddAssert("transfer 1-2 fails", () => drawable1.Bindable.Value != drawable2.Bindable.Value);
        }

        public class BindableExposingFillFlowContainer : FillFlowContainer
        {
            [Cached]
#pragma warning disable IDE0052 // Unread private member
            private Bindable<int> bindable = new Bindable<int>();
#pragma warning restore IDE0052 //
        }

        public class TestResolvedBindableDrawable : TestExposedBindableDrawable
        {
            private readonly bool badActor;

            public TestResolvedBindableDrawable(bool badActor = false)
                : base(badActor)
            {
                this.badActor = badActor;
            }

            [BackgroundDependencyLoader]
            private void load(Bindable<int> parentBindable)
            {
                if (badActor)
                    Bindable = parentBindable;
                else
                {
                    Bindable = new Bindable<int>();
                    Bindable.BindTo(parentBindable);
                }
            }
        }

        public class TestExposedBindableDrawable : CompositeDrawable
        {
            public Bindable<int> Bindable;

            private readonly SpriteText spriteText;

            public TestExposedBindableDrawable(bool badActor = false)
            {
                Size = new Vector2(50);
                InternalChildren = new Drawable[]
                {
                    new Box
                    {
                        Colour = badActor ? Color4.Red : Color4.Green,
                        RelativeSizeAxes = Axes.Both,
                    },
                    spriteText = new SpriteText
                    {
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                    }
                };
            }

            protected override void LoadComplete()
            {
                base.LoadComplete();
                Bindable.BindValueChanged(val => spriteText.Text = val.NewValue.ToString(), true);
            }
        }
    }
}
