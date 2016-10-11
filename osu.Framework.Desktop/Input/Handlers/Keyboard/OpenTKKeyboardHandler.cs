// Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Collections.Generic;
using osu.Framework.Input.Handlers;
using OpenTK.Input;
using osu.Framework.Platform;

namespace osu.Framework.Desktop.Input.Handlers.Keyboard
{
    class OpenTKKeyboardHandler : InputHandler, IKeyboardInputHandler
    {
        public override bool IsActive => true;

        public override int Priority => 0;

        public override void Dispose()
        {
        }

        public event Action<char> CharacterInput;

        internal void OnCharacterInput(char c)
        {
            CharacterInput?.Invoke(c);
        }

        public override bool Initialize(BasicGameHost host)
        {
            PressedKeys = new List<Key>();
            return true;
        }

        public override void UpdateInput(bool isActive)
        {
            OpenTK.Input.KeyboardState state = OpenTK.Input.Keyboard.GetState();

            PressedKeys.Clear();

            if (state.IsAnyKeyDown)
            {
                foreach (Key k in Enum.GetValues(typeof(Key)))
                {
                    if (state.IsKeyDown(k))
                        PressedKeys.Add(k);
                }
            }
        }

        public List<Key> PressedKeys { get; set; }
    }
}
