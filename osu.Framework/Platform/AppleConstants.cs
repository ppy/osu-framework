// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using osu.Framework.Input;
using osu.Framework.Input.Bindings;

namespace osu.Framework.Platform
{
    public static class AppleConstants
    {
        /// <summary>
        /// Platform key bindings for Apple devices.
        /// </summary>
        internal static IEnumerable<KeyBinding> KeyBindings => new[]
        {
            new KeyBinding(new KeyCombination(InputKey.Super, InputKey.X), PlatformAction.Cut),
            new KeyBinding(new KeyCombination(InputKey.Super, InputKey.C), PlatformAction.Copy),
            new KeyBinding(new KeyCombination(InputKey.Super, InputKey.V), PlatformAction.Paste),
            new KeyBinding(new KeyCombination(InputKey.Super, InputKey.A), PlatformAction.SelectAll),
            new KeyBinding(InputKey.Left, PlatformAction.MoveBackwardChar),
            new KeyBinding(InputKey.Right, PlatformAction.MoveForwardChar),
            new KeyBinding(InputKey.BackSpace, PlatformAction.DeleteBackwardChar),
            new KeyBinding(InputKey.Delete, PlatformAction.DeleteForwardChar),
            new KeyBinding(new KeyCombination(InputKey.Shift, InputKey.Left), PlatformAction.SelectBackwardChar),
            new KeyBinding(new KeyCombination(InputKey.Shift, InputKey.Right), PlatformAction.SelectForwardChar),
            new KeyBinding(new KeyCombination(InputKey.Shift, InputKey.BackSpace), PlatformAction.DeleteBackwardChar),
            new KeyBinding(new KeyCombination(InputKey.Shift, InputKey.Delete), PlatformAction.DeleteForwardChar),
            new KeyBinding(new KeyCombination(InputKey.Alt, InputKey.Left), PlatformAction.MoveBackwardWord),
            new KeyBinding(new KeyCombination(InputKey.Alt, InputKey.Right), PlatformAction.MoveForwardWord),
            new KeyBinding(new KeyCombination(InputKey.Alt, InputKey.BackSpace), PlatformAction.DeleteBackwardWord),
            new KeyBinding(new KeyCombination(InputKey.Alt, InputKey.Delete), PlatformAction.DeleteForwardWord),
            new KeyBinding(new KeyCombination(InputKey.Alt, InputKey.Shift, InputKey.Left), PlatformAction.SelectBackwardWord),
            new KeyBinding(new KeyCombination(InputKey.Alt, InputKey.Shift, InputKey.Right), PlatformAction.SelectForwardWord),
            new KeyBinding(new KeyCombination(InputKey.Super, InputKey.Left), PlatformAction.MoveBackwardLine),
            new KeyBinding(new KeyCombination(InputKey.Super, InputKey.Right), PlatformAction.MoveForwardLine),
            new KeyBinding(new KeyCombination(InputKey.Super, InputKey.BackSpace), PlatformAction.DeleteBackwardLine),
            new KeyBinding(new KeyCombination(InputKey.Super, InputKey.Delete), PlatformAction.DeleteForwardLine),
            new KeyBinding(new KeyCombination(InputKey.Super, InputKey.Shift, InputKey.Left), PlatformAction.SelectBackwardLine),
            new KeyBinding(new KeyCombination(InputKey.Super, InputKey.Shift, InputKey.Right), PlatformAction.SelectForwardLine),
            new KeyBinding(new KeyCombination(InputKey.Alt, InputKey.Super, InputKey.Left), PlatformAction.DocumentPrevious),
            new KeyBinding(new KeyCombination(InputKey.Alt, InputKey.Super, InputKey.Right), PlatformAction.DocumentNext),
            new KeyBinding(new KeyCombination(InputKey.Super, InputKey.W), PlatformAction.DocumentClose),
            new KeyBinding(new KeyCombination(InputKey.Super, InputKey.N), PlatformAction.DocumentNew),
            new KeyBinding(new KeyCombination(InputKey.Super, InputKey.T), PlatformAction.TabNew),
            new KeyBinding(new KeyCombination(InputKey.Super, InputKey.Shift, InputKey.T), PlatformAction.TabRestore),
            new KeyBinding(new KeyCombination(InputKey.Control, InputKey.Tab), PlatformAction.DocumentNext),
            new KeyBinding(new KeyCombination(InputKey.Control, InputKey.Shift, InputKey.Tab), PlatformAction.DocumentPrevious),
            new KeyBinding(new KeyCombination(InputKey.Super, InputKey.S), PlatformAction.Save),
            new KeyBinding(new KeyCombination(InputKey.Super, InputKey.Up), PlatformAction.MoveToListStart),
            new KeyBinding(new KeyCombination(InputKey.Super, InputKey.Down), PlatformAction.MoveToListEnd),
            new KeyBinding(new KeyCombination(InputKey.Super, InputKey.Z), PlatformAction.Undo),
            new KeyBinding(new KeyCombination(InputKey.Super, InputKey.Shift, InputKey.Z), PlatformAction.Redo),
            new KeyBinding(new KeyCombination(InputKey.Delete), PlatformAction.Delete),
            new KeyBinding(new KeyCombination(InputKey.Super, InputKey.Plus), PlatformAction.ZoomIn),
            new KeyBinding(new KeyCombination(InputKey.Super, InputKey.KeypadPlus), PlatformAction.ZoomIn),
            new KeyBinding(new KeyCombination(InputKey.Super, InputKey.Minus), PlatformAction.ZoomOut),
            new KeyBinding(new KeyCombination(InputKey.Super, InputKey.KeypadMinus), PlatformAction.ZoomOut),
            new KeyBinding(new KeyCombination(InputKey.Super, InputKey.Number0), PlatformAction.ZoomDefault),
            new KeyBinding(new KeyCombination(InputKey.Super, InputKey.Keypad0), PlatformAction.ZoomDefault),
        };
    }
}
