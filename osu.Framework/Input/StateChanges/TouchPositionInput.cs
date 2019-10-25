// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using osu.Framework.Input.StateChanges.Events;
using osu.Framework.Input.States;
using osuTK.Input;

namespace osu.Framework.Input.StateChanges
{
    /// <summary>
    /// Denotes an absolute change in touch position input.
    /// When all provided pointers are not active, no changes will be made.
    /// </summary>
    public class TouchPositionInput : IInput
    {
        public IReadOnlyList<PositionalPointer> Pointers;

        public void Apply(InputState state, IInputStateChangeHandler handler)
        {
            var touch = state.Touch;

            if (Pointers == null)
                return;

            var activePointers = touch.Pointers.ToList();
            var buttonsState = state.Mouse.Buttons.Clone();

            foreach (var pointer in Pointers)
            {
                for (int i = 0; i < activePointers.Count; i++)
                {
                    if (!pointer.Equals(activePointers[i]) || !(pointer.Position != activePointers[i].Position))
                        continue;

                    var buttons = new List<MouseButton>();
                    if (touch.PrimaryPointer?.Equals(pointer) ?? false)
                        buttons.Add(MouseButton.Left);
                    else
                        buttons.Add(pointer.Source);

                    state.Mouse.Buttons.Set(buttons);
                    state.Mouse.Position = pointer.Position;
                    handler.HandleInputStateChange(new MousePositionChangeEvent(state, this, activePointers[i].Position));

                    activePointers[i] = pointer;
                }
            }

            state.Mouse.Buttons.Set(buttonsState);
            if (touch.PrimaryPointer != null)
                state.Mouse.Position = touch.PrimaryPointer.Value.Position;

            touch.Pointers.Set(activePointers);
        }
    }
}
