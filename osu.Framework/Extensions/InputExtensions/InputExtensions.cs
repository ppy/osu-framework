using OpenTK.Input;

namespace osu.Framework.Extensions.InputExtensions
{
    /// <summary>
    /// This class holds extension methods for mouse/keyboard input
    /// </summary>
    public static class InputExtensions
    {
        /// <summary>
        /// Gets a <see cref="System.Boolean"/> indicating whether at least one of the specified
        /// <see cref="OpenTK.Input.Key"/> objects is pressed.
        /// </summary>
        /// <param name="keyboardState">The <see cref="KeyboardState"/> used for the check</param>
        /// <param name="keys">The <see cref="Key"/> objects to check.</param>
        /// <returns>True if key is pressed; false otherwise.</returns>
        public static bool IsKeyDown(this KeyboardState keyboardState, params Key[] keys)
        {
            foreach (var key in keys)
                if (keyboardState.IsKeyDown(key))
                    return true;
            return false;
        }
    }
}
