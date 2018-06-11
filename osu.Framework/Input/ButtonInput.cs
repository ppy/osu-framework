// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System.Collections.Generic;

namespace osu.Framework.Input
{
    public abstract class ButtonInput<TButton> : IInput
    where TButton : struct
    {
        public IEnumerable<ButtonInputEntry<TButton>> Entries;

        protected abstract ButtonStates<TButton> GetButtonStates(InputState state);
        protected abstract void Handle(IInputStateChangeHandler handler, InputState state, TButton button, ButtonStateChangeKind kind);

        public void Apply(InputState state, IInputStateChangeHandler handler)
        {
            var buttonStates = GetButtonStates(state);
            foreach (var entry in Entries)
            {
                if (buttonStates.SetPressed(entry.Button, entry.IsPressed))
                {
                    Handle(handler, state, entry.Button, entry.IsPressed ? ButtonStateChangeKind.Pressed : ButtonStateChangeKind.Released);
                }
            }
        }
    }

    public struct ButtonInputEntry<TButton>
    where TButton : struct
    {
        public TButton Button;
        public bool IsPressed;

        public ButtonInputEntry(TButton button, bool isPressed)
        {
            Button = button;
            IsPressed = isPressed;
        }
    }

    internal static class ButtonInputHelper
    {
        public static TInput MakeSingleton<TInput, TButton>(TButton button, bool isPressed)
        where TButton : struct
        where TInput : ButtonInput<TButton>, new() => new TInput
        {
            Entries = new[] { new ButtonInputEntry<TButton>(button, isPressed) }
        };
    }
}
