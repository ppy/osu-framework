// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System.Collections.Generic;
using NUnit.Framework;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Input.Bindings;
using osu.Framework.Input.Events;
using osu.Framework.Testing;
using osuTK;
using osuTK.Graphics;
using osuTK.Input;

namespace osu.Framework.Tests.Visual.Input
{
    public class TestSceneKeyBindingInputQueueChange : ManualInputManagerTestScene
    {
        [Test]
        public void TestReleaseDoesNotTriggerWithoutPress()
        {
            TestInputReceptor shownReceptor = null;
            TestInputReceptor hiddenReceptor = null;

            AddStep("set children", () =>
            {
                Child = new TestKeyBindingContainer
                {
                    Children = new[]
                    {
                        shownReceptor = new TestInputReceptor("first")
                        {
                            Anchor = Anchor.Centre,
                            Origin = Anchor.Centre,
                            Size = new Vector2(100),
                            Colour = Color4.LightPink
                        },
                        hiddenReceptor = new TestInputReceptor("second")
                        {
                            Anchor = Anchor.Centre,
                            Origin = Anchor.Centre,
                            Size = new Vector2(100),
                            Alpha = 0,
                            Colour = Color4.LightGreen
                        }
                    }
                };
            });

            AddStep("click-hold shown receptor", () =>
            {
                InputManager.MoveMouseTo(shownReceptor);
                InputManager.PressButton(MouseButton.Left);
            });
            AddStep("hide shown receptor", () => shownReceptor.Hide());
            AddStep("show hidden receptor", () => hiddenReceptor.Show());
            AddStep("release button", () => InputManager.ReleaseButton(MouseButton.Left));

            AddAssert("shown pressed", () => shownReceptor.Pressed);
            AddAssert("shown released", () => shownReceptor.Released);
            AddAssert("hidden not pressed", () => !hiddenReceptor.Pressed);
            AddAssert("hidden not released", () => !hiddenReceptor.Released);
        }

        private class TestInputReceptor : CompositeDrawable, IKeyBindingHandler<TestAction>
        {
            public bool Pressed;
            public bool Released;

            public TestInputReceptor(string name)
            {
                InternalChildren = new Drawable[]
                {
                    new Box { RelativeSizeAxes = Axes.Both },
                    new SpriteText
                    {
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                        Colour = Color4.Black,
                        Text = name
                    }
                };
            }

            public bool OnPressed(KeyBindingPressEvent<TestAction> e)
            {
                Pressed = true;
                return true;
            }

            public void OnReleased(KeyBindingReleaseEvent<TestAction> e)
            {
                Released = true;
            }
        }

        private enum TestAction
        {
            Action1
        }

        private class TestKeyBindingContainer : KeyBindingContainer<TestAction>
        {
            public override IEnumerable<IKeyBinding> DefaultKeyBindings => new[]
            {
                new KeyBinding(InputKey.MouseLeft, TestAction.Action1)
            };
        }
    }
}
