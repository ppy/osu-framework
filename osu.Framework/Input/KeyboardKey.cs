// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Extensions;
using osu.Framework.Graphics;
using osu.Framework.Input.Handlers.Keyboard;
using osuTK.Input;

namespace osu.Framework.Input
{
    /// <summary>
    /// This struct encompasses two usages:
    /// <list type="table">
    ///     <item>Translating native keyboard input into <see cref="KeyboardKey"/>s (eg. <see cref="KeyboardHandler"/>) and propagating that trough the input hierarchy.</item>
    ///     <item>Drawables checking if said keys match some desired action in <see cref="Drawable.OnKeyDown"/>.</item>
    /// </list>
    /// </summary>
    public readonly struct KeyboardKey
    {
        /// <summary>
        /// The key that was pressed (roughly equivalent to a scancode).
        /// Independent of the system keyboard layout.
        /// </summary>
        /// <remarks>
        /// Should be matched against when the location of a key on the keyboard is more important than the character printed on it.
        /// </remarks>
        /// <seealso cref="Character"/>
        public Key Key { get; }

        /// <summary>
        /// The character that this key would generate if pressed (roughly equivalent to the keycode - the character printed on the key).
        /// Dependant on the system keyboard layout.
        /// </summary>
        /// <remarks>
        /// Should be matched against for common platform actions (eg. copy, paste) and actions that match mnemonically to the character (eg. 'o' for "open file").
        /// Generally, only alphanumeric characters [a-z, 0-9] are safe to match against. Other characters could be absent from international keyboard layouts,
        /// or appear in a shifted / AltGr state (something not currently provided by <see cref="KeyboardKey"/>).
        /// </remarks>
        /// <seealso cref="Key"/>
        public char Character { get; }

        public KeyboardKey(Key key, char character)
        {
            Key = key;
            Character = character;
        }

        public override string ToString() => $@"({ToString(Key, Character)})";

        /// <summary>
        /// Creates a new <see cref="KeyboardKey"/> from the specified <see cref="Key"/> while filling in the default character (if available).
        /// </summary>
        public static KeyboardKey FromKey(Key key) => new KeyboardKey(key, key.GetDefaultCharacter());

        public static string ToString(Key key, char c) => c == default
            ? $@"{key}"
            : $@"{key}, {c.StringRepresentation()}";
    }
}
