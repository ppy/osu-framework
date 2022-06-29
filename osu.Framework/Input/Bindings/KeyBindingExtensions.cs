// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

namespace osu.Framework.Input.Bindings
{
    public static class KeyBindingExtensions
    {
        /// <summary>
        /// Get the action associated with this binding, cast to the required enum type.
        /// </summary>
        /// <typeparam name="T">The enum type.</typeparam>
        /// <returns>A cast <typeparamref name="T"/> representation of <see cref="KeyBinding.Action"/>.</returns>
        public static T GetAction<T>(this IKeyBinding obj)
            where T : struct
        {
            return (T)obj.Action;
        }
    }
}
