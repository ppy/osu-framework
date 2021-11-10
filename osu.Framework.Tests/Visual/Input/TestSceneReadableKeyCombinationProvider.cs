// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Input;
using osu.Framework.Input.Bindings;
using osu.Framework.Input.Events;
using osu.Framework.Input.States;
using osuTK;
using osuTK.Graphics;

namespace osu.Framework.Tests.Visual.Input
{
    public class TestSceneReadableKeyCombinationProvider : FrameworkTestScene
    {
        private readonly InputKey[][] keyboard =
        {
            new[]
            {
                InputKey.Grave, InputKey.Number1, InputKey.Number2, InputKey.Number3, InputKey.Number4, InputKey.Number5, InputKey.Number6,
                InputKey.Number7, InputKey.Number8, InputKey.Number9, InputKey.Number0, InputKey.Minus, InputKey.Plus, InputKey.BackSpace
            },
            new[]
            {
                InputKey.Tab, InputKey.Q, InputKey.W, InputKey.E, InputKey.R, InputKey.T, InputKey.Y, InputKey.U,
                InputKey.I, InputKey.O, InputKey.P, InputKey.BracketLeft, InputKey.BracketRight, InputKey.BackSlash
            },
            new[]
            {
                InputKey.CapsLock, InputKey.A, InputKey.S, InputKey.D, InputKey.F, InputKey.G, InputKey.H,
                InputKey.J, InputKey.K, InputKey.L, InputKey.Semicolon, InputKey.Quote, InputKey.Enter
            },
            new[]
            {
                InputKey.LShift, InputKey.NonUSBackSlash, InputKey.Z, InputKey.X, InputKey.C, InputKey.V, InputKey.B,
                InputKey.N, InputKey.M, InputKey.Comma, InputKey.Period, InputKey.Slash, InputKey.RShift
            },
            new[]
            {
                InputKey.LControl, InputKey.LSuper, InputKey.LAlt, InputKey.Space, InputKey.RAlt, InputKey.RSuper, InputKey.Menu, InputKey.RControl
            },
            new[]
            {
                InputKey.Control, InputKey.Super, InputKey.Alt
            }
        };

        protected override void LoadComplete()
        {
            base.LoadComplete();

            var fillFlow = new FillFlowContainer
            {
                Anchor = Anchor.TopCentre,
                Origin = Anchor.TopCentre,
                Width = 750,
                AutoSizeAxes = Axes.Y,
                Direction = FillDirection.Vertical,
            };

            foreach (var row in keyboard)
            {
                var fillRow = new FillFlowContainer
                {
                    RelativeSizeAxes = Axes.X,
                    AutoSizeAxes = Axes.Y,
                    Direction = FillDirection.Horizontal,
                };

                float keyWidth = 1f / row.Length;

                foreach (var key in row)
                {
                    fillRow.Add(new Key(key)
                    {
                        Anchor = Anchor.TopLeft,
                        Origin = Anchor.TopLeft,
                        Height = 50,
                        RelativeSizeAxes = Axes.X,
                        Width = keyWidth,
                    });
                }

                fillFlow.Add(fillRow);
            }

            Child = new FillFlowContainer
            {
                Anchor = Anchor.TopCentre,
                Origin = Anchor.TopCentre,
                Children = new Drawable[] { fillFlow, new PressedKeyCombinationDisplay() },
                Spacing = new Vector2(5),
                Direction = FillDirection.Vertical,
                AutoSizeAxes = Axes.Both,
            };
        }

        public class Key : CompositeDrawable
        {
            [Resolved]
            private ReadableKeyCombinationProvider readableKeyCombinationProvider { get; set; }

            private readonly Box box;
            private readonly SpriteText text;
            private readonly KeyCombination keyCombination;

            public Key(InputKey key)
            {
                keyCombination = new KeyCombination(key);

                Padding = new MarginPadding(3);
                InternalChildren = new Drawable[]
                {
                    box = new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = Color4.DarkGray,
                        Alpha = 0.6f,
                    },
                    text = new SpriteText
                    {
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                    },
                };
            }

            protected override void LoadComplete()
            {
                base.LoadComplete();

                readableKeyCombinationProvider.KeymapChanged += updateText;
                updateText();
            }

            protected override bool OnKeyDown(KeyDownEvent e)
            {
                if (keyCombination.IsPressed(new KeyCombination(KeyCombination.FromKey(e.Key)), KeyCombinationMatchingMode.Any))
                    box.Colour = Color4.Navy;

                return base.OnKeyDown(e);
            }

            protected override void OnKeyUp(KeyUpEvent e)
            {
                if (keyCombination.IsPressed(new KeyCombination(KeyCombination.FromKey(e.Key)), KeyCombinationMatchingMode.Any))
                    box.Colour = Color4.DarkGray;

                base.OnKeyUp(e);
            }

            private void updateText()
            {
                string newText = readableKeyCombinationProvider.GetReadableString(keyCombination);

                if (text.Text != newText)
                {
                    Schedule(() => box.FlashColour(Color4.LightBlue, 500));
                    text.Text = newText;
                }
            }

            protected override void Dispose(bool isDisposing)
            {
                base.Dispose(isDisposing);

                readableKeyCombinationProvider.KeymapChanged -= updateText;
            }
        }

        public class PressedKeyCombinationDisplay : CompositeDrawable
        {
            [Resolved]
            private ReadableKeyCombinationProvider readableKeyCombinationProvider { get; set; }

            private readonly SpriteText text;

            public PressedKeyCombinationDisplay()
            {
                Anchor = Anchor.TopCentre;
                Origin = Anchor.TopCentre;
                AutoSizeAxes = Axes.Both;

                InternalChildren = new[]
                {
                    text = new SpriteText
                    {
                        Font = new FontUsage(size: 20),
                        Text = "press a key",
                    }
                };
            }

            private void updateText(UIEvent e)
            {
                var state = new InputState(keyboard: e.CurrentState.Keyboard);
                var keyCombination = KeyCombination.FromInputState(state);
                string str = readableKeyCombinationProvider.GetReadableString(keyCombination);
                text.Text = $"pressed: {str}";
            }

            protected override bool OnKeyDown(KeyDownEvent e)
            {
                updateText(e);
                return base.OnKeyDown(e);
            }

            protected override void OnKeyUp(KeyUpEvent e)
            {
                updateText(e);
            }
        }
    }
}
