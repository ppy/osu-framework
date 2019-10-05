// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Input.Bindings;
using osu.Framework.Testing;
using osuTK;
using osuTK.Graphics;

namespace osu.Framework.Tests.Visual.Input
{
    public class TestSceneKeyBindingInputQueueChange : ManualInputManagerTestScene
    {
        public TestInputReceptor ShownReceptor { get; private set; }
        public TestInputReceptor HiddenReceptor { get; private set; }

        public void SetChildren()
        {
            Child = new TestKeyBindingContainer
            {
                Children = new[]
                {
                    ShownReceptor = new TestInputReceptor("first")
                    {
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                        Size = new Vector2(100),
                        Colour = Color4.LightPink
                    },
                    HiddenReceptor = new TestInputReceptor("second")
                    {
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                        Size = new Vector2(100),
                        Alpha = 0,
                        Colour = Color4.LightGreen
                    }
                }
            };
        }

        public class TestInputReceptor : CompositeDrawable, IKeyBindingHandler<TestAction>
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

            public bool OnPressed(TestAction action)
            {
                Pressed = true;
                return true;
            }

            public bool OnReleased(TestAction action)
            {
                Released = true;
                return true;
            }
        }

        public enum TestAction
        {
            Action1
        }

        private class TestKeyBindingContainer : KeyBindingContainer<TestAction>
        {
            public override IEnumerable<KeyBinding> DefaultKeyBindings => new[]
            {
                new KeyBinding(InputKey.MouseLeft, TestAction.Action1)
            };
        }
    }
}
