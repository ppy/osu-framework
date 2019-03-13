// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Testing;
using osuTK;
using osuTK.Graphics;

namespace osu.Framework.Tests.Visual.Bindable
{
    public class TestCaseBindableAutoUnbinding : TestCase
    {
        public TestCaseBindableAutoUnbinding()
        {
            var drawable1 = new TestExposedBindableDrawable();
            var drawable2 = new TestExposedBindableDrawable();
            var drawable3 = new TestExposedBindableDrawable(true);
            var drawable4 = new TestExposedBindableDrawable();

            AddStep("add drawables", () =>
            {
                drawable1.Bindable = new Bindable<int>();
                drawable2.Bindable = drawable1.Bindable.GetBoundCopy();
                drawable3.Bindable = drawable1.Bindable; // an example of a bad usage of bindables
                drawable4.Bindable = drawable1.Bindable.GetBoundCopy();

                Child = new FillFlowContainer
                {
                    RelativeSizeAxes = Axes.Both,
                    Direction = FillDirection.Vertical,
                    Spacing = new Vector2(5),
                    Children = new Drawable[]
                    {
                        drawable1,
                        drawable2,
                        drawable3,
                        drawable4,
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
